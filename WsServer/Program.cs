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
            app.UseMiddleware<WsMiddleware>();
        }
    }

    /// <summary>
    /// Takes care of new websocket connection and loads singleton OpenWebSockesMiddleware
    /// </summary>
    class WsMiddleware
    {
        private OpenSocketsService service;
        public WsMiddleware(RequestDelegate req,OpenSocketsService service)
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
        private ConcurrentDictionary<int, OpenSocket> Connections;
        private int Counter;

        public OpenSocketsService()
        {
            this.Connections = new ConcurrentDictionary<int, OpenSocket>();
        }

        public async Task HandleNewConnection(WebSocket ws)
        {
            int id = this.Counter;
            this.Counter++;
            OpenSocket osc = new OpenSocket(ws, this,id);
            
            if(!this.Connections.TryAdd(this.Counter++, osc))
            {
                // Missing proper CancellationToken handling
                await osc.Socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Missmatch in ids", CancellationToken.None);
            }
            else
            {
                await  osc.Dispatch();
            }

        }

        public class OpenSocket
        {
            private OpenSocketsService Manager;
            public readonly int Id;
            public WebSocket Socket;
            public JsonRpc Rpc;

            public OpenSocket(WebSocket ws, OpenSocketsService manager, int id)
            {
                this.Socket = ws;
                this.Manager = manager;
                this.Rpc = new JsonRpc(new WebSocketMessageHandler(this.Socket), new Handlers(this.Manager));
            }

            public async Task Dispatch()
            {

                    using (Rpc){
                        Rpc.CancelLocallyInvokedMethodsWhenConnectionIsClosed = true;
                        Rpc.StartListening();
                        // Attach api
                        var otherSide = Rpc.Attach<Api.IHandlers>();
                        while (true)
                        {
                            try{
                                // Call and response
                                Console.WriteLine(await otherSide.Ping());
                                await otherSide.Hello(new Api.Models.Hello { name = "C#" });

                                var result = await otherSide.SubscribeTick();

                                await Task.Delay(5000);
                                var result2 = await otherSide.UnsubscribeTick(result);
                                Console.WriteLine(JsonConvert.SerializeObject(result2));
                                await Rpc.Completion; // throws exceptions - closed connection,etc.
                                if (Rpc.Completion.Exception == null)
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

                        // Missing: WS close
                        this.Manager.Connections.TryRemove(this.Id, out OpenSocket value);
                    }             
            }        
        }
    }
}
