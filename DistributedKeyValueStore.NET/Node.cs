using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal class Node : ReceiveActor
    {
        public Node()
        {
            Receive<WriteMessage>(OnWrite);
            Receive<UpdateMessage>(OnUpdate);
            Receive<GetMessage>(OnGet);
            Receive<ReadMessage>(OnRead);
            Receive<PreWriteMessage>(OnPreWrite);
        }

        /*
         * protected override void OnReceive(object message)
    {
        switch (message)
        {
            case "test":
                log.Info("received test");
                break;
            default:
                log.Info("received unknown message");
                break;
        }
    }
         */

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

        protected void OnWrite(WriteMessage message)
        {

        }

        protected void OnUpdate(UpdateMessage message)
        {

        }

        protected void OnGet(GetMessage message)
        {

        }

        protected void OnRead(ReadMessage message)
        {

        }
        
        protected void OnPreWrite(PreWriteMessage message)
        {

        }

    }
}
