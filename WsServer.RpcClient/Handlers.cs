using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcExamples.Api;
using JsonRpcExamples.Api.Models;

namespace JsonRpcExamples.WsServer
{
    class Handlers : IHandlers
    {
        private OpenSocketsService manager;

        public Handlers(OpenSocketsService manager) { 
        }
        
        async public Task<string> Ping()
        {
            Console.WriteLine($"Ping from server!");
            return "Pong";
        }

        async public Task Tick(int tick)
        {
            Console.WriteLine("Tick "+tick.ToString());
        }

        async public Task<int> Add(int a, int b)
        {
            await Task.Delay(4000);
            return a + b;
        }

        async public Task Hello(Hello info)
        {
            Console.WriteLine("Hello! My name: "+info.name.ToString());
            return;
        }

        async public Task<Subscription> SubscribeTick()
        {
            throw new NotImplementedException();
        }

        async public Task<Subscription> UnsubscribeTick(Subscription subscription)
        {
            throw new NotImplementedException();
        }
    }
}
