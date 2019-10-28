using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace JsonRpcExamples.RpcServer
{
    class Handlers
    {
        /// <summary>
        /// Occurs every second. Just for the heck of it.
        /// </summary>
        public event EventHandler<int> Tick;

        public int Add(int a, int b) => a + b;


        /// <summary>
        /// Notification subscription
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendTicksAsync(CancellationToken cancellationToken)
        {
            int tickNumber = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                this.Tick?.Invoke(this, ++tickNumber);
            }
        }
    }
}
