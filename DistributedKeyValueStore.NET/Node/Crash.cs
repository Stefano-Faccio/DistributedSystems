using Akka.Actor;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    internal partial class Node : UntypedActor, IWithTimers
    {
        protected void OnCrash(CrashMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} has crashed");

            crashed = true;
        }

        protected void OnRecovery(RecoveryMessage message)
        {
            if (!crashed)
                throw new Exception("Node not crashed cannot recover!");

            if (generalDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} is back online and recoverying...");
            
            //Chiedo la lista di nodi attivi
            Context.ActorSelection($"/user/node{message.NodeToContactForList}").Tell(new GetNodeListMessage(this.Id, RequestIdentifier.RECOVERY));

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} request GET NODE LIST to node {message.NodeToContactForList}");
        }

        protected void RecoveryGetNodeListResponse(GetNodeListResponseMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} response GET NODE LIST CRASH => Values:[{string.Join(",", message.Nodes)}]");

            //Sovrascrivo la lista dei nodi che ho
            nodes = new(message.Nodes);

            //Elimino dal mio db tutte le chiavi di cui non sono più responsabile
            RemoveElementsNoResponsible();

            //Prendo il nodo successiovo e quello precedente
            uint prev_node = PreviousNode();
            uint next_node = NextNode();

            //Domando le chiavi al nodo precedente ed a quello successivo 
            Context.ActorSelection($"/user/node{prev_node}").Tell(new GetKeysListMessage(this.Id, RequestIdentifier.RECOVERY));
            Context.ActorSelection($"/user/node{next_node}").Tell(new GetKeysListMessage(this.Id, RequestIdentifier.RECOVERY));

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} sended GET KEYS LIST to Node{prev_node} and Node{next_node}");
        }

        protected void GetKeysListResponseRecovery(GetKeysListResponseMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received GET KEY LIST RESPONSE => Values:[{string.Join(",", message.KeysList)}]");

            //Faccio una get di tutte le chiavi che non ho
            message.KeysList.ForEach(key =>
            {
                if(!data.ContainsKey(key))
                {
                    if (sendDebug)
                        lock (Console.Out)
                            Console.WriteLine($"{Self.Path.Name} sended GET KEY to {Sender.Path.Name} => Value:{key}");
                    Sender.Tell(new GetMessage(key, RequestIdentifier.RECOVERY));
                }
            });

            Timers.StartSingleTimer($"Recovery{myMersenneTwister.Next()}", new BackOnlineMessage(this.Id), TimeSpan.FromMilliseconds(TIMEOUT_TIME));
        }

        protected void GetResponseRecovery(GetResponseMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{Self.Path.Name} received GET KEY RESPONSE (CRASH RECOVERY) from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value}");
                    Console.ResetColor();
                }

            if(message.Value is not null)
                data.Add(message.Key, message.Value, message.Version);
        }

        protected void BackOnline(BackOnlineMessage message)
        {
            if (generalDebug && crashed)
                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{Self.Path.Name} is back online");
                    Console.ResetColor();
                }  

            //Riprendo ad elaborare i messaggi
            crashed = false;
        }
    }
}
