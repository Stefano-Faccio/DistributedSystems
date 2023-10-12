using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal class Client: UntypedActor
    {
        bool debug = true;
        protected override void PreStart()
        {
            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Client Started");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        protected override void PostStop()
        {
            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Client gone!");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }



        protected void DoGetRequest(GetMessage message)
        {
            Thread.Sleep(1000);

            if(debug)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"    Client send Get {message.Key} request to node {Context.ActorSelection("/user/0").Path}");
            }
            //Qui la richiesta parte
            Task testGet = Context.ActorSelection("/user/0").Ask(new GetMessage(5), TimeSpan.FromSeconds(1));
            //Task.WhenAny(testGet).PipeTo(Context.ActorSelection("/user/0"));
            //Task.WhenAny(testGet).PipeTo(Context.ActorSelection("/user/0"), Self);
        }

        protected void OnGetResponse(GetResponseMessage message)
        {
            Console.WriteLine($"Risposta: Key={message.Key} Value={message.Value}");
            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Client recive Get from {Sender.Path}");
            }
        }

        protected override void OnReceive(object msg)
        {
            switch(msg)
            {
                case GetResponseMessage message:
                    OnGetResponse(message);
                    break;
                case GetMessage message:
                    DoGetRequest(message);
                    break;
                default:
                    throw new Exception("Not implemented yet!");
            }
        }
    }
}
