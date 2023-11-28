
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
        List<IActorRef> nodi;
        List<IActorRef> clients;
        List<uint> chiaviInserite;
        ActorSystem system;

        List<string> citazioni = new()
        {
            "640K ought to be enough for anybody",
            "Lo scopriremo solo vivendo",
            "Houston, we have a problem",
            "Dignitosamente... brillo!",
            "Fatti non foste a viver come bruti, ma per seguir virtute e canoscenza.",
            "Vivi e lascia vivere.",
            "Hello, World!",
            "It's not a bug, it's a feature.",
            "There are 10 types of people in the world: those who understand binary and those who don't.",
            "Uomini forti destini forti, uomini deboli destini deboli",
            "Troppo spesso la saggezza è la prudenza più stagnante."
        };

        static void Main(string[] args)
        {
            TestCases test = new TestCases();
        }

        public TestCases()
        {
            PrintHeader("*** Distributed KeyValue Store with Akka.NET ***", "Simone Marrocco & Stefano Faccio");
            WriteLine();

            lock (Console.Out)
            {
                BackgroundColor = ConsoleColor.DarkRed;
                WriteLine("\nSetup Network: ");
                WriteLine();
                ResetColor();
            }

            //Sovrascrivo il generatore casuale per rendere i risualtati ripetibili
            myMersenneTwister = new MathNet.Numerics.Random.MersenneTwister(2);

            //Creazione contenitore per gli actors
            system = ActorSystem.Create("povoland");
            //Inizializzo le liste per i nodi
            nodi = new();
            clients = new()
            {
                //Creo 2 client
                system.ActorOf<Client>("client1"),
                system.ActorOf<Client>("client2")
            };
            chiaviInserite = new();

            //Creo 10 nodi
            for (uint i = 0; i < 10; i++)
            {
                IActorRef tmp = system.ActorOf<Node>("node" + (i * 10).ToString());
                int nodeToAsk = nodi.Count > 0 ? Constants.myMersenneTwister.Next(nodi.Count) : 0;
                tmp.Tell(new StartMessage(i * 10, (uint)nodeToAsk * 10));
                nodi.Add(tmp);
                Thread.Sleep(100);

                lock (Console.Out)
                    WriteLine();
            }

            lock (Console.Out)
            {
                BackgroundColor = ConsoleColor.DarkRed;
                WriteLine("\nAsk random keys: ");
                ResetColor();
                WriteLine();
            }
            Console.ReadKey();

            //Chiedo un po' di chiavi che non ci sono
            for (int i = 0; i < 3; i++)
            {
                lock (Console.Out)
                {
                    ForegroundColor = ConsoleColor.Blue;
                    WriteLine("\nGet Message: ");
                    ResetColor();
                }
                clients[myMersenneTwister.Next(clients.Count)].Tell(new GetClientMessage((uint)myMersenneTwister.Next(nodi.Count * 11)));

                Thread.Sleep(100);
            }
            
            lock (Console.Out)
            {
                BackgroundColor = ConsoleColor.DarkRed;
                WriteLine("\nUpdate random keys: ");
                ResetColor();
                WriteLine();
            }
            Console.ReadKey();

            //Aggiungo un po' di chiavi
            for (int i = 0; i < 10; ++i)
            {
                lock (Console.Out)
                {
                    ForegroundColor = ConsoleColor.Blue;
                    WriteLine("\nUpdate Message: ");
                    ResetColor();
                }
                chiaviInserite.Add((uint)myMersenneTwister.Next(nodi.Count * 10));
                clients[myMersenneTwister.Next(clients.Count)].Tell(new UpdateClientMessage(chiaviInserite.Last(), citazioni[myMersenneTwister.Next(citazioni.Count)]));
                Thread.Sleep(100);
            }

            WriteLine();
            lock (Console.Out)
            {
                BackgroundColor = ConsoleColor.DarkRed;
                WriteLine("\nGet all keys inserted: ");
                ResetColor();
                WriteLine();
            }
            Console.ReadKey();

            for (int i = 0; i < chiaviInserite.Count; i++)
            {
                lock (Console.Out)
                {
                    ForegroundColor = ConsoleColor.Blue;
                    WriteLine("\nGet Message: ");
                    ResetColor();
                }
                clients[myMersenneTwister.Next(clients.Count)].Tell(new GetClientMessage(chiaviInserite[i]));
                Thread.Sleep(100);
            }

            WriteLine();
            lock (Console.Out)
            {
                BackgroundColor = ConsoleColor.DarkRed;
                WriteLine("\nPrint network : ");
                ResetColor();
                WriteLine();
            }
            Console.ReadKey();

            foreach (var att in nodi)
            {
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }

            WriteLine();
            lock (Console.Out)
            {
                BackgroundColor = ConsoleColor.DarkRed;
                WriteLine("\n 2 same time update for same key from 2 different clients : ");
                ResetColor();
                WriteLine();
            }
            Console.ReadKey();
            {
                uint key = chiaviInserite[myMersenneTwister.Next(chiaviInserite.Count)];

                clients[0].Tell(new UpdateClientMessage(key, citazioni[myMersenneTwister.Next(citazioni.Count)]));
                clients[1].Tell(new UpdateClientMessage(key, citazioni[myMersenneTwister.Next(citazioni.Count)]));
            }
            Thread.Sleep(100);
            foreach (var att in nodi)
            {
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }

            WriteLine();
            lock (Console.Out)
            {
                BackgroundColor = ConsoleColor.DarkRed;
                WriteLine("\n 2 same time update for same key from 2 different clients to the same node: ");
                ResetColor();
                WriteLine();
            }
            Console.ReadKey();
            {
                uint key = chiaviInserite[myMersenneTwister.Next(chiaviInserite.Count)];

                clients[0].Tell(new UpdateClientMessage(key, citazioni[myMersenneTwister.Next(citazioni.Count)]));
                clients[1].Tell(new UpdateClientMessage(key, citazioni[myMersenneTwister.Next(citazioni.Count)]));
            }
            Thread.Sleep(100);
            foreach (var att in nodi)
            {
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }

            WriteLine();
            lock (Console.Out)
            {
                BackgroundColor = ConsoleColor.DarkRed;
                WriteLine("\n a new node joining the network : ");
                ResetColor();
                WriteLine();
            }
            Console.ReadKey();
            {
                IActorRef tmp = system.ActorOf<Node>("node" + ((uint)nodi.Count * 10).ToString());
                int nodeToAsk = nodi.Count > 0 ? Constants.myMersenneTwister.Next(nodi.Count) : 0;
                tmp.Tell(new StartMessage((uint)nodi.Count*10, (uint)nodeToAsk * 10));
                nodi.Add(tmp);
                Thread.Sleep(100);
            }
            foreach (var att in nodi)
            {
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }

            WriteLine();
            lock (Console.Out)
            {
                BackgroundColor = ConsoleColor.DarkRed;
                WriteLine("\n a node leaving the network : ");
                ResetColor();
                WriteLine();
            }
            Console.ReadKey();
            {
                uint nodeId = (uint)Constants.myMersenneTwister.Next(nodi.Count);
                nodi[(int)nodeId].Tell(new StopMessage(nodeId));
            }
            Thread.Sleep(500);
            foreach (var att in nodi)
            {
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }

            Console.ReadKey();

            /*
             * TODO
             * CRASH
             * 
             * GET CON CRASH
             * UPDATE CON CRASH
             * LEAVE CON CRASH
             */
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
