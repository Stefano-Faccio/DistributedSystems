namespace DistributedKeyValueStore.NET
{
    internal class Node : UntypedActor
    {
        //Datatabase dei dati chiave valore del nodo
        readonly Collection data = new();
        //Lista degli altri nodi
        SortedSet<uint> nodes = new();
        //Id del nodo
        public uint Id { get; private set; }
        //Hashset thread-safe per le richieste get
        readonly ConcurrentDictionary<int, GetDataStructure> getRequestsData = new();
        /* N.B. Tutti i metodi pubblici e protetti sono thread-safe tranne quelli implementati tramite interfaccia
         * https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2?view=net-7.0
         */

        //Teniamo una convenzione nei log:
        //{Chi? - Es. node0} {Cosa? Es. ricevuto/inviato GET/UPDATE} {da/a chi? - Es. node0} => [{ecc}]

        readonly bool debug = true;

        public Node()
        {
            //Setto l'id ad un valore di default
            Id = uint.MaxValue;
        }

        protected override void PreStart()
        {
            if (debug)
            {
                Console.WriteLine($"{Self.Path.Name} started succesfully");
            }
        }

        protected override void PostStop()
        {
            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Self.Path.Name} is gone!");
                Console.ResetColor();
            }
        }

        private List<uint> FindNodesThatKeepKey(uint key)
        {
            //Converto l'albero autobilanciante in una lista ordinata
            List<uint> sortedList = nodes.ToList();
            //Prendo l'id dell'elemento più grande
            uint maxId = sortedList[sortedList.Count - 1];
            //Creo la lista dei nodi da ritornare
            List<uint> returnList = new List<uint>(N);
            //Rappresenta il numero di nodi che devono ancora essere inseriti nella lista di ritorno
            int nodesToFind = N;

            if (sortedList.Count < N)
                throw new Exception("There are less Nodes active than N");

            for (int i = 0; i < sortedList.Count && nodesToFind > 0; i++)
            {
                if (sortedList[i] >= key)
                {
                    returnList.Add(sortedList[i]);
                    nodesToFind--;
                }
            }
            //Riparto dall'inizio dell'anello
            key = 0;
            for (int i = 0; i < sortedList.Count && nodesToFind > 0; i++)
            {
                returnList.Add(sortedList[i]);
                nodesToFind--;
            }

            return returnList;
        }

        //-------------------------------------------------------------------------------------------------------
        //MESSAGGI DI SUPPORTO

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
                {
                    //Mi annuncio a tutti gli altri nodi (client) (me stesso compreso)
                    Context.ActorSelection("/user/*").Tell(new AddNodeMessage(this.Id));

                    if (debug)
                        Console.WriteLine($"{Self.Path.Name} initialized succesfully");
                }
            }
            else
                throw new Exception("Node already initialided");
        }

        protected void AddNode(AddNodeMessage message)
        {
            //Un nuovo nodo si presenta
            //Lo aggiungo alla lista dei nodi
            nodes.Add(message.Id);

            if (debug)
            {
                Console.WriteLine($"{Self.Path.Name} added node {message.Id}");
            }

            //Devo eliminare tutti gli elementi di cui non sono più responsabile
            //TODO
        }

        protected void RemoveNode(RemoveNodeMessage message)
        {

        }

        private void GetNodeList(GetNodeListMessage message)
        {
            if (debug)
            {
                Console.WriteLine($"{Self.Path.Name} received GET NODE LIST from {Sender.Path.Name}");
                Console.WriteLine($"{Self.Path.Name} sended NODE LIST to {Sender.Path.Name} => Value:[{string.Join(",", nodes)}]");
            }
            //Ritorno (il riferimento) della lista dei nodi 
            Sender.Tell(new GetNodeListResponseMessage(Id, new SortedSet<uint>(nodes)));
        }

        private void GetNodeListResponse(GetNodeListResponseMessage message)
        {
            //Inizializzo la lista di nodi PER COPIA
            nodes = new SortedSet<uint>(message.Nodes);

            if (debug)
            {
                Console.WriteLine($"{Self.Path.Name} received GET NODE LIST RESPONSE from {Sender.Path.Name} => Value:[{string.Join(",", nodes)}]");
                Console.WriteLine($"{Self.Path.Name} initialized succesfully");
            }

            //Mando un messaggio al prossimo nodo per prendere tutte le key che tiene
            //TODO

            //Mi annuncio a tutti gli altri nodi (me stesso compreso)
            Context.ActorSelection("/user/*").Tell(new AddNodeMessage(this.Id));
        }

        //-------------------------------------------------------------------------------------------------------
        //MESSAGGI DI UTILIZZO

        protected void OnWrite(WriteMessage message)
        {
            // Write e' forzata
            var Key = message.Key;
            var Value = message.Value;
            var Version = message.Version;
            if (Value == null) return;
            data.Add(Key, Value, Version);
        }

        protected void OnUpdate(UpdateMessage message)
        {

        }

        protected void OnGet(GetMessage message)
        {
            if (debug)
                Console.WriteLine($"{Self.Path.Name} received GET from {Sender.Path.Name} => Key:{message.Key}");

            //Genero un numero casuale id della richiesta GET
            int getID = SuperMain.mersenneTwister.Next();

            //Alloco lo spazio e salvo Key e nome del nodo che ha fatto la richiesta
            if (!getRequestsData.TryAdd(getID, new GetDataStructure(message.Key, Sender.Path.Name)))
                throw new Exception("Errors adding item to getRequestsData Dictionary");

            //Imposto un timer per inviare una risposta di timeout al client in caso di non raggiungimento del quorum
            System.Timers.Timer timoutTimer = new System.Timers.Timer(TIMEOUT_TIME);
            //La variabile Sender e Self non sono presenti nel contesto del Timer quindi salvo i riferimenti
            IActorRef SenderRef = Sender;
            IActorRef SelfRef = Self;
            //Funzione di callback
            timoutTimer.Elapsed += (source, e) =>
            {
                //Rimuovo i dati della richiesta GET se esistono
                if (getRequestsData.TryRemove(getID, out GetDataStructure? getRequestData))
                {
                    //Se i dati della richiesta GET ci sono ancora significa che non è stata soddisfatta quindi invio un timeout al client
                    //Invio la risposta di timeout
                    SenderRef.Tell(new GetResponseMessage(message.Key, true), SelfRef);

                    if (debug)
                        Console.WriteLine($"{SelfRef.Path.Name} sended GET RESPONSE (TIMEOUT) to {SenderRef.Path.Name} => Key:{message.Key}");
                }
                else if (debug)
                    Console.WriteLine($"{SelfRef.Path.Name} TIMEOUT not achieved => Key:{message.Key}");
                //Se getRequestData è null posso ignorare la risposta in quanto è già stata soddisfatta
            };
            timoutTimer.AutoReset = false;
            timoutTimer.Enabled = true;

            //Recupero i nodi che tengono quel valore
            List<uint> nodesWithValue = FindNodesThatKeepKey(message.Key);

            //Invio messaggi di READ a tutti gli altri nodi che hanno il valore
            foreach (uint node in nodesWithValue)
                Context.ActorSelection($"/user/node{node}").Tell(new ReadMessage(message.Key, getID));

            if (debug)
                Console.WriteLine($"{Self.Path.Name} sended READ to ALL NODES => Key:{message.Key}");
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

                if (debug)
                    Console.WriteLine($"{Self.Path.Name} received READ RESPONSE from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value ?? "null"} Version: {message.Version} PreWriteBlock: {message.PreWriteBlock}");

                //Cerco di prendere il valore da restituire alla GET
                string? returnValue = getRequestData.GetReturnValue();

                //GetReturnValue ritorna il valore più recente se ho abbastanza risposte i.e. >= READ_QUORUM
                //oppure torna null nel caso in cui:
                //- c'è una write in corso di cui non ho (ancora) il valore
                //- non ho risposte >= READ_QUORUM
                //Bisogna vedere se risco ad eliminare i dati della richiesta GET (i.e. potrebbe essere che il timeout sia partito nel frattempo)
                if (returnValue is not null && getRequestsData.TryRemove(message.GetId, out GetDataStructure? getRequestDataRemoved))
                {
                    //Invio la risposta al nodo
                    Context.ActorSelection($"/user/{getRequestDataRemoved.NodeName}").Tell(new GetResponseMessage(message.Key, returnValue));

                    if (debug)
                        Console.WriteLine($"{Self.Path.Name} sended GET RESPONSE (QUORUM ACHIEVED) to {Sender.Path.Name} => Key:{message.Key} Value:{returnValue ?? "null"}");
                }
            }
            else if (debug)
                Console.WriteLine($"{Self.Path.Name} received READ RESPONSE (IGNORED) from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value ?? "null"}");


            //Se getRequestData è null posso ignorare la risposta in quanto è già stata soddisfatta
        }

        protected void OnPreWrite(PreWriteMessage message)
        {
            var Key = message.Key;
            var doc = data[Key];
            if (doc == null)
            {
                // Non abbiamo il valore salvato, quindi per noi va bene aggiungerlo
                Sender.Tell(new PreWriteResponseMessage(message.Key, true), Self);
                return;
            }
            bool preWriteBlock = doc.GetPreWriteBlock();
            if (preWriteBlock)
            {
                // Siamo gia' in prewrite per questa chiave, quindi diciamo che non possiamo aggiornare
                Sender.Tell(new PreWriteResponseMessage(message.Key, false), Self);
            }
            else
            {
                doc.SetPreWriteBlock();
                // Va bene aggiornare il valore per noi
                Sender.Tell(new PreWriteResponseMessage(message.Key, true), Self);
            }
        }

        protected void OnPreWriteResponse(PreWriteResponseMessage message)
        {

        }

        //-------------------------------------------------------------------------------------------------------

        protected override void OnReceive(object msg)
        {
            //if(msg is not null)
            switch (msg)
            {
                //MESSAGGI DI SUPPORTO
                case StartMessage message:
                    Start(message);
                    break;
                case AddNodeMessage message:
                    AddNode(message);
                    break;
                case RemoveNodeMessage message:
                    RemoveNode(message);
                    break;
                case GetNodeListMessage message:
                    GetNodeList(message);
                    break;
                case GetNodeListResponseMessage message:
                    GetNodeListResponse(message);
                    break;

                //MESSAGGI DI UTILIZZO
                case ReadMessage message:
                    OnRead(message);
                    break;
                case ReadResponseMessage message:
                    OnReadResponse(message);
                    break;
                case UpdateMessage message:
                    OnUpdate(message);
                    break;
                case GetMessage message:
                    OnGet(message);
                    break;
                case WriteMessage message:
                    OnWrite(message);
                    break;
                case PreWriteMessage message:
                    OnPreWrite(message);
                    break;
                case PreWriteResponseMessage message:
                    OnPreWriteResponse(message);
                    return;

                //MESSAGGI DI TESTING
                case TestMessage message:
                    Test(message);
                    break;
                default:
                    throw new Exception("Not yet implemented!");
            }
        }

        protected void Test(TestMessage message)
        {
            if (debug)
            {
                Console.WriteLine($"Count nodes: {nodes.Count}");
                foreach (var foo in nodes)
                {
                    Console.Write(foo.ToString() + " ");
                }
                Console.WriteLine();
            }
        }
    }
}
