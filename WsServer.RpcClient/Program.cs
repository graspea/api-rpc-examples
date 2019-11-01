using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using StreamJsonRpc;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using JsonRpcExamples.RpcClient;
using Newtonsoft.Json;

namespace JsonRpcExamples.WsServer.RpcClient
{
    class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://localhost:5000")
                .UseStartup<Startup>()
                .Build();
    }

    class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection colllection)
        {
            colllection.AddSingleton<OpenWebSocketsMiddleware>();
        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.UseMiddleware<DispatcherMiddleware>(); //receives socket here
        }
    }

    /// <summary>
    /// Takes care of new websocket connection and loads singleton OpenWebSockesMiddleware
    /// </summary>
    class DispatcherMiddleware
    {
        private OpenWebSocketsMiddleware middleware;
        public DispatcherMiddleware(RequestDelegate req,OpenWebSocketsMiddleware middleware)
        {
            this.middleware = middleware;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }
            await middleware.HandleNewConnection(await context.WebSockets.AcceptWebSocketAsync());
        }
    }


    public class OpenWebSocketsMiddleware
    {
        private ConcurrentDictionary<int, OpenSocket> connections;
        private int counter;
        CancellationTokenSource source = new CancellationTokenSource();

        public OpenWebSocketsMiddleware()
        {
            this.connections = new ConcurrentDictionary<int, OpenSocket>();
        }

        public async Task HandleNewConnection(WebSocket ws)
        {
            OpenSocket osc = new OpenSocket(ws, this.counter, this);
            
            if(!this.connections.TryAdd(this.counter++, osc))
            {
                await osc.socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Missmatch in ids", this.source.Token);
            }
            else
            {
                var a =  osc.Dispatch();
                await a;
                Console.WriteLine("End of dispatching for OpenSocket "+ osc.GetId().ToString());
            }

        }

        public class OpenSocket
        {
            private OpenWebSocketsMiddleware manager;
            public WebSocket socket;
            public JsonRpc rpc;
            private int socket_id;

            public OpenSocket(WebSocket ws, int id, OpenWebSocketsMiddleware manager)
            {
                this.socket = ws;
                this.socket_id = id;
                this.manager = manager;
                this.rpc = new JsonRpc(new WebSocketMessageHandler(this.socket), new Handlers());
            }

            public int GetId() => this.socket_id;

            public async Task Dispatch()
            {
                try
                {
                    Console.WriteLine("New connection: " + this.socket_id.ToString());

                    using (rpc)
                    {
                        rpc.CancelLocallyInvokedMethodsWhenConnectionIsClosed = true;
                        rpc.AddLocalRpcMethod("PrintSomething", new Action<int>(param => Console.WriteLine($"Something: {param}!")));
                        rpc.StartListening();
                        var foo = rpc.Attach<IFoo>();
                        // Call and response
                        Console.WriteLine(await foo.Ping());

                        var result = await foo.SubscribeTick();
                        
                        Thread.Sleep(10000);

                        var result2 = await foo.UnsubscribeTick(result);
                        Console.WriteLine(JsonConvert.SerializeObject(result2));

                        await rpc.Completion; // throws exceptions - closed connection,etc.
                    }
                }
                catch (WebSocketException ee)
                {
                    Console.WriteLine("Unhandled end of WebSocket: "+ee.ToString());
                }catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    Console.WriteLine("End connection: " + this.socket_id.ToString());
                    this.manager.connections.TryRemove(this.socket_id, out OpenSocket value);
                }
            }        
        }
    }
}
