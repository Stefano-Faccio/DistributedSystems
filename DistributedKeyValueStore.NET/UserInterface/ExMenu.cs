/*
using Akka.Actor;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static DistributedKeyValueStore.NET.Constants;
using static System.Console;

namespace DistributedKeyValueStore.NET.UserInterface
{
    internal class TestCases
    {
        //Variabili per il funzioanamento della rete
        List<uint> nodi;
        List<uint> clients;
        ActorSystem system;

        static void Main(string[] args)
        {
            TestCases test = new TestCases();
        }

        public TestCases()
        {
            //Creazione contenitore per gli actors
            system = ActorSystem.Create("povoland");
            //Inizializzo le liste per i nodi
            nodi = new();
            clients = new();

            PrintMenu();
        }

        void Case1()
        {
            string str;
            uint key;
            uint clientToAskId;
            uint nodeToAskId;

            do
            {
                Write("Insert the key to get: ");
                str = ReadLine() ?? "";
            } while (!UInt32.TryParse(str, out key));

            do
            {
                Write("Insert the client Id to ask to ask for the key (for random node just press enter): ");
                str = ReadLine() ?? "";

                if (str.Trim() == "")
                {
                    clientToAskId = nodi[myMersenneTwister.Next(nodi.Count)];
                    break;
                }
            } while (!UInt32.TryParse(ReadLine(), out clientToAskId));

            do
            {
                Write("Insert the node Id to ask the key (for random node just press enter): ");
                str = ReadLine() ?? "";

                if (str.Trim() == "")
                {
                    nodeToAskId = nodi[myMersenneTwister.Next(nodi.Count)];
                    break;
                }
            } while (!UInt32.TryParse(ReadLine(), out nodeToAskId));

            WriteLine($"Get key {key} from node {nodeToAskId} by client {clientToAskId}\n");

            system.ActorSelection($"/user/client{clientToAskId}").Tell(new GetMessage(key));

            //TODO aggiungere scelta nodo di destinazione
        }

        void Case2()
        {
            string str;
            uint key;
            uint clientToAskId;
            uint nodeToAskId;
            string value;

            do
            {
                Write("Insert the key to update: ");
                str = ReadLine() ?? "";
            } while (!UInt32.TryParse(str, out key));

            do
            {
                Write("Insert the new value: ");
                value = ReadLine() ?? "";
                value = value.Trim();
            } while (value == "");

            do
            {
                Write("Insert the client Id to ask to ask for the update (for random node just press enter): ");
                str = ReadLine() ?? "";

                if (str.Trim() == "")
                {
                    clientToAskId = nodi[myMersenneTwister.Next(nodi.Count)];
                    break;
                }
            } while (!UInt32.TryParse(ReadLine(), out clientToAskId));

            do
            {
                Write("Insert the node Id to ask the update (for random node just press enter): ");
                str = ReadLine() ?? "";

                if (str.Trim() == "")
                {
                    nodeToAskId = nodi[myMersenneTwister.Next(nodi.Count)];
                    break;
                }
            } while (!UInt32.TryParse(ReadLine(), out nodeToAskId));

            WriteLine($"Update key {key} with value {value} from node {nodeToAskId} by client {clientToAskId}\n");

            system.ActorSelection($"/user/client{clientToAskId}").Tell(new UpdateMessage(key, value));

            //TODO aggiungere scelta nodo di destinazione
        }

        void Case3()
        {
            uint newNodeId;
            uint nodeToAskId;
            string str;

            do
            {
                Write("Insert new node Id (for default just press enter): ");
                str = ReadLine() ?? "";

                if (str.Trim() == "")
                {
                    newNodeId = nodi.Count > 0 ? nodi.Max() + 10 : 0;
                    break;
                }
            } while (!UInt32.TryParse(str, out newNodeId));

            do
            {
                Write("Insert the node Id to ask the list of nodes (for random node just press enter): ");
                str = ReadLine() ?? "";

                if (str.Trim() == "")
                {
                    nodeToAskId = nodi.Count > 0 ? nodi[myMersenneTwister.Next(nodi.Count)] : newNodeId;
                    break;
                }
            } while (!UInt32.TryParse(ReadLine(), out nodeToAskId));

            WriteLine($"Create new node with id {newNodeId} and ask to node {nodeToAskId} for info\n");

            nodi.Add(newNodeId);
            system.ActorOf<Node>("node" + newNodeId.ToString()).Tell(new StartMessage(newNodeId, nodeToAskId));
        }

        void Case4()
        {
            uint newClientId;
            string str;

            do
            {
                Write("Insert new client Id (for default just press enter): ");
                str = ReadLine() ?? "";

                if (str.Trim() == "")
                {
                    newClientId = clients.Count > 0 ? clients.Max() + 10 : 0;
                    break;
                }
            } while (!UInt32.TryParse(str, out newClientId));

            WriteLine($"Create new client with id {newClientId}\n");

            clients.Add(newClientId);
            system.ActorOf<Client>("client" + newClientId.ToString());
        }
        void Case5()
        {
            uint nodeId;
            string str;

            do
            {
                Write("Insert node Id that have to leave the network (for default just press enter): ");
                str = ReadLine() ?? "";

                if (str.Trim() == "")
                {
                    nodeId = nodi[myMersenneTwister.Next(nodi.Count)];
                    break;
                }
            } while (!UInt32.TryParse(str, out nodeId));

            WriteLine($"Send leave network to node with id {nodeId}\n");

            system.ActorSelection($"/user/node{nodeId}").Tell(new StopMessage(nodeId));
        }


        void Case6()
        {

        }

        void Case7()
        {

        }

        void Case8()
        {
            WriteLine();

            foreach (var node in nodi)
            {
                system.ActorSelection($"/user/node{node}").Tell(new TestMessage());
                Thread.Sleep(100);
            }

            foreach (var client in clients)
            {
                system.ActorSelection($"/user/client{client}").Tell(new TestMessage());
                Thread.Sleep(100);
            }
        }


        void PrintMenu()
        {
            //Varibili per il menu
            uint menuChoice;
            bool stop = false;
            ConsoleColor coloreMenu = ConsoleColor.Blue;

            PrintHeader("*** Distributed KeyValue Store with Akka.NET ***", "Simone Marrocco & Stefano Faccio");
            WriteLine();

            while (!stop)
            {
                ForegroundColor = coloreMenu;
                WriteLine("TestCases:");
                ForegroundColor = ConsoleColor.Gray;
                WriteLine("0) Exit");
                WriteLine("1) Get key");
                WriteLine("2) Update key");
                WriteLine("3) Create new node and join the network");
                WriteLine("4) Create new client");
                WriteLine("5) Node leave network");
                WriteLine("6) Crash");
                WriteLine("7) Recovery");
                WriteLine("8) See network overview");

                do
                {
                    ForegroundColor = coloreMenu;
                    Write("Select an option: ");
                    ForegroundColor = ConsoleColor.Gray;
                } while (!UInt32.TryParse(ReadLine(), out menuChoice));

                switch (menuChoice)
                {
                    case 0:
                        stop = true;
                        break;
                    case 1:
                        Case1();
                        break;
                    case 2:
                        Case2();
                        break;
                    case 3:
                        Case3();
                        break;
                    case 4:
                        Case4();
                        break;
                    case 5:
                        Case5();
                        break;
                    case 6:
                        Case6();
                        break;
                    case 7:
                        Case7();
                        break;
                    case 8:
                        Case8();
                        break;
                    default:
                        Clear();
                        break;
                }
                Thread.Sleep(500);
                WriteLine();
            }
        }

        static void PrintHeader(string title, string subtitle, ConsoleColor backgroundColor = ConsoleColor.DarkBlue)
        {
            //Imposto il titolo
            Title = "Distributed KeyValue Store with Akka.NET";

            //Imposto il colore di background
            BackgroundColor = backgroundColor;

            char hor = '═';
            char ver = '║';
            int myWindowWidth = WindowWidth - 1;

            string start = "╔" + new string(hor, (myWindowWidth - 2)) + "╗";
            string end = "╚" + new string(hor, (myWindowWidth - 2)) + "╝";
            string newLine = ver + new string(' ', (myWindowWidth - 2)) + ver;

            string preTitle = ver + new string(' ', (int)Math.Ceiling((double)(myWindowWidth - title.Length - 2) / 2));
            string preSubtitle = ver + new string(' ', (int)Math.Ceiling((double)(myWindowWidth - subtitle.Length - 2) / 2));
            string postTitle = new string(' ', (int)Math.Floor((double)(myWindowWidth - title.Length - 2) / 2)) + ver;
            string postSubtitle = new string(' ', (int)Math.Floor((double)(myWindowWidth - subtitle.Length - 2) / 2)) + ver;

            WriteLine(start);
            WriteLine(newLine);
            WriteLine(newLine);
            WriteLine(newLine);
            WriteLine(newLine);

            Write(preTitle);
            ForegroundColor = ConsoleColor.White;
            Write(title);
            ForegroundColor = ConsoleColor.Gray;
            WriteLine(postTitle);
            WriteLine(newLine);

            Write(preSubtitle);
            ForegroundColor = ConsoleColor.White;
            Write(subtitle);
            ForegroundColor = ConsoleColor.Gray;
            WriteLine(postSubtitle);

            WriteLine(newLine);
            WriteLine(newLine);
            WriteLine(newLine);
            WriteLine(newLine);
            WriteLine(end);

            ResetColor();
        }
    }
}
*/