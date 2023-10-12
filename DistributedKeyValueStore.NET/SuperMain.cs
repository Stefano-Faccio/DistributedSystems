using Akka.Actor;

namespace DistributedKeyValueStore.NET
{
    internal class SuperMain
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            // Creazione contenitore per gli actors
            ActorSystem system = ActorSystem.Create("povoland");

            //Cliente singolo
            IActorRef client = system.ActorOf<Client>("client");
            //Lista attori temporanei
            IActorRef[] attori = new IActorRef[2];

            for ( uint i = 0; i < attori.Length; i++ )
            {
                attori[i] = system.ActorOf<Node>((i*10).ToString());
                attori[i].Tell(new StartMessage(i*10));
            }

            for (uint i = 0; i < attori.Length; i++)
                for (uint y = 0; y < attori.Length; y++)
                    if(i != y)
                        system.ActorSelection($"/user/{y*10}").Tell(new AddNodeMessage(i *10));

            Thread.Sleep(1000);
            system.ActorSelection($"/user/client").Tell(new GetMessage(5));

            Console.ReadKey();
        }
    }
}