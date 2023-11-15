using Akka.Actor;
using MathNet.Numerics.Random;
using System.Drawing;
using System.Globalization;
using static DistributedKeyValueStore.NET.Constants;
using static System.Console;

namespace DistributedKeyValueStore.NET
{
    internal class SuperMain
    {
        //Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Menu();

            //Test();
        }

        static void Menu()
        {
            uint menuChoice;
            bool stop = false;
            ConsoleColor coloreMenu = ConsoleColor.DarkYellow;

            PrintHeader("*** Distributed KeyValue Store with Akka.NET ***", "Simone Marocco & Stefano Faccio");
            WriteLine();

            while (!stop)
            {
                ForegroundColor = coloreMenu;
                WriteLine("Menu:");
                ForegroundColor = ConsoleColor.Gray;
                WriteLine("0) Exit");
                WriteLine("1) Get key");
                WriteLine("2) Update key");
                WriteLine("3) Join network");
                WriteLine("3) Leave network");
                WriteLine("4) Create new node");
                WriteLine("4) Create new client");
                WriteLine("5) See network overview");

                do
                {
                    ForegroundColor = coloreMenu;
                    Write("Scegli un'opzione: ");
                    ForegroundColor = ConsoleColor.Gray;
                } while (!UInt32.TryParse(ReadLine(), out menuChoice));
                    

                switch (menuChoice) { 
                    case 0:
                        stop = true;
                        break;
                    case 1:
                        //Get
                        //client.Tell(new GetMessage(6));
                        break;
                    default:
                        Clear();
                        break;
                }
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

            string start = "╔" + new string(hor, (WindowWidth - 2)) + "╗";
            string end = "╚" + new string(hor, (WindowWidth - 2)) + "╝";
            string newLine = ver + new string(' ', (WindowWidth - 2)) + ver;
            
            string preTitle = ver + new string(' ', (int)Math.Ceiling((double)(WindowWidth - title.Length - 2) / 2));
            string preSubtitle = ver + new string(' ', (int)Math.Ceiling((double)(WindowWidth - subtitle.Length - 2) / 2));
            string postTitle = new string(' ', (int)Math.Floor((double)(WindowWidth - title.Length - 2) / 2)) + ver;
            string postSubtitle = new string(' ', (int)Math.Floor((double)(WindowWidth - subtitle.Length - 2) / 2)) + ver;

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

        static void Test()
        {
            //Numero di attori iniziali
            const uint NATTORI = 7;

            ForegroundColor = ConsoleColor.Blue;
            WriteLine("Startup: ");
            ResetColor();

            // Creazione contenitore per gli actors
            ActorSystem system = ActorSystem.Create("povoland");

            //Cliente singolo
            IActorRef client = system.ActorOf<Client>("client");
            //Lista attori
            List<IActorRef> attori = new List<IActorRef>((int)NATTORI);

            //------------------------------------------------------------------------------------------

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nFirst actors: ");
            ResetColor();

            for (uint i = 0; i < (NATTORI > N ? N : NATTORI); i++)
            {
                Thread.Sleep(500);
                IActorRef tmp = system.ActorOf<Node>("node" + (i * 10).ToString());
                int nodeToAsk = attori.Count > 0 ? Constants.myMersenneTwister.Next(attori.Count) : 0;
                //Messaggio di start (id, id del nodo a cui chiedere la lista dei nodi)
                tmp.Tell(new StartMessage(i * 10, (uint)nodeToAsk * 10));
                //Aggiungo l'attore alla lista del main
                attori.Add(tmp);
            }

            //----------------------------------------------------------------------------------------


            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            client.Tell(new GetMessage(6));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nUpdate Message: ");
            ResetColor();
            client.Tell(new UpdateMessage(6, "Comunisti con il Rolex!"));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            client.Tell(new GetMessage(6));

            //-------------------------------------------------------------

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nAdd actors: ");
            ResetColor();

            for (uint i = (uint)attori.Count; i < NATTORI - 2; i++)
            {
                Thread.Sleep(500);
                IActorRef tmp = system.ActorOf<Node>("node" + (i * 10).ToString());
                int nodeToAsk = attori.Count > 0 ? Constants.myMersenneTwister.Next(attori.Count) : 0;
                //Messaggio di start (id, id del nodo a cui chiedere la lista dei nodi)
                tmp.Tell(new StartMessage(i * 10, (uint)nodeToAsk * 10));
                //Aggiungo l'attore alla lista del main
                attori.Add(tmp);
            }

            //---------------------------------------------------------------------

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nUpdate Message: ");
            ResetColor();
            client.Tell(new UpdateMessage(42, "H24 In gaina!"));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nUpdate Message: ");
            ResetColor();
            client.Tell(new UpdateMessage(26, "Alla canna del gas!"));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            client.Tell(new GetMessage(42));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            client.Tell(new GetMessage(26));

            //----------------------------------------------------------------------

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nAdd actors: ");
            ResetColor();

            for (uint i = (uint)attori.Count; i < NATTORI; i++)
            {
                Thread.Sleep(500);
                IActorRef tmp = system.ActorOf<Node>("node" + (i * 10).ToString());
                int nodeToAsk = attori.Count > 0 ? Constants.myMersenneTwister.Next(attori.Count) : 0;
                //Messaggio di start (id, id del nodo a cui chiedere la lista dei nodi)
                tmp.Tell(new StartMessage(i * 10, (uint)nodeToAsk * 10));
                //Aggiungo l'attore alla lista del main
                attori.Add(tmp);
            }

            //------------------------------------------------------------------------

            //Test
            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nStart Test:");
            ResetColor();
            foreach (var att in attori)
            {
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }
            WriteLine(client.Path);
            client.Tell(new TestMessage());

            //------------------------------------------------------------------------

            //Stop Node
            Thread.Sleep(500);
            int actorToStop = myMersenneTwister.Next(0, attori.Count);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nStop Node:");
            ResetColor();
            attori[actorToStop].Tell(new StopMessage((uint)actorToStop));
            //attori.RemoveAt(actorToStop);

            //------------------------------------------------------------------------

            //Test
            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nStart Test:");
            ResetColor();
            foreach (var att in attori)
            {
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }
            WriteLine(client.Path);
            client.Tell(new TestMessage());

            ReadKey();
        }
    }
}


//Format code with Ctrl + k Ctrl + d
