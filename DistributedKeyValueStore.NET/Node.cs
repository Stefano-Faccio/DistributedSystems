using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal class Node : UntypedActor
    {
        //Datatabase dei dati chiave valore del nodo
        Collection data = new Collection();
        //Lista degli altri nodi
        SortedSet<uint> nodes = new SortedSet<uint>();
        //Id del nodo
        public uint Id { get; private set; }

        bool debug = true;

        public Node()
        {
            //Setto l'id ad un valore di default
            Id = uint.MaxValue;
        }

        protected override void PreStart()
        {
            /*
            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Node {Id} started succesfully");
                Console.ForegroundColor = ConsoleColor.White;
            }
            */
        }

        protected override void PostStop()
        {
            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Node {Id} gone!");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        protected void Start(StartMessage message)
        {
            if (this.Id == uint.MaxValue)
            {
                this.Id = message.Id;

                if(debug)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Node {Id} initialized succesfully");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            else
                throw new Exception("Node already initialided");
        }

        protected void AddNode(AddNodeMessage message)
        {
            nodes.Add(message.Id);
            
            if(debug)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  Node {Id} added node {message.Id}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        protected void RemoveNode(RemoveNodeMessage message)
        {

        }

        protected void OnWrite(WriteMessage message)
        {

        }

        protected void OnUpdate(UpdateMessage message)
        {

        }

        protected void OnGet(GetMessage message)
        {
            Thread.Sleep(1000);//Da togliere
            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"    Node {Id} received Get {message.Key} from {Sender.Path}");
                Console.WriteLine($"    Node {Id} send responce to {Sender.Path}");
            }
            //Sender.Tell(new GetResponseMessage(message.Key, "69"), Self);
        }

        protected void OnRead(ReadMessage message)
        {

        }

        protected void OnPreWrite(PreWriteMessage message)
        {

        }

        protected override void OnReceive(object msg)
        {
            //if(msg is not null)
            switch (msg) 
            {
                case StartMessage message:
                    Start(message);
                    break;
                case AddNodeMessage message:
                    AddNode(message);
                    break;
                case RemoveNodeMessage message:
                    RemoveNode(message);
                    break;
                case ReadMessage message:
                    OnRead(message);
                    break;
                case UpdateMessage message:
                    OnUpdate(message);
                    break;
                case GetMessage message:
                    OnGet(message);
                    break;
                case WriteMessage message:
                    OnWrite(message);
                    break;
                case PreWriteMessage message:
                    OnPreWrite(message);
                    break;
                default:
                    throw new Exception("Not yet implemented!");
            }
        }
    }
}
