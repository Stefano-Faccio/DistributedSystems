using Akka.Actor;
using MathNet.Numerics.Random;
using System.Linq;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    internal class Client : UntypedActor
    {
        //Lista degli altri nodi
        SortedSet<uint> nodes = new();

        protected override void PreStart()
        {
            if (generalDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} started succesfully");
                }
        }

        protected override void PostStop()
        {
            if (generalDebug)
                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{Self.Path.Name} is gone!");
                    Console.ResetColor();
                }
        }

        protected void AddNode(AddNodeMessage message)
        {
            nodes.Add(message.Id);

            if (deepDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} added node {message.Id}");
                }
        }
        protected void RemoveNode(RemoveNodeMessage message)
        {
            nodes.Remove(message.Id);

            if (deepDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} removed node {message.Id}");
                }
        }

        protected void DoGetRequest(GetClientMessage message)
        {
            uint nodeToAsk = message.NodeToAsk;
            //Prendo un nodo a caso se non mi è dato un nodo a cui chiedere
            if(nodeToAsk == UInt32.MaxValue)
                nodeToAsk = (uint)myMersenneTwister.Next(nodes.Count);

            ActorSelection receiver = Context.ActorSelection($"/user/node{nodes.ElementAt((int)nodeToAsk)}");

            if (generalDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} request GET from {receiver.Path[receiver.Path.Length - 1]} => Key:{message.Key}");
                }

            //Qui la richiesta parte
            receiver.Tell(new GetMessage(message.Key), Self);
        }
        protected void DoUpdateRequest(UpdateClientMessage message)
        {
            uint nodeToAsk = message.NodeToAsk;
            //Prendo un nodo a caso se non mi è dato un nodo a cui chiedere
            if (nodeToAsk == UInt32.MaxValue)
                nodeToAsk = (uint)myMersenneTwister.Next(nodes.Count);

            ActorSelection receiver = Context.ActorSelection($"/user/node{nodes.ElementAt((int)nodeToAsk)}");

            if (generalDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} request WRITE from {receiver.Path[receiver.Path.Length - 1]} => Key:{message.Key}, Value:{message.Value}");
                }

            //Qui la richiesta parte
            receiver.Tell(new UpdateMessage(message.Key, message.Value), Self);
        }

        protected void OnGetResponse(GetResponseMessage message)
        {
            lock (Console.Out)
            {
                if (message.Timeout)
                    Console.ForegroundColor = ConsoleColor.Red;
                else
                    Console.ForegroundColor = ConsoleColor.Green;

                Console.WriteLine($"{Self.Path.Name} received GET RESPONSE from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value ?? "null"} Timeout:{message.Timeout}");
                Console.ResetColor();
            }
        }

        protected void OnUpdateResponse(UpdateResponseMessage message)
        {
            lock (Console.Out)
            {
                if (message.Achieved)
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"{Self.Path.Name} received UPDATE RESPONSE from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value ?? "null"} Achieved:{message.Achieved}");
                Console.ResetColor();
            }
        }

        protected void Test(TestMessage message)
        {
            lock (Console.Out)
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
            switch (msg)
            {
                case UpdateResponseMessage message:
                    OnUpdateResponse(message);
                    break;
                case GetResponseMessage message:
                    OnGetResponse(message);
                    break;
                case GetClientMessage message:
                    DoGetRequest(message);
                    break;
                case UpdateClientMessage message:
                    DoUpdateRequest(message);
                    return;
                case AddNodeMessage message:
                    AddNode(message);
                    break;
                case RemoveNodeMessage message:
                    RemoveNode(message);
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
