using Akka.Actor;
using Akka.Util.Internal;
using MathNet.Numerics.Distributions;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    //Funzioni per inizializzare e aggiungere il nodo alla rete
    internal partial class Node : UntypedActor
    {
        //Hashset thread-safe per le read iniziali (numero di nodi che hanno quella chiave, numero di risposte per quella chiave ricevute)
        readonly ConcurrentDictionary<uint, (uint, uint)> inizializationsBulkReadsData = new();

        protected override void PreStart()
        {
            if (generalDebug)
                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"{Self.Path.Name} started the process of joining the network...");
                    Console.ResetColor();
                }
        }

        protected void Start(StartMessage message)
        {
            if (this.Id == uint.MaxValue)
            {
                //Questo è l'id univoco del nodo
                this.Id = message.Id;

                //Se non sono il primo nodo
                if (this.Id != message.AskNode)
                {
                    //Contatto un altro nodo a caso per recuperare la lista di nodi
                    ActorSelection receiver = Context.ActorSelection($"/user/node{message.AskNode}");

                    //Domando la lista dei nodi
                    receiver.Tell(new GetNodeListMessage(this.Id), Self);
                }
                else
                    //Attivo il nodo e lo aggiugno alla rete
                    ActivateNode();
            }
            else
                throw new Exception("Node already initialided");
        }

        private void GetNodeList(GetNodeListMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received GET NODE LIST from {Sender.Path.Name}");

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} sended NODE LIST to {Sender.Path.Name} => Value:[{string.Join(",", nodes)}]");

            //Ritorno la lista dei nodi
            Sender.Tell(new GetNodeListResponseMessage(Id, new SortedSet<uint>(nodes)), Self);
        }

        private void GetNodeListResponse(GetNodeListResponseMessage message)
        {
            //Inizializzo la lista di nodi PER COPIA
            nodes = new SortedSet<uint>(message.Nodes);

            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received GET NODE LIST RESPONSE from {Sender.Path.Name} => Value:[{string.Join(",", nodes)}]");

            //Prendo il prossimo nodo dopo di me
            uint nextNode = 0;
            {
                ImmutableList<uint> tmpList = nodes.ToImmutableList();
                for (uint i = 0; i < tmpList.Count; i++)
                    if (tmpList[(int)i] > this.Id)
                    {
                        nextNode = i;
                        break;
                    }
            }

            //Mando un messaggio al prossimo nodo per prendere tutte le key che tiene
            Context.ActorSelection($"/user/node{nextNode}").Tell(new GetKeysListMessage(this.Id), Self);

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} request GET KEYS LIST to node{nextNode}");
        }

        protected void AddNode(AddNodeMessage message)
        {
            //Un nuovo nodo si presenta
            //Lo aggiungo alla lista dei nodi
            nodes.Add(message.Id);

            if (deepDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} added node{message.Id}");

            //Elimino tutti gli elementi di cui non sono più responsabile
            //Per ogni chiave che ho
            data.KeyCollection().ForEach(key =>
            {
                //Prendo tutti i nodi che hanno quella chiave e controllo se "io" sono uno di quelli
                if (!FindNodesThatKeepKey(key).Any(node => node == this.Id))
                    data.Remove(key);//Se non dovrei avere questa chiave la rimuovo dal db
            });
        }

        private void GetKeysList(GetKeysListMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received GET KEYS LIST from {Sender.Path.Name}");

            //Aggiugo le chiavi di cui è responsabile il nuovo nodo
            List<uint> keysToReturn = new();
            data.KeyCollection().ForEach(key =>
            {
                if (FindNodesThatKeepKey(key, message.Id).Any(node => node == message.Id))
                    keysToReturn.Add(key);
            });

            //Restituisco la lista di chiavi
            Sender.Tell(new GetKeysListResponseMessage(keysToReturn), Self);

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} sended GET KEYS LIST RESPONSE to {Sender.Path.Name}  => Value:[{string.Join(",", keysToReturn)}]");
        }

        private void GetKeysListResponse(GetKeysListResponseMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received GET KEYS LIST RESPONSE from {Sender.Path.Name}  => Value:[{string.Join(",", message.KeysList)}]");

            if (message.KeysList.Count == 0)
            {
                //Questo è il caso in cui il db è ancora vuoto
                //Il nodo può attivarsi senza nessun problema
                ActivateNode();
            }
            else
            {
                //Ci sono già chiavi nel db. Bisogna recuperare questi elementi

                //Struttura dati per salvarsi per ogni nodo tutte le chiavi da chiedere
                Dictionary<uint, List<uint>> bulkRead = new();

                //Per ogni chiave ricevuta
                message.KeysList.ForEach(key =>
                {
                    //Prendo i nodi che hanno quella chiave
                    List<uint> nodesWithKey = FindNodesThatKeepKey(key);
                    //Per ogni nodo che ha la chiave
                    nodesWithKey.ForEach(node =>
                    {
                        //Se la lista delle chiavi è già presente
                        if (bulkRead.TryGetValue(node, out List<uint>? keys))
                            keys.Add(key);
                        else
                            bulkRead.Add(node, new List<uint> { key });
                    });

                    //Inizializzo il numero di risposte bulkread
                    //(numero di nodi che hanno quella chiave, numero di risposte per quella chiave ricevute)
                    inizializationsBulkReadsData[key] = ((uint)nodesWithKey.Count, 0);
                });

                bulkRead.ForEach(kvp =>
                {
                    if (sendDebug)
                        lock (Console.Out)
                            Console.WriteLine($"{Self.Path.Name} sended BULK READ to node{kvp.Key} => Value:[{string.Join(",", kvp.Value)}]");

                    //Se la lista di chiavi da chiedere non è vuota mando una bulk read ad ogni nodo
                    if (kvp.Value.Count > 0)
                        Context.ActorSelection($"/user/node{kvp.Key}").Tell(new BulkReadMessage(new(kvp.Value)), Self);
                });
            }
        }

        protected void BulkRead(BulkReadMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received BULK READ from {Sender.Path.Name} => Value:[{string.Join(",", message.KeysList)}]");

            //Mi salvo tutti i valori da inviare compreso di stato e versione
            List<Document?> toSend = new();
            message.KeysList.ForEach(key => toSend.Add(data[key]));

            if (sendDebug)
                lock (Console.Out)
                {
                    string str = $"{Self.Path.Name} sended BULK READ RESPONSE to {Sender.Path.Name} => Values:";
                    for (int i = 0; i < message.KeysList.Count; i++)
                        str += $"\n\t{message.KeysList[i]} -> {toSend[i]}";

                    Console.WriteLine(str);
                }

            Sender.Tell(new BulkReadResponseMessage(new(message.KeysList), toSend));
        }
        protected void BulkReadResponse(BulkReadResponseMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                {
                    string str = $"{Self.Path.Name} received BULK READ from {Sender.Path.Name} => Values:";
                    for (int i = 0; i < message.KeysList.Count; i++)
                        str += $"\n\t{message.KeysList[i]} -> {message.ValuesList[i]}";

                    Console.WriteLine(str);
                }

            for (int i = 0; i < message.KeysList.Count; i++)
            {
                //Aggiungo (o aggiorno se la versione è maggiore) i nuovi dati al db
                data.Add(message.KeysList[i], message.ValuesList[i]);

                //Se il nodo non è attivo
                if (!active)
                {
                    //Controllo se il numero di risposte esiste nel dizionario
                    //Esiste se:
                    //La chiave è stata richiesta e il numero di risposte è < QUORUM e il numero di risposte è < NUMERO DI NODI CHE HANNO LA CHIAVE
                    if (inizializationsBulkReadsData.TryGetValue(message.KeysList[i], out (uint nNodesThatKeepKey, uint nOfResponses) request))
                    {
                        //Se il numero di risposte è >= al QUORUM o comunque uguale al numero di nodi che hanno quella chiave
                        if (request.nOfResponses + 1 >= INIT_QUORUM)// || request.nOfResponses + 1 == request.nNodesThatKeepKey)
                            //Rimuovo la tupla dal dizionario
                            inizializationsBulkReadsData.TryRemove(message.KeysList[i], out _);
                        else
                            //Aggiorno il numero di risposte ricevute
                            inizializationsBulkReadsData.TryUpdate(message.KeysList[i], (request.nNodesThatKeepKey, request.nOfResponses + 1), request);
                    }
                }
            }

            //Se il nodo non è attivo e la struttura è vuota significa che ho ricevuto tutti i dati
            //Quindi ho finito il setup del nodo e posso attivarmi annunciandomi a tutta la rete
            if (!active && inizializationsBulkReadsData.IsEmpty)
                ActivateNode();
        }

        private void ActivateNode()
        {
            if (active)
                throw new Exception($"{Self.Path.Name} alreay active!");
            //Mi annuncio a tutti gli altri nodi (me stesso compreso)
            Context.ActorSelection("/user/*").Tell(new AddNodeMessage(this.Id));

            //Mi attivo
            active = true;

            if (generalDebug)
                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{Self.Path.Name} joined the network succesfully");
                    Console.ResetColor();
                }
        }
    }
}
