using Akka.Actor;
using MathNet.Numerics.Random;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    internal class SuperMain
    {
        static void Main(string[] args)
        {
            Test();
        }

        static void Menu()
        {
            uint menuChoice;
            bool stop = false;

            while (!stop)
            {
                Console.WriteLine("Menu:");
                //TODO WRITE MENU
                Console.WriteLine("0) Exit");
                do
                {
                    Console.Write("Scegli un'opzione: ");
                } while (!UInt32.TryParse(Console.ReadLine(), out menuChoice));
                    

                switch (menuChoice) { 
                    case 0:
                        stop = true;
                        break;
                }
            }
        }

        static void Test()
        {
            //Numero di attori iniziali
            const uint NATTORI = 7;

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Startup: ");
            Console.ResetColor();

            // Creazione contenitore per gli actors
            ActorSystem system = ActorSystem.Create("povoland");

            //Cliente singolo
            IActorRef client = system.ActorOf<Client>("client");
            //Lista attori
            List<IActorRef> attori = new List<IActorRef>((int)NATTORI);

            //------------------------------------------------------------------------------------------

            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nFirst actors: ");
            Console.ResetColor();

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
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nGet Message: ");
            Console.ResetColor();
            client.Tell(new GetMessage(6));

            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nUpdate Message: ");
            Console.ResetColor();
            client.Tell(new UpdateMessage(6, "Comunisti con il Rolex!"));

            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nGet Message: ");
            Console.ResetColor();
            client.Tell(new GetMessage(6));

            //-------------------------------------------------------------

            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nAdd actors: ");
            Console.ResetColor();

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
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nUpdate Message: ");
            Console.ResetColor();
            client.Tell(new UpdateMessage(42, "H24 In gaina!"));

            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nUpdate Message: ");
            Console.ResetColor();
            client.Tell(new UpdateMessage(26, "Alla canna del gas!"));

            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nGet Message: ");
            Console.ResetColor();
            client.Tell(new GetMessage(42));

            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nGet Message: ");
            Console.ResetColor();
            client.Tell(new GetMessage(26));

            //----------------------------------------------------------------------

            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nAdd actors: ");
            Console.ResetColor();

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
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nStart Test:");
            Console.ResetColor();
            foreach (var att in attori)
            {
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }
            Console.WriteLine(client.Path);
            client.Tell(new TestMessage());

            //------------------------------------------------------------------------

            //Stop Node
            Thread.Sleep(500);
            int actorToStop = myMersenneTwister.Next(0, attori.Count);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nStop Node:");
            Console.ResetColor();
            attori[actorToStop].Tell(new StopMessage((uint)actorToStop));
            //attori.RemoveAt(actorToStop);

            //------------------------------------------------------------------------

            //Test
            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nStart Test:");
            Console.ResetColor();
            foreach (var att in attori)
            {
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }
            Console.WriteLine(client.Path);
            client.Tell(new TestMessage());

            Console.ReadKey();
        }
    }
}


//Format code with Ctrl + k Ctrl + d
