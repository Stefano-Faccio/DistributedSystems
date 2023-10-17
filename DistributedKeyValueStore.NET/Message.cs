﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public int GetId {get; private set;}
        public ReadMessage(uint Key, int readId) : base(Key) 
        {
            GetId = readId;
        }
    }
    internal class ReadResponseMessage : KeyValueMessage
    {
        public int GetId {get; private set;}
        public ReadResponseMessage(uint Key, string? value, int readId) : base(Key, value)
        {
            GetId = readId;
        }
    }

    internal class GetMessage : KeyMessage
    {
        public GetMessage(uint Key) : base(Key) { }
    }

    internal class GetResponseMessage : KeyValueMessage
    {
        public GetResponseMessage(uint Key, string? value) : base(Key, value)
        { }
    }

    internal class PreWriteMessage : KeyMessage
    {
        public PreWriteMessage(uint Key) : base(Key) { }
    }

    internal class WriteMessage : KeyValueMessage
    {
        public WriteMessage(uint Key, string? value) : base(Key, value) { }
    }

    internal class UpdateMessage : KeyValueMessage
    {
        public UpdateMessage(uint Key, string? value) : base(Key, value) { }
    }

    internal class StartMessage : NodeMessage
    {
        public uint AskNode { get; private set; }
        public StartMessage(uint id, uint askNode) : base(id)
        {
            AskNode = askNode;
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
        public SortedSet<uint> Nodes{ get; private set; }
        public GetNodeListResponseMessage(uint id, SortedSet<uint> nodes) : base(id)
        {
            this.Nodes = nodes;
        }
    }

    internal class TestMessage : Message
    {
        
    }
}
