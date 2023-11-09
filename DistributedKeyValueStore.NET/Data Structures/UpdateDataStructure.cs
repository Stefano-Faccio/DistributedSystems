using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET.Data_Structures
{
    internal class UpdateDataStructure
    {
        public int UpdateId { get; private set; }
        public List<uint> Versions { get; private set; }
        public uint Responses { get; set; }
        public uint Key { get; private set; }
        public string? Value { get; private set; }
        public List<uint> NodesWithValue { get; private set; }
        public IActorRef Sender { get; private set; }
        public UpdateDataStructure(uint Key, string? Value, List<uint> NodesWithValue, int UpdateId, IActorRef Sender)
        {
            this.Key = Key;
            this.Value = Value;
            this.NodesWithValue = NodesWithValue;
            this.UpdateId = UpdateId;
            this.Sender = Sender;
            Versions = new List<uint>();
            Responses = 0;
        }
    }
}
