using Akka.Actor;
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

            Test();
        }

        static void Test()
        {
            //Numero di attori iniziali
            const uint NATTORI = 7;
            const int NCLIENTS = 3;

            ForegroundColor = ConsoleColor.Blue;
            WriteLine("Startup: ");
            ResetColor();

            // Creazione contenitore per gli actors
            ActorSystem system = ActorSystem.Create("povoland");

            //Cliente singolo
            List<IActorRef> clients = new();
            for (int i = 0; i < NCLIENTS; i++)
            {
                clients.Add(system.ActorOf<Client>("client_" + i));
            }
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
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(6));
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(6));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nUpdate Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new UpdateMessage(6, "Comunisti con il Rolex!"));
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(6));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(6));
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(6));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nUpdate Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new UpdateMessage(6, "Forza Napoli"));
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(6));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(6));
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(6));

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
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new UpdateMessage(42, "H24 In gaina!"));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nUpdate Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new UpdateMessage(26, "Alla canna del gas!"));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(42));

            Thread.Sleep(500);
            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nGet Message: ");
            ResetColor();
            clients[Constants.myMersenneTwister.Next(NCLIENTS)].Tell(new GetMessage(26));

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
            foreach (var client in clients)
            {
                WriteLine(client.Path);
                client.Tell(new TestMessage());
            }

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
            foreach (var client in clients)
            {
                WriteLine(client.Path);
                client.Tell(new TestMessage());
            }

            ReadKey();
        }
    }
}
