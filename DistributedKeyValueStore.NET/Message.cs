using System;
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
        public string Value { get; private set; }

        public KeyValueMessage(uint Key, string value)
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

    internal class ReadMessage : KeyMessage
    {
        public ReadMessage(uint Key) : base(Key) { }
    }

    internal class GetMessage : KeyMessage
    {
        public GetMessage(uint Key) : base(Key) { }
    }

    internal class PreWriteMessage : KeyMessage
    {
        public PreWriteMessage(uint Key) : base(Key) { }
    }

    internal class WriteMessage : KeyValueMessage
    {
        public WriteMessage(uint Key, string value) : base(Key, value) { }
    }

    internal class UpdateMessage : KeyValueMessage
    {
        public UpdateMessage(uint Key, string value) : base(Key, value) { }
    }
}
