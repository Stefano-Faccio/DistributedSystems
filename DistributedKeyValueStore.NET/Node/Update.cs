using Akka.Actor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    //Funzioni per eseguire l'opzione update, write e prewrite
    internal partial class Node : UntypedActor, IWithTimers
    {
        //Hashset per le richieste update, i valori nella lista sono le versioni che vengono ritornate dal prewrite message
        readonly Dictionary<uint, List<uint>> updateRequestsData = new();

        protected void OnUpdate(UpdateMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received UPDATE from {Sender.Path.Name} => Key:{message.Key}, Value: {message.Value}");

            //Recupero i nodi che tengono quel valore
            List<uint> nodesWithValue = FindNodesThatKeepKey(message.Key);

            //Imposto un timer per inviare una risposta di timeout al client in caso di non raggiungimento del quorum
            Timers.StartSingleTimer($"Update{MersenneTwister.Next()}", new TimeoutUpdateMessage(message.Key, message.Value, new(nodesWithValue), Sender), TimeSpan.FromMilliseconds(TIMEOUT_TIME));

            //Invio messaggi di PREWRITE a tutti gli altri nodi che hanno il valore
            foreach (uint node in nodesWithValue)
                Context.ActorSelection($"/user/node{node}").Tell(new PreWriteMessage(message.Key));

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} sended PREWRITE to ALL NODES => Key:{message.Key}");
        }

        private void OnUpdateTimout(TimeoutUpdateMessage message)
        {
            if(updateRequestsData.Remove(message.Key, out _))
            {
                //Messaggio di risposta negativo al client
                message.Sender.Tell(new UpdateResponseMessage(message.Key, message.Value, false), Self);

                if (generalDebug)
                    lock (Console.Out)
                        Console.WriteLine($"{Self.Path.Name} UPDATE did not receive enough positive responses, aborting => Key:{message.Key}");
            }
            else if (deepDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} UPDATE TIMEOUT not achieved => Key:{message.Key} Value:{message.Value}");
        }

        protected void OnPreWrite(PreWriteMessage message)
        {
            Document? doc = data[message.Key];
            if (doc is null)
            {
                // Non abbiamo il valore salvato, quindi per noi va bene aggiungerlo
                Sender.Tell(new PreWriteResponseMessage(message.Key, true, 0), Self);
            }
            else
            {
                bool preWriteBlock = doc.GetPreWriteBlock();
                if (preWriteBlock)
                {
                    // Siamo gia' in prewrite per questa chiave, quindi diciamo che non possiamo aggiornare
                    Sender.Tell(new PreWriteResponseMessage(message.Key, false, 0), Self);
                }
                else
                {
                    //Il Timeout per resettare prewriteblock è settato con questa funzione
                    doc.SetPreWriteBlock();
                    // Va bene aggiornare il valore per noi
                    Sender.Tell(new PreWriteResponseMessage(message.Key, true, doc.Version), Self);
                }
            }
        }

        protected void OnPreWriteResponse(PreWriteResponseMessage message)
        {
            //Se abbiamo ricevuto una risposta positiva
            if (message.Result)
            {
                if (receiveDebug)
                    lock (Console.Out)
                        Console.WriteLine($"{Self.Path.Name} received PREWRITE RESPONSE from {Sender.Path.Name} => Key:{message.Key} Result:{message.Result}");

                /*
            //Rimuovo i dati della richiesta UPDATE se esistono
            if (updateRequestsData.Remove(message.Key, out List<uint>? updateRequestDataForKey))
            {
                int count = updateRequestDataForKey.Count;
                if (count >= WRITE_QUORUM)
                {
                    // manda WRITE message a tutti
                    foreach (uint node in message.NodesWithValue)
                        Context.ActorSelection($"/user/node{node}").Tell(new WriteMessage(message.Key, message.Value, updateRequestDataForKey.Max() + 1));

                    //Messaggio di risposta positivo al client
                    message.Sender.Tell(new UpdateResponseMessage(message.Key, message.Value, true), Self);

                    if (sendDebug)
                        lock (Console.Out)
                            Console.WriteLine($"{Self.Path.Name} sended WRITE to ALL NODES => Key:{message.Key}, New Value: {message.Value}");
                }
            }*/


                //Se il valore è nel dizionario aggiungo, altrimenti creo la lista ed aggiungo il valore
                if (updateRequestsData.TryGetValue(message.Key, out List<uint>? updateList))
                    updateList.Add(message.Version);
                else
                    updateRequestsData.Add(message.Key, new List<uint> { message.Version });
            }
        }

        protected void OnWrite(WriteMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received WRITE => Key:{message.Key}, Value:{message.Value}, Version:{message.Version}");

            //La Write è forzata
            if (message.Value is not null)
                data.Add(message.Key, message.Value, message.Version, false);
        }
    }
}
