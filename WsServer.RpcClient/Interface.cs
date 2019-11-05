using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonRpcExamples.Api
{
    using Models;
    /// <summary>
    /// API
    /// </summary>
    public interface IHandlers
    {
        /// <summary>
        /// Sum of numbers
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>int result</returns>
        Task<int> Add(int a, int b);

        /// <summary>
        /// Ping
        /// </summary>
        /// <returns>"Pong"</returns>
        Task<string> Ping();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        public Task Tick(int tick);

        /// <summary>
        /// Hello message
        /// </summary>
        /// <param name="info"></param>
        Task Hello(Hello info);

        /// <summary>
        /// Subscribe to ping
        /// </summary>
        /// <returns></returns>
        Task<Subscription> SubscribeTick();
        
        /// <summary>
        /// Unsubscribe
        /// </summary>
        /// <param name="subscription">Object with id of a task, that should be stopped.</param>
        /// <returns></returns>
        Task<Subscription> UnsubscribeTick(Subscription subscription);
    }

    namespace Models
    {
        public class Subscription
        {
            /// <summary>
            /// Identification of task
            /// 
            /// String to be sure, we dont run ot of ints.
            /// </summary>
            public string i { get; set; }
            /// <summary>
            /// Status
            /// True - running
            /// False - stopped
            /// </summary>
            public bool s { get; set; }
        }

        public class Hello
        {
            /// <summary>
            /// Identification of 
            /// </summary>
            public string name { get; set; }
        }
    }
}
