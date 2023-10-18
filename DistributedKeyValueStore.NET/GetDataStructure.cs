using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal class GetDataStructure
    {
        public string NodeName {  get; private set; }
        public uint Key { get; private set; }
        private List<string?> Values { get; set; }
        private List<uint> Versions { get; set; }
        private List<bool> PreWriteBlocks { get; set; }

        public GetDataStructure(uint key, string nodeName) 
        {
            Key = key;
            NodeName = nodeName;
            Values = new List<string?>();
            Versions = new List<uint>();
            PreWriteBlocks = new List<bool>();
        }

        public void Add(string? value, uint version, bool preWriteBlock)
        {
            Values.Add(value);
            Versions.Add(version);
            PreWriteBlocks.Add(preWriteBlock);
        }

        public string? GetReturnValue()
        {
            //getRequestData.Values.Count >= READ_QUORUM
            return null;
        }

        public override string? ToString()
        {
            return $"NodeName: {NodeName} Key:{Key} Values: [{string.Join(",", Values)}] " +
                $"Versions: [{string.Join(",", Versions)}], PreWriteBlocks: [{string.Join(",", PreWriteBlocks)}]";
        }
    }
}
