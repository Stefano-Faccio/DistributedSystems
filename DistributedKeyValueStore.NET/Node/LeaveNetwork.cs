using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util.Internal;
using DistributedKeyValueStore.NET.Data_Structures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    //Funzioni per rimuovere il nodo dalla rete e spegnerlo
    internal partial class Node : UntypedActor, IWithTimers
    {
        //Intero che indica se sono durante il processo di leave della rete
        //-1 Indica leave non in corso, un altro numero indica leave in corso
        int leaveId = -1;
        //Hashset per tenere i nodi a cui dovrei mandare i dati prima di fare il leave con chiavi/valori da mandare
        readonly Dictionary<uint, List<(uint, Document)>> dataToSendToLeave = new();
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
            //Se attivo e non sta già cercando di lasciare la rete
            if (active && leaveId == -1)
            {
                //Mi setto come leaving genrando l'id di leaving
                leaveId = myMersenneTwister.Next();

                //Pulisco l'hashset dove salvo i nodi a cui mandare i dati
                dataToSendToLeave.Clear();

                //Mando un messaggio a tutti gli altri nodi con cui mi rimuovo dalle loro liste nodi
                Context.ActorSelection("/user/*").Tell(new RemoveNodeMessage(this.Id));

                //Imposto un timer per avere tempo di completare tutte le richieste in corso
                Timers.StartSingleTimer($"Shutdown1{leaveId}", new TimeoutShutdown1Message(), TimeSpan.FromMilliseconds(2 * TIMEOUT_TIME));

                if (generalDebug)
                    lock (Console.Out)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"{Self.Path.Name} started the process of leaving the network...");
                        Console.ResetColor();
                    }
            }
            else
                throw new Exception("Node not initialided or already leaving");
        }

        protected void OnShutdownTimout1(TimeoutShutdown1Message message)
        {
            //Tutte le operzioni che erano in corso dovrebbero essere state completate
            //Non dovrei avere altri messaggi in coda poichè sono passati 2 TIMEOUT da quando il nodo non è più nella rete
            //Raccolgo tutti i dati da mandare agli altri nodi
            //E mando una richiesta per vedere se sono attivi

            if (generalDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} SHUTDOWN MANDATORY TIMEOUT achieved");
                }

            //Per ogni chiave che ho
            data.KeyCollection().ForEach(key =>
            {
                //Prendo il nodo a cui mandare il dato
                //Lo calcolo prendendo i nodi che dovrebbero tenere la chiave me stesso escluso MENO i nodi che dovrebbero tenere la chiave me stesso incluso
                uint nodeWithoutKey = FindNodesThatKeepKey(key).Except(FindNodesThatKeepKey(key, this.Id)).First();

                //Prendo il dato da mandare dal db
                //Lo aggiungo al dizionario dei dati da mandare
                //DIzionario: nodeToSendData => Lista<Key, Value>
                Document nowData = data[key] ?? throw new Exception($"Data with node {key} not present in the db");
                //Se la lista delle chiavi è già presente
                if (dataToSendToLeave.TryGetValue(nodeWithoutKey, out List<(uint, Document)>? keys))
                    keys.Add((key, nowData));
                else
                    dataToSendToLeave.Add(nodeWithoutKey, new List<(uint, Document)> { (key, nowData) });
            });

            //Mando una richiesta per vedere se tutti i nodi a cui dovrei mandare i dati sono attivi
            dataToSendToLeave.Keys.ForEach(key =>
            {
                if (sendDebug)
                    lock (Console.Out)
                    {
                        Console.WriteLine($"{Self.Path.Name} sended REQUEST TO LEAVE to node{key}");
                    }

                Context.ActorSelection($"/user/node{key}").Tell(new RequestToLeaveMessage(key, leaveId), Self);
            });

            //Imposto un timeout per controllare che tutti i nodi abbiano risposto
            //N.B. Se tutti i nodi rispondono prima del timeout il nodo si spegne quindi il timeout non viene processato
            Timers.StartSingleTimer($"Shutdown2{leaveId}", new TimeoutShutdown2Message(), TimeSpan.FromMilliseconds(TIMEOUT_TIME));
        }

        protected void OnShutdownTimout2(TimeoutShutdown2Message message)
        {
            //E' scaduto il timeout per le request to leave
            //C'è almeno 1 nodo a cui dovrei mandare i dati che non ha risposto
            //Posso ancora fare il leave se senza di me c'è il read quorum per tutte le chiavi che avrebbero dovuto tenere i nodi che non hanno risposto

            if (generalDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} REQUEST TO LEAVE TIMEOUT achieved");
                }

            //In questo caso faccio una get delle chiavi che non sono riuscito a soddisfare
            //Se ricevo la vecchia risposta nel frattempo o la risposta alla get significa che posso fare il leave tranquillamente
            dataToSendToLeave.Keys.ForEach(node =>
            {
                if(dataToSendToLeave.TryGetValue(node, out List<(uint, Document)>? keys))
                {
                    keys.ForEach(foo =>
                    {
                        uint key = foo.Item1;
                        if (sendDebug)
                            lock (Console.Out)
                            {
                                Console.WriteLine($"{Self.Path.Name} sendend SHUTDOWN GET to {Self.Path.Name} => Key:{key}");
                            }

                        Self.Tell(new GetMessage(key, RequestIdentifier.SHUTDOWN));
                    });
                }
            });

            //Imposto un timeout per controllare di ricevere tutte le risposte
            Timers.StartSingleTimer($"Shutdown3{leaveId}", new TimeoutShutdown3Message(), TimeSpan.FromMilliseconds(TIMEOUT_TIME));
        }

        protected void OnShutdownTimout3(TimeoutShutdown3Message message)
        {
            //In questo caso non c'è modo in cui possa lasciare la rete in modo sicuro
            //Ripristino il normale funzioanamento del nodo
            //Leave fallita

            if (generalDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} GET SHUTDOWN TIMEOUT achieved");
                }

            DeactivateNode(false);
        }

        protected void RequestToLeave(RequestToLeaveMessage message)
        {
            //Ho ricevuto una richiesta di un nodo che vuole leavare la rete
            if (receiveDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} received REQUEST TO LEAVE from {Sender.Path.Name}");
                }

            //Visto che sono attivo rispondo positivamente
            bool response = true;
            Sender.Tell(new RequestToLeaveResponseMessage(message.Id, message.LeaveId, response), Self);

            if (sendDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} sended REQUEST TO LEAVE RESPONSE to {Sender.Path.Name} => Value: {response}");
                }
        }

        protected void RequestToLeaveResponse(RequestToLeaveResponseMessage message)
        {
            //Ricevo una risposta alla richiesta di leave
            //Se positiva mando tutti i dati al nodo che so essere attivo

            uint nodeToSendData = message.Id;
            //Se la risposta è relativa alla richiesta di leave attuale
            //E la risposta è positiva
            //prendo i valori da inviare
            if (message.LeaveId == this.leaveId && message.Success && dataToSendToLeave.TryGetValue(nodeToSendData, out List<(uint, Document)>? sendingData))
            {
                if (receiveDebug)
                    lock (Console.Out)
                    {
                        Console.WriteLine($"{Self.Path.Name} received REQUEST TO LEAVE RESPONSE from {Sender.Path.Name} => Value: {message.Success}");
                    }

                //Mando i dati
                if (sendDebug)
                    lock (Console.Out)
                    {
                        Console.WriteLine($"{Self.Path.Name} sended BULK WRITE to {Sender.Path.Name} => ");
                        sendingData.ForEach(tupla => Console.WriteLine($"\t{tupla.Item1} -> {tupla.Item2}"));
                    }

                //Mando una bulk write 
                //Ossia mando tutti i dati (di sua competenza) accorpati al nodo
                Sender.Tell(new BulkWriteMessage(new(sendingData)), Self);

                //Elimino il valore dal dizionario
                dataToSendToLeave.Remove(nodeToSendData);

                //Se il dizionario è vuoto significa che non devo mandare più dati a nessun nodo
                //Quindi posso disattivarmi con successo
                if (dataToSendToLeave.Count == 0)
                    DeactivateNode(true);
            }
            else
            {
                //La richiesta di leave è caduta in timeout
                //Oppure ho già ricevuto risposte get sufficienti a soddisfare tutte le chiavi
                if (receiveDebug)
                    lock (Console.Out)
                    {
                        Console.WriteLine($"{Self.Path.Name} received REQUEST TO LEAVE RESPONSE (IGNORED) from {Sender.Path.Name}");
                    }
            }
        }

        protected void GetResponseShutdown(GetResponseMessage message)
        {
            //Ricevo una risposta get per vedere se posso disattivarmi

            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received SHUTDOWN GET RESPONSE from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value} Success:{!message.Timeout}");

            //Se la richiesta get ha avuto successo
            if(!message.Timeout)
            {
                //Rimuovo i dati dal dizionario
                //Non è necessario inviare questi dati

                //Passo tutti i dati che avrei dovuto inviare
                //Per ogni nodo
                //  Per ogni chiave controllo che questa sia uguale a quella del messaggio
                //  Se cosi' è posso rimuoverla perchè non serve inviarla
                //  Se la lista risulata vuota allora posso togliere il nodo dal dizionario
                dataToSendToLeave.Keys.ForEach(node =>
                {
                    if(dataToSendToLeave.TryGetValue(node, out List<(uint, Document)>? keyValuePairs))
                    {
                        for (int i = 0; i < keyValuePairs.Count; i++)
                        {
                            uint key = keyValuePairs[i].Item1;
                            if (key == message.Key)
                            {
                                keyValuePairs.RemoveAt(i);
                                --i;

                                if (deepDebug)
                                    lock (Console.Out)
                                        Console.WriteLine($"{Self.Path.Name} (SHUTDOWN) removed key={key} from data to send to leave");
                            }
                        }

                        if(keyValuePairs.Count  == 0)
                        {

                            if (deepDebug)
                                lock (Console.Out)
                                    Console.WriteLine($"{Self.Path.Name} (SHUTDOWN) removed node={node} from data to send to leave");

                            dataToSendToLeave.Remove(node);
                        }
                    }
                });

                //Se il dizionario è vuoto allora posso disattivarmi con successo
                if (dataToSendToLeave.Count == 0)
                    DeactivateNode(true);
            }
        }

        protected void BulkWrite(BulkWriteMessage message)
        {
            //Ricevo una write forzata di dati multipli
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
        }

        private void DeactivateNode(bool successo)
        {
            if (!active || leaveId == -1)
                throw new Exception($"{Self.Path.Name} not active or not leaving the network!");

            //Fine della leave
            leaveId = -1;

            //Se sono riuscito con successo a leavare la rete
            if (successo)
            {
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
            else
            {
                //Altrimenti rispristino il normale funzionamento
                if (generalDebug)
                    lock (Console.Out)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{Self.Path.Name} was not able to leave the network succesfully");
                        Console.ResetColor();
                    }

                //Mi (ri)annuncio a tutti gli altri nodi (me stesso compreso)
                Context.ActorSelection("/user/*").Tell(new AddNodeMessage(this.Id));
            }
        }
    }
}
