using Akka.Actor;

namespace DistributedKeyValueStore.NET
{
    internal abstract class Message
    {
        public Message() { }
    }
    internal abstract class KeyValueMessage : Message
    {
        public uint Key { get; private set; }
        public string? Value { get; private set; }

        public KeyValueMessage(uint Key, string? value)
        {
            this.Key = Key;
            Value = value;
        }
    }

    internal abstract class KeyMessage : Message
    {
        public uint Key { get; private set; }

        public KeyMessage(uint Key)
        {
            this.Key = Key;
        }
    }

    internal abstract class NodeMessage : Message
    {
        public uint Id { get; private set; }
        public NodeMessage(uint id)
        {
            Id = id;
        }
    }

    internal class ReadMessage : KeyMessage
    {
        public int GetId { get; private set; }
        public ReadMessage(uint Key, int GetId) : base(Key)
        {
            this.GetId = GetId;
        }
    }
    internal class ReadResponseMessage : KeyValueMessage
    {
        public int GetId { get; private set; }
        public bool PreWriteBlock { get; private set; }
        public uint Version { get; private set; }
        public ReadResponseMessage(uint key, string? value, int getId, uint version, bool preWriteBlock) : base(key, value)
        {
            GetId = getId;
            Version = version;
            PreWriteBlock = preWriteBlock;
        }

        public ReadResponseMessage(uint key, string? value, int getId) : this(key, value, getId, 0, false)
        { }
    }

    internal class GetMessage : KeyMessage
    {
        public GetMessage(uint Key) : base(Key) { }
    }

    internal class GetResponseMessage : KeyValueMessage
    {
        public bool Timeout { get; private set; }
        public GetResponseMessage(uint Key, string? value) : base(Key, value)
        {
            Timeout = false;
        }

        public GetResponseMessage(uint Key, bool timeout) : base(Key, null)
        {
            Timeout = timeout;
        }
    }

    internal class PreWriteMessage : KeyMessage
    {
        public int UpdateId { get; private set; }
        public PreWriteMessage(uint Key, int UpdateId) : base(Key)
        {
            this.UpdateId = UpdateId;
        }
    }

    internal class PreWriteResponseMessage : PreWriteMessage
    {
        public bool Result { get; private set; }
        public uint Version { get; private set; }
        public PreWriteResponseMessage(uint Key, int UpdateId, bool result, uint version) : base(Key, UpdateId)
        {
            this.Result = result;
            this.Version = version;
        }
    }

    internal class WriteMessage : KeyValueMessage
    {
        public uint Version { get; private set; }
        public WriteMessage(uint Key, string? value, uint version) : base(Key, value)
        {
            this.Version = version;
        }
    }

    internal class UpdateMessage : KeyValueMessage
    {
        public UpdateMessage(uint Key, string? value) : base(Key, value) { }
    }

    internal class UpdateResponseMessage : KeyValueMessage
    {
        public bool Achieved { get; private set; }
        public UpdateResponseMessage(uint Key, string? value, bool Achieved) : base(Key, value)
        {
            this.Achieved = Achieved;
        }
    }

    internal class StartMessage : NodeMessage
    {
        public uint AskNode { get; private set; }
        public StartMessage(uint id, uint askNode) : base(id)
        {
            AskNode = askNode;
        }
    }

    internal class StopMessage : NodeMessage
    {
        public StopMessage(uint id) : base(id)
        {
        }
    }

    internal class AddNodeMessage : NodeMessage
    {
        public AddNodeMessage(uint id) : base(id)
        {
        }
    }
    internal class RemoveNodeMessage : NodeMessage
    {
        public RemoveNodeMessage(uint id) : base(id)
        {
        }
    }

    internal class GetNodeListMessage : NodeMessage
    {
        public GetNodeListMessage(uint id) : base(id)
        {
        }
    }

    internal class GetNodeListResponseMessage : NodeMessage
    {
        public SortedSet<uint> Nodes { get; private set; }
        public GetNodeListResponseMessage(uint id, SortedSet<uint> nodes) : base(id)
        {
            this.Nodes = nodes;
        }
    }

    internal class GetKeysListMessage : NodeMessage
    {
        public GetKeysListMessage(uint id) : base(id)
        {
        }
    }
    internal class GetKeysListResponseMessage : Message
    {
        public List<uint> KeysList { get; private set; }
        public GetKeysListResponseMessage(List<uint> KeysList)
        {
            this.KeysList = KeysList;
        }
    }
    internal class BulkReadMessage : Message
    {
        public List<uint> KeysList { get; private set; }
        public BulkReadMessage(List<uint> KeysList)
        {
            this.KeysList = KeysList;
        }
    }

    internal class RequestToLeaveMessage : NodeMessage
    {
        public int LeaveId { get; private set; }
        public RequestToLeaveMessage(uint id, int leaveId) : base(id)
        {
            LeaveId = leaveId;
        }
    }
    internal class RequestToLeaveResponseMessage : NodeMessage
    {
        public int LeaveId { get; private set; }
        public bool Success { get; private set; }
        public RequestToLeaveResponseMessage(uint id, int leaveId, bool success) : base(id)
        {
            LeaveId = leaveId;
            Success = success;
        }
    }

    internal class BulkWriteMessage : Message
    {
        public List<(uint, Document)> KeyValuesList { get; private set; }
        public BulkWriteMessage(List<(uint, Document)> KeyValuesList)
        {
            this.KeyValuesList = KeyValuesList;
        }
    }

    internal class BulkReadResponseMessage : BulkReadMessage
    {
        public List<Document?> ValuesList { get; private set; }

        public BulkReadResponseMessage(List<uint> KeysList, List<Document?> ValuesList) : base(KeysList)
        {
            this.ValuesList = ValuesList;
        }
    }

    internal class CrashMessage : NodeMessage
    {
        public CrashMessage(uint id) : base(id) { }
    }

    internal class RecoverMessage : NodeMessage
    {
        public RecoverMessage(uint id) : base(id) { }
    }

    internal class TimeoutGetMessage : ReadMessage
    {
        public IActorRef Sender { get; private set; }
        public TimeoutGetMessage(uint Key, int GetId, IActorRef Sender) : base(Key, GetId)
        {
            this.Sender = Sender;
        }
    }

    internal class TimeoutUpdateMessage : KeyMessage
    {
        public int UpdateId { get; private set; }
        public TimeoutUpdateMessage(uint Key, int UpdateId) : base(Key)
        {
            this.UpdateId = UpdateId;
        }
    }
    internal class TimeoutShutdown1Message : Message
    {
        public TimeoutShutdown1Message()
        {
        }
    }

    internal class TimeoutShutdown2Message : Message
    {
        public TimeoutShutdown2Message()
        {
        }
    }

    internal class TimeoutShutdown3Message : Message
    {
        public TimeoutShutdown3Message()
        {
        }
    }

    internal class TestMessage : Message
    {

    }
}
