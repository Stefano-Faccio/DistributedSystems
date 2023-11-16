using Akka.Actor;
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
            if (!active)
            {
                if (generalDebug)
                    lock (Console.Out)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{Self.Path.Name} is crashed and cannot receive a message");
                        Console.ResetColor();
                    }
                return;
            }

            switch (msg)
            {
                case TimeoutGetMessage message:
                    OnGetTimout(message);
                    break;
                case TimeoutUpdateMessage message:
                    OnUpdateTimout(message);
                    break;
                case TimeoutShutdown1Message message:
                    OnShutdownTimout1(message);
                    break;
                case TimeoutShutdown2Message message:
                    OnShutdownTimout2(message);
                    break;
                case TimeoutShutdown3Message message:
                    OnShutdownTimout3(message);
                    break;
                case StartMessage message:
                    Start(message);
                    break;
                case StopMessage message:
                    Stop(message);
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
                    switch(message.Identifier)
                    {
                        case RequestIdentifier.NONE:
                            GetNodeListResponse(message);
                            break;
                        case RequestIdentifier.RECOVERY:
                            //
                            break;
                        default:
                            throw new Exception("Not yet implemented!");
                    }
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
                case RequestToLeaveMessage message:
                    RequestToLeave(message);
                    break;
                case RequestToLeaveResponseMessage message:
                    RequestToLeaveResponse(message);
                    break;
                case BulkReadMessage message:
                    BulkRead(message);
                    break;
                case BulkWriteMessage message:
                    BulkWrite(message);
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
                case GetResponseMessage message:
                    switch(message.Identifier)
                    {
                        case RequestIdentifier.NONE:
                            break;
                        case RequestIdentifier.SHUTDOWN:
                            GetResponseShutdown(message);
                            break;
                        case RequestIdentifier.RECOVERY:
                            //GetResponseRecovery(message);
                            break;
                        default:
                            throw new Exception("Not yet implemented!");
                    }
                    break;
                case WriteMessage message:
                    OnWrite(message);
                    break;
                case PreWriteResponseMessage message:
                    OnPreWriteResponse(message);
                    return;
                case PreWriteMessage message:
                    OnPreWrite(message);
                    break;
                case CrashMessage message:
                    onCrash(message);
                    break;
                case RecoveryMessage message:
                    onRecovery(message);
                    break;
                case TestMessage message:
                    Test(message);
                    break;
                default:
                    throw new Exception("Not yet implemented!");
            }
        }
    }
}
