using Akka.Actor;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal class Node : UntypedActor
    {
        //Datatabase dei dati chiave valore del nodo
        Collection data = new Collection();
        //Lista degli altri nodi
        SortedSet<uint> nodes = new SortedSet<uint>();
        //Id del nodo
        public uint Id { get; private set; }
        //Hashset per le richieste get
        Dictionary<int, GetDataStructure> getRequestsData = new Dictionary<int, GetDataStructure>();

        //Teniamo una convenzione nei log:
        //{Chi? - Es. node0} {Cosa? Es. ricevuto/inviato GET/UPDATE} {da/a chi? - Es. node0} => [{ecc}]

        const int READQUORUM = 2;
        bool debug = true;

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
            
            if(debug)
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
            Sender.Tell(new GetNodeListResponseMessage(Id, nodes));
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

            //Recupero i valori aggiornati
            //TODO

            //Mi annuncio a tutti gli altri nodi (me stesso compreso)
            Context.ActorSelection("/user/*").Tell(new AddNodeMessage(this.Id));
        }

        //-------------------------------------------------------------------------------------------------------
        //MESSAGGI DI UTILIZZO

        protected void OnWrite(WriteMessage message)
        {

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
            getRequestsData[getID] = new GetDataStructure(message.Key, Sender.Path.Name);

            //Invio messaggi di READ a tutti gli altri nodi
            foreach (uint node in nodes)
                Context.ActorSelection($"/user/node{node}").Tell(new ReadMessage(message.Key, getID));

            if (debug)
                Console.WriteLine($"{Self.Path.Name} sended REAT to ALL NODES => Key:{message.Key}");
        }

        protected void OnRead(ReadMessage message)
        {
            //Recupero il valore
            string? value = data[message.Key]?.Value ?? null;
            //Invio la risposta
            Sender.Tell(new ReadResponseMessage(message.Key, value, message.GetId), Self);

            if (debug)
            {
                Console.WriteLine($"{Self.Path.Name} received READ from {Sender.Path.Name} => Key:{message.Key}");
                Console.WriteLine($"{Self.Path.Name} sended READ RESPONSE to {Sender.Path.Name} => Key:{message.Key} Value:{value ?? "null"}");
            }
        }
        
        private void OnReadResponse(ReadResponseMessage message)
        {
            if (message.Value is null)
                throw new Exception("Read responce contains null value");

            //Prendo i dati della richiesta GET
            getRequestsData.TryGetValue(message.GetId, out GetDataStructure? getRequestData);
            if(getRequestData is not null)
            {
                //Aggiungo il valore ricevuto ai dati della 
                getRequestData.NodesResponse.Add(message.Value);                   

                //Se ho abbastanza risposte rispondo alla GET
                if (getRequestData.NodesResponse.Count > READQUORUM)
                {
                    //Prendo il valore per maggioranza
                    //TODO
                    string majorityValue = getRequestData.NodesResponse.GetMajorityValue();

                    //Invio la risposta al nodo
                    Context.ActorSelection($"/user/{getRequestData.NodeName}").Tell(new GetResponseMessage(message.Key, majorityValue));

                    //Pulisco 

                }
            }

            //Se getRequestData è null posso ignorare la risposta
        }

        protected void OnPreWrite(PreWriteMessage message)
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
