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
    //Funzioni per eseguire l'opzione get e read
    internal partial class Node : UntypedActor, IWithTimers
    {
        //Hashset per le richieste get
        readonly Dictionary<int, GetDataStructure> getRequestsData = new();

        protected void OnGet(GetMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received GET from {Sender.Path.Name} => Key:{message.Key}");

            //Genero un numero casuale id della richiesta GET
            int getID = myMersenneTwister.Next();

            //Alloco lo spazio e salvo Key e nome del nodo che ha fatto la richiesta
            if (!getRequestsData.TryAdd(getID, new GetDataStructure(message.Key, Sender.Path.Name, message.Identifier)))
                throw new Exception("Errors adding item to getRequestsData Dictionary");

            //Imposto un timer per inviare una risposta di timeout al client in caso di non raggiungimento del quorum
            Timers.StartSingleTimer($"Get{myMersenneTwister.Next()}", new TimeoutGetMessage(message.Key, getID, Sender), TimeSpan.FromMilliseconds(TIMEOUT_TIME));

            //Recupero i nodi che tengono quel valore
            List<uint> nodesWithValue = FindNodesThatKeepKey(message.Key);

            //Invio messaggi di READ a tutti gli altri nodi che hanno il valore
            foreach (uint node in nodesWithValue)
                Context.ActorSelection($"/user/node{node}").Tell(new ReadMessage(message.Key, getID));

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} sended READ to ALL NODES => Key:{message.Key}");
        }
        private void OnGetTimout(TimeoutGetMessage message)
        {
            //Rimuovo i dati della richiesta GET se esistono
            if (getRequestsData.Remove(message.GetId, out GetDataStructure? getRequestData))
            {
                //Se i dati della richiesta GET ci sono ancora significa che non è stata soddisfatta quindi invio un timeout al client
                //Invio la risposta di timeout
                message.Sender.Tell(new GetResponseMessage(message.Key, getRequestData.Identifier), Self);

                if (sendDebug)
                    lock (Console.Out)
                        Console.WriteLine($"{Self.Path.Name} sended GET RESPONSE (TIMEOUT) to {message.Sender.Path.Name} => Key:{message.Key}");
            }
            else if (deepDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} GET TIMEOUT not achieved => Key:{message.Key}");
            //Se getRequestData è null posso ignorare la risposta in quanto è già stata soddisfatta
        }

        protected void OnRead(ReadMessage message)
        {
            Document? fetchedData = data[message.Key];
            ReadResponseMessage responseMessage;
            //Se non ho l'oggetto della chiave, ritorno null
            if (fetchedData is not null)
                responseMessage = new(message.Key, fetchedData.Value, message.GetId, fetchedData.Version, fetchedData.PreWriteBlock);
            else
                responseMessage = new(message.Key, fetchedData?.Value, message.GetId);

            //Invio la risposta
            Sender.Tell(responseMessage, Self);
        }

        private void OnReadResponse(ReadResponseMessage message)
        {
            //Prendo i dati della richiesta GET se esistono
            if (getRequestsData.TryGetValue(message.GetId, out GetDataStructure? getRequestData))
            {
                //Aggiungo il valore ricevuto ai dati
                getRequestData.Add(message.Value, message.Version, message.PreWriteBlock);

                if (receiveDebug)
                    lock (Console.Out)
                        Console.WriteLine($"{Self.Path.Name} received READ RESPONSE from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value ?? "null"} Version: {message.Version} PreWriteBlock: {message.PreWriteBlock}");

                //Se reggiungo il quorum DEVO inviare una risposta
                if(getRequestData.CountResponses >= READ_QUORUM)
                {
                    //Rimuovo i dati dal dizionario
                    getRequestsData.Remove(message.GetId, out _);
                    //Cerco di prendere il valore da restituire alla GET
                    (string? returnValue, uint? returnVersion) = getRequestData.GetReturnValue();
                    
                    if (returnValue is not null && returnVersion is not null)
                    {
                        //Invio la risposta positiva al nodo 
                        Context.ActorSelection($"/user/{getRequestData.NodeName}").Tell(new GetResponseMessage(message.Key, returnValue, (uint)returnVersion, getRequestData.Identifier));

                        if (sendDebug)
                            Console.WriteLine($"{Self.Path.Name} sended GET RESPONSE (POSITIVE) to {Sender.Path.Name} => Key:{message.Key} Value:{returnValue ?? "null"}");
                    }
                    else
                    {
                        //Invio la risposta negativa al nodo
                        Context.ActorSelection($"/user/{getRequestData.NodeName}").Tell(new GetResponseMessage(message.Key, getRequestData.Identifier));

                        if (sendDebug)
                            Console.WriteLine($"{Self.Path.Name} sended GET RESPONSE (NEGATIVE) to {Sender.Path.Name} => Key:{message.Key} Value:{returnValue ?? "null"}");
                    }
                }
            }
            else if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received READ RESPONSE (IGNORED) from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value ?? "null"}");

            //Se getRequestData è null posso ignorare la risposta in quanto è già stata soddisfatta
        }
    }
}
