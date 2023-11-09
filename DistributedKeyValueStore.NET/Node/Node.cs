using Akka.Actor;
using Akka.Util.Internal;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    internal partial class Node : UntypedActor, IWithTimers
    {
        //Datatabase dei dati chiave valore del nodo
        readonly Collection data = new();
        //Lista degli altri nodi
        SortedSet<uint> nodes = new();
        //Id del nodo
        public uint Id { get; private set; }
        //Per i timeout
        public ITimerScheduler Timers { get; set; }

        //Booleano che indica se il nodo è attivo ossia fa parte attivamente della rete
        bool active = false;

        //Convenzione nei log:
        //{Chi? - Es. node0} {Cosa? Es. ricevuto/inviato GET/UPDATE} {da/a chi? - Es. node0} => [{ecc}]

        public Node()
        {
            //Setto l'id ad un valore di default
            Id = uint.MaxValue;
            //Setto active a false per indicare che il nodo deve ancora aggiungersi alla rete
            active = false;
        }

        protected override void OnReceive(object msg)
        {
            switch (msg)
            {
                case TimeoutGetMessage message:
                    OnGetTimout(message);
                    break;
                case TimeoutUpdateMessage message:
                    OnUpdateTimout(message);
                    break;
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
                case GetKeysListMessage message:
                    GetKeysList(message);
                    break;
                case GetKeysListResponseMessage message:
                    GetKeysListResponse(message);
                    break;
                case BulkReadResponseMessage message:
                    BulkReadResponse(message);
                    break;
                case BulkReadMessage message:
                    BulkRead(message);
                    break;
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
                case TestMessage message:
                    Test(message);
                    break;
                default:
                    throw new Exception("Not yet implemented!");
            }
        }
    }
}
