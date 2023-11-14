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
        //Booleano che indica se sono durante il processo di leave della rete
        bool leaving = false;
        //Hashset per tenere i nodi a cui devo mandare i dati prima di fare il leave con chiavi/valori da mandare
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
            if (active && !leaving)
            {
                //Mi setto come leaving
                leaving = true;

                //Pulisco l'hashset dove salvo i nodi a cui mandare i dati
                dataToSendToLeave.Clear();

                //Mando un messaggio a tutti gli altri nodi con cui mi rimuovo dalle loro liste nodi
                Context.ActorSelection("/user/*").Tell(new RemoveNodeMessage(this.Id));

                //Imposto un timer per avere tempo di completare tutte le richieste in corso
                Timers.StartSingleTimer($"Shutdown{myMersenneTwister.Next()}", new TimeoutShutdownMessage(), TimeSpan.FromMilliseconds(2 * TIMEOUT_TIME));
            }
            else
                throw new Exception("Node not initialided or already leaving");
        }

        protected void OnShutdownTimout(TimeoutShutdownMessage message)
        {
            //Tutte le operzioni che erano in corso dovrebbero essere state completate
            //Non dovrei avere altri messaggi in coda poichè sono passati 2 TIMEOUT da quando il nodo non è più nella rete
            //Raccolgo tutti i dati da mandare agli altri nodi
            //E mando una richiesta per vedere se sono attivi

            //Per ogni chiave che ho
            data.KeyCollection().ForEach(key =>
            {
                //Prendo il nodo a cui mandare il dato
                uint nodeWithKey = FindNodesThatKeepKey(key).Except(FindNodesThatKeepKey(key, this.Id)).First();

                Document nowData = data[key] ?? throw new Exception($"Data with key {key} not present in the db");
                //Se la lista delle chiavi è già presente
                if (dataToSendToLeave.TryGetValue(nodeWithKey, out List<(uint, Document)>? keys))
                    keys.Add((key, nowData));
                else
                    dataToSendToLeave.Add(nodeWithKey, new List<(uint, Document)> { (key, nowData) });
            });

            //Mando una richiesta per vedere se tutti gli altri nodi sono attivi
            dataToSendToLeave.ForEach(kvp =>
            {
                if (sendDebug)
                    lock (Console.Out)
                    {
                        Console.WriteLine($"{Self.Path.Name} sended REQUEST TO LEAVE to node{kvp.Key}");
                    }

                Context.ActorSelection($"/user/node{kvp.Key}").Tell(new RequestToLeaveMessage(kvp.Key), Self);
            });

            //Imposto un timeout per controllare che tutti i nodi abbiano risposto
            Timers.StartSingleTimer($"Leave{myMersenneTwister.Next()}", new TimeoutLeaveMessage(), TimeSpan.FromMilliseconds(2 * TIMEOUT_TIME));
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
            Sender.Tell(new RequestToLeaveResponseMessage(message.Id, response), Self);

            if (sendDebug)
                lock (Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} sended REQUEST TO LEAVE RESPONSE to {Sender.Path.Name} => Value: {response}");
                }
        }

        protected void RequestToLeaveResponse(RequestToLeaveResponseMessage message)
        {
            //Se trovo i dati nel dizionario

            //Prendo i valori da inviare
            if(dataToSendToLeave.TryGetValue(message.Id, out List<(uint, Document)>? sendingData))
            {
                if (receiveDebug)
                    lock (Console.Out)
                    {
                        Console.WriteLine($"{Self.Path.Name} received REQUEST TO LEAVE RESPONSE from {Sender.Path.Name} => Value: {message.Success}");
                    }

                if (sendDebug)
                    lock (Console.Out)
                    {
                        Console.WriteLine($"{Self.Path.Name} sended BULK WRITE to {Sender.Path.Name} => ");
                        sendingData.ForEach(tupla => Console.WriteLine($"\t{tupla.Item1} -> {tupla.Item2}"));
                    }

                //Mando una bulk write
                Sender.Tell(new BulkWriteMessage(new(sendingData)), Self);

                //Elimino il valore dal dizionario
                dataToSendToLeave.Remove(message.Id);

                //Se il dizionario è vuoto significa che posso disattivarmi con successo
                if (dataToSendToLeave.Count == 0)
                    DeactivateNode(true);
            }
            else
            {
                //La richiesta di leave è caduta in timeout
                if (receiveDebug)
                    lock (Console.Out)
                    {
                        Console.WriteLine($"{Self.Path.Name} received REQUEST TO LEAVE RESPONSE (IGNORED) from {Sender.Path.Name}");
                    }
            }
        }

        protected void OnLeaveTimout(TimeoutLeaveMessage message)
        {
            if (generalDebug)
                lock(Console.Out)
                {
                    Console.WriteLine($"{Self.Path.Name} LEAVE TIMEOUT achieved");
                }

            //Ripristino il normale funzioanamento del nodo
            DeactivateNode(false);
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
        }

        private void DeactivateNode(bool successo)
        {
            if (!active || !leaving)
                throw new Exception($"{Self.Path.Name} not active or not leaving the network!");

            //Fine della leave
            leaving = false;

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
