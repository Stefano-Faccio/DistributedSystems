using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    //Funzioni per rimuovere il nodo dalla rete e spegnerlo
    internal partial class Node : UntypedActor, IWithTimers
    {
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

        protected void Stop(StopMessage message)
        {
            if (active)
            {
                //Mando un messaggio a tutti gli altri nodi con cui mi rimuovo dalle loro liste nodi
                Context.ActorSelection("/user/*").Tell(new RemoveNodeMessage(this.Id));

                //Imposto un timer per avere tempo di completare tutte le richieste in corso
                Timers.StartSingleTimer($"Shutdown{myMersenneTwister.Next()}", new TimeoutShutdownMessage(), TimeSpan.FromMilliseconds(2*TIMEOUT_TIME));
            }
            else
                throw new Exception("Node not initialided");
        }

        protected void OnShutdownTimout(TimeoutShutdownMessage message)
        {
            //Tutte le operzioni che erano in corso dovrebbero essere state completate
            //Non dovrei avere altri messaggi in coda poichè sono passati 2 TIMEOUT da quando il nodo non è più nella rete
            //Posso mandare safe tutti i valori che ho agli altri nodi

            //Struttura dati per salvarsi per ogni nodo tutte le chiavi/valori da mandare
            Dictionary<uint, List<(uint, Document)>> bulkWrite = new();

            //Per ogni chiave che ho
            data.KeyCollection().ForEach(key =>
            {
                //Prendo i nodi che hanno quella chiave
                List<uint> nodesWithKey = FindNodesThatKeepKey(key);
                //Per ogni nodo che ha la chiave
                nodesWithKey.ForEach(node =>
                {
                    Document nowData = data[key] ?? throw new Exception($"Data with key {key} not present in the db");
                    //Se la lista delle chiavi è già presente
                    if (bulkWrite.TryGetValue(node, out List<(uint, Document)>? keys))
                        keys.Add((key, nowData));
                    else
                        bulkWrite.Add(node, new List<(uint, Document)> { (key, nowData) });
                });
            });

            bulkWrite.ForEach(kvp =>
            {
                if (sendDebug)
                    lock (Console.Out)
                    {
                        Console.WriteLine($"{Self.Path.Name} sended BULK WRITE to node{kvp.Key} => ");
                        kvp.Value.ForEach(tupla => Console.WriteLine($"\t{tupla.Item1} -> {tupla.Item2}"));
                    }
                        
                //Se la lista da mandare non è vuota mando una bulk write ad ogni nodo
                if (kvp.Value.Count > 0)
                    Context.ActorSelection($"/user/node{kvp.Key}").Tell(new BulkWriteMessage(new(kvp.Value)), Self);
            });

            DeactivateNode();
        }

        protected void BulkWrite(BulkWriteMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} received BULK WRITE from {Sender.Path.Name} => ");
                    message.KeyValuesList.ForEach(tupla => Console.WriteLine($"\t{tupla.Item1} -> {tupla.Item2}"));
                }

            //Scrivo i valori mandati dal nodo che se ne è andato
            message.KeyValuesList.ForEach(tupla =>
            {
                uint key = tupla.Item1;
                Document doc = tupla.Item2;
                //Controllo della versione viene fatta nella classe Collection
                data.Add(key, doc);
            });
        }

        protected void RemoveNode(RemoveNodeMessage message)
        {
            //Un nodo vuole andarsene
            //Lo rimuovo dalla lista dei nodi
            nodes.Remove(message.Id);

            if (deepDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} removed node{message.Id}");

            //N.B.
            //Forse è più sensato che ogni nodo a questo punto richieda gli elementi che gli mancano
        }

        private void DeactivateNode()
        {
            if (!active)
                throw new Exception($"{Self.Path.Name} not active!");

            //Mi disattivo
            active = false;

            if (generalDebug)
                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{Self.Path.Name} leave the network succesfully");
                    Console.ResetColor();
                }

            //Stoppo l'attore
            Context.Stop(Self);
        }
    }
}
