using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using StreamJsonRpc;
using JsonRpcExamples.RpcServer;

namespace JsonRpcExamples.WsServer.RpcServer
{
    public class Program
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

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseWebSockets();
            app.Map("/ws",builder => {
                builder.Use(async(context, next)=> {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        JsonRpc jsonRpc = new JsonRpc(new WebSocketMessageHandler(webSocket), new Handlers());
                        using (jsonRpc)
                        {
                            jsonRpc.CancelLocallyInvokedMethodsWhenConnectionIsClosed = true; 
                            jsonRpc.StartListening();
                            await jsonRpc.Completion;
                        }
                        return;
                    }
                    await next();
                });
            });

        }
    }
}
