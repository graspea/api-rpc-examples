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
}
