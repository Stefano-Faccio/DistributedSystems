using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace DistributedKeyValueStore.NET
{
    class TestAkka
    {


        static void Main(string[] args)
        {
            // create a new actor system (a container for actors)
            var system = ActorSystem.Create("the-universe");

            // create actor and get a reference to it.
            // this will be an "ActorRef", which is not a 
            // reference to the actual actor instance
            // but rather a client or proxy to it
            var greeter = system.ActorOf<GreetingActor>("greeter");

            // send a message to the actor
            greeter.Tell(new Greet("World"));

            //this is for demostration purposes
            Thread.Sleep(5000);
            system.Stop(greeter);

            // prevent the application from exiting before message is handled            
            Console.ReadLine();
        }
    }

    internal class Greet
    {
        public string Who { get; private set; }

        public Greet(string who)
        {
            Who = who;
        }
    }
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
