using Akka.Actor;
using Akka.Actor.Dsl;
using MathNet.Numerics.Random;
using System.Xml.Linq;

namespace DistributedKeyValueStore.NET
{
    internal class SuperMain
    {
        //MersenneTwister per i numeri casuali
        public static MersenneTwister mersenneTwister = new MersenneTwister(Guid.NewGuid().GetHashCode());
        //Numero di attori iniziali
        const uint NATTORI = 5;
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Startup: ");
            Console.ResetColor();

            // Creazione contenitore per gli actors
            ActorSystem system = ActorSystem.Create("povoland");

            //Cliente singolo
            IActorRef client = system.ActorOf<Client>("client");
            //Lista attori
            List<IActorRef> attori = new List<IActorRef>((int)NATTORI);

            for ( uint i = 0; i < NATTORI; i++)
            {
                Thread.Sleep(100);
                IActorRef tmp = system.ActorOf<Node>("node" + (i * 10).ToString());
                int nodeToAsk = attori.Count > 0 ? mersenneTwister.Next(attori.Count) : 0;
                //Messaggio di start (id, id del nodo a cui chiedere la lista dei nodi)
                tmp.Tell(new StartMessage(i*10, (uint)nodeToAsk * 10));
                //Aggiungo l'attore alla lista del main
                attori.Add(tmp);
            }

            //----------------------------------------------------------------------------------------

            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nMessaggi: ");
            Console.ResetColor();

            client.Tell(new GetMessage(69));
            //client.Tell(new GetMessage(6));

            //-----------------------------------------------------------------------------------------

            //Test
            /*
            Thread.Sleep(500);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nStart Test:");
            Console.ResetColor();
            foreach(var att in attori)
            {
                Console.WriteLine(att.Path);
                att.Tell(new TestMessage());
                Thread.Sleep(100);
            }
            Console.WriteLine(client.Path);
            client.Tell(new TestMessage());
            */

            Console.ReadKey();
        }
    }
}

/*
Thread.Sleep(2000);
//Aggiungo un altro attore
var attNew = system.ActorOf<Node>("nodeNevio");
attNew.Tell(new StartMessage(1000));
Console.WriteLine(attNew.Path);
attNew.Tell(new TestMessage());
Thread.Sleep(1000);
*/
