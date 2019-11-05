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
using Newtonsoft.Json;

namespace JsonRpcExamples.WsServer
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
            colllection.AddSingleton<OpenSocketsService>();
        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.UseMiddleware<WSDispatcherMiddleware>(); //receives socket here
        }
    }

    /// <summary>
    /// Takes care of new websocket connection and loads singleton OpenWebSockesMiddleware
    /// </summary>
    class WSDispatcherMiddleware
    {
        private OpenSocketsService service;
        public WSDispatcherMiddleware(RequestDelegate req,OpenSocketsService service)
        {
            this.service = service;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }
            await service.HandleNewConnection(await context.WebSockets.AcceptWebSocketAsync());
        }
    }


    public class OpenSocketsService
    {
        private ConcurrentDictionary<int, OpenSocket> connections;
        private int counter;

        public OpenSocketsService()
        {
            this.connections = new ConcurrentDictionary<int, OpenSocket>();
        }

        public async Task HandleNewConnection(WebSocket ws)
        {
            OpenSocket osc = new OpenSocket(ws, this.counter, this);
            
            if(!this.connections.TryAdd(this.counter++, osc))
            {
                await osc.socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Missmatch in ids", CancellationToken.None);
            }
            else
            {
                await  osc.Dispatch();
            }

        }

        public class OpenSocket
        {
            private OpenSocketsService manager;
            public WebSocket socket;
            public JsonRpc rpc;
            private int socket_id;

            public OpenSocket(WebSocket ws, int id, OpenSocketsService manager)
            {
                this.socket = ws;
                this.socket_id = id;
                this.manager = manager;
                this.rpc = new JsonRpc(new WebSocketMessageHandler(this.socket), new Handlers(this.manager));
            }

            public int GetId() => this.socket_id;

            public async Task Dispatch()
            {
                    Console.WriteLine("New connection: " + this.socket_id.ToString());

                    using (rpc){
                        rpc.CancelLocallyInvokedMethodsWhenConnectionIsClosed = true;
                        rpc.StartListening();
                        // Attach api
                        var otherSide = rpc.Attach<Api.IHandlers>();
                        while (true)
                        {
                            try{
                                // Call and response
                                Console.WriteLine(await otherSide.Ping());
                                await otherSide.Hello(new Api.Models.Hello { name = "C#" });

                                var result = await otherSide.SubscribeTick();

                                Thread.Sleep(10000);
                                if (result.s)
                                {
                                    Console.WriteLine("Status true");
                                }

                                var result2 = await otherSide.UnsubscribeTick(result);
                                Console.WriteLine(JsonConvert.SerializeObject(result2));
                                await rpc.Completion; // throws exceptions - closed connection,etc.
                                if (rpc.Completion.Exception == null)
                                {
                                    break;
                                }
                            }catch (Exception e) { 
                                if(e is WebSocketException || e is ConnectionLostException)
                                {
                                    Console.WriteLine(e.Message.ToString());
                                    Console.WriteLine("================================================");
                                    break;
                                }
                                if(e is RemoteRpcException)
                                {
                                    Console.WriteLine(e.Message.ToString());
                                    Console.WriteLine("================================================");
                                    break;
                                }
                            }
                        }
                        Console.WriteLine("End connection: " + this.socket_id.ToString());
                        this.manager.connections.TryRemove(this.socket_id, out OpenSocket value);
                    }             
            }        
        }
    }
}
