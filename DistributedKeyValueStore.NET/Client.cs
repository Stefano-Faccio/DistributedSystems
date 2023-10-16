using Akka.Actor;
using MathNet.Numerics.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal class Client: UntypedActor
    {
        //Lista degli altri nodi
        SortedSet<uint> nodes = new SortedSet<uint>();

        bool debug = true;
        protected override void PreStart()
        {
            if (debug)
            {
                Console.WriteLine($"{Self.Path.Name} started succesfully");
            }
        }

        protected override void PostStop()
        {
            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Self.Path.Name} is gone!");
                Console.ResetColor();
            }
        }

        protected void AddNode(AddNodeMessage message)
        {
            nodes.Add(message.Id);

            if (debug)
            {
                Console.WriteLine($"{Self.Path.Name} added node {message.Id}");
            }
        }
        protected void RemoveNode(RemoveNodeMessage message)
        {

        }

        protected void DoGetRequest(GetMessage message)
        {
            //Prendo un nodo a caso
            MersenneTwister mersenneTwister = new MersenneTwister(Guid.NewGuid().GetHashCode());
            ActorSelection receiver = Context.ActorSelection($"/user/node{nodes.ElementAt(mersenneTwister.Next(nodes.Count))}");

            if (debug)
            {
                Console.WriteLine($"{Self.Path.Name} request GET from {receiver.Path[receiver.Path.Length-1]} => Key:{message.Key}");
            }

            //Qui la richiesta parte
            receiver.Tell(new GetMessage(message.Key), Self);
        }

        protected void OnGetResponse(GetResponseMessage message)
        {
            if (debug)
            {
                Console.WriteLine($"{Self.Path.Name} received GET RESPONSE from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value ?? "null"}");
            }
        }

        protected void Test(TestMessage message)
        {
            if (debug)
            {
                Console.WriteLine($"Count nodes: {nodes.Count}");
                foreach (var foo in nodes)
                {
                    Console.Write(foo.ToString() + " ");
                }
                Console.WriteLine();
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
                case AddNodeMessage message:
                    AddNode(message);
                    break;
                case RemoveNodeMessage message:
                    RemoveNode(message);
                    break;
                case ReadResponseMessage message: //TEMPORANEO REINDIRIZZO
                    OnGetResponse(new GetResponseMessage(message.Key, message.Value));
                    break;
                case TestMessage message:
                    Test(message);
                    break;
                default:
                    throw new Exception("Not implemented yet!");
            }
        }
    }
}
