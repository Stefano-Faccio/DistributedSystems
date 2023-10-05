using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace DistributedKeyValueStore.NET
{
    internal class GreetingActor : ReceiveActor
    {
        public GreetingActor()
        {
            // Tell the actor to respond to the Greet message
            Receive<Greet>(greet => Console.WriteLine($"Hello {greet.Who}"));
        }
        protected override void PreStart()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Good Morning, we are awake!");
        }

        protected override void PostStop()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Good Night, going to bed!");
        }
    }
}
