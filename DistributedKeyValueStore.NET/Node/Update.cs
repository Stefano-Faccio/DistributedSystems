using Akka.Actor;
using DistributedKeyValueStore.NET.Data_Structures;
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
        readonly Dictionary<uint, UpdateDataStructure> updateRequestsData = new();

        protected void OnUpdate(UpdateMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received UPDATE from {Sender.Path.Name} => Key:{message.Key}, Value: {message.Value}");

            //Se ho già una richiesta UPDATE per questa chiave abortisco immediatamente
            if (updateRequestsData.ContainsKey(message.Key))
            {
                //Messaggio di risposta negativo al client
                Sender.Tell(new UpdateResponseMessage(message.Key, message.Value, false), Self);

                if (generalDebug)
                    lock (Console.Out)
                        Console.WriteLine($"{Self.Path.Name} UPDATE for the same key already exist, aborting => Key:{message.Key}");
            }
            else
            {
                //Genero un numero casuale id della nuova richiesta UPDATE
                int updateID = myMersenneTwister.Next();

                //Recupero i nodi che tengono il valore richiesto
                List<uint> nodesWithValue = FindNodesThatKeepKey(message.Key);

                //Aggiungo al dizionario la nuova richiesta con questa chiave
                updateRequestsData.Add(message.Key, new UpdateDataStructure(message.Key, message.Value, nodesWithValue, updateID, Sender));

                //Imposto un timer per inviare una risposta di timeout al client in caso di non raggiungimento del quorum
                Timers.StartSingleTimer($"Update{updateID}", new TimeoutUpdateMessage(message.Key, updateID), TimeSpan.FromMilliseconds(TIMEOUT_TIME));

                //Invio messaggi di PREWRITE a tutti gli altri nodi che hanno il valore
                foreach (uint node in nodesWithValue)
                    Context.ActorSelection($"/user/node{node}").Tell(new PreWriteMessage(message.Key, updateID));

                if (sendDebug)
                    lock (Console.Out)
                        Console.WriteLine($"{Self.Path.Name} sended PREWRITE to ALL NODES => Key:{message.Key}");
            }
        }

        private void OnUpdateTimout(TimeoutUpdateMessage message)
        {
            if(updateRequestsData.Remove(message.Key, out UpdateDataStructure? updateData))
            {
                //Messaggio di risposta negativo al client
                updateData.Sender.Tell(new UpdateResponseMessage(updateData.Key, updateData.Value, false), Self);

                if (generalDebug)
                    lock (Console.Out)
                        Console.WriteLine($"{Self.Path.Name} UPDATE TIMEOUT: did not receive enough positive responses, aborting => Key:{message.Key}");
            }
            else if (deepDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} UPDATE TIMEOUT not achieved => Key:{message.Key}");
        }

        protected void OnPreWrite(PreWriteMessage message)
        {
            Document? doc = data[message.Key];
            if (doc is null)
            {
                // Non abbiamo il valore salvato, quindi per noi va bene aggiungerlo
                Sender.Tell(new PreWriteResponseMessage(message.Key, message.UpdateId, true, 0), Self);
            }
            else
            {
                bool preWriteBlock = doc.GetPreWriteBlock();
                if (preWriteBlock)
                {
                    // Siamo gia' in prewrite per questa chiave, quindi diciamo che non possiamo aggiornare
                    Sender.Tell(new PreWriteResponseMessage(message.Key, message.UpdateId, false, 0), Self);
                }
                else
                {
                    //Il Timeout per resettare prewriteblock è settato con questa funzione
                    doc.SetPreWriteBlock();
                    // Va bene aggiornare il valore per noi
                    Sender.Tell(new PreWriteResponseMessage(message.Key, message.UpdateId, true, doc.Version), Self);
                }
            }
        }

        protected void OnPreWriteResponse(PreWriteResponseMessage message)
        {
            bool ignored = true;
            
            //Se il la key della risposta è presente nel dizionario
            //e il UpdateID è lo stesso
            if(updateRequestsData.TryGetValue(message.Key, out UpdateDataStructure? updateData) && updateData.UpdateId == message.UpdateId)
            {
                //Aggiorno il numero di risposte generiche
                updateData.Responses++;

                //Se la risposta è positiva
                if(message.Result)
                {
                    if (receiveDebug)
                        lock (Console.Out)
                            Console.WriteLine($"{Self.Path.Name} received PREWRITE RESPONSE (POSITIVE) from {Sender.Path.Name} => Key:{message.Key} Result:{message.Result}");

                    //Aggiungo la versione che ha l'altro nodeo
                    updateData.Versions.Add(message.Version);
                    //Marco questa risposta come non ignorata
                    ignored = false;

                    //Verifico se posso rispondere al client ed eventualmente rispondo
                    if (updateData.Versions.Count >= WRITE_QUORUM)
                    {
                        //mando WRITE message a tutti
                        foreach (uint node in updateData.NodesWithValue)
                            Context.ActorSelection($"/user/node{node}").Tell(new WriteMessage(updateData.Key, updateData.Value, updateData.Versions.Max() + 1));

                        //Messaggio di risposta positivo al client
                        updateData.Sender.Tell(new UpdateResponseMessage(updateData.Key, updateData.Value, true), Self);

                        if (sendDebug)
                            lock (Console.Out)
                                Console.WriteLine($"{Self.Path.Name} sended WRITE (QUORUM ACHIEVED) to ALL NODES => Key:{updateData.Key}, New Value: {updateData.Value}");

                        //Elimino i dati di questa richiesta dal dizionario
                        updateRequestsData.Remove(message.Key);
                    }
                }
            }

            if (ignored && receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received PREWRITE RESPONSE (IGNORED) from {Sender.Path.Name} => Key:{message.Key} Result:{message.Result}");
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
