using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace JsonRpcExamples.RpcClient
{
    class Handlers
    {

        public void Tick(int param) => Console.WriteLine($"TickFromRpcServer: {param}!");

        public void Hello(string param) => Console.WriteLine($"HelloFromWsClient: {param}!");

        public int Add(int a, int b){
            Thread.Sleep(5000);
            return a + b;
        }


    }

    public class TickSubs
    {
        public int CId { get; set; }
        public string S { get; set; }
    }

    public interface IFoo
    {
        Task<TickSubs> SubscribeTick();
        Task<TickSubs> UnsubscribeTick(TickSubs sub);
        Task<string> Ping();
    }
}
