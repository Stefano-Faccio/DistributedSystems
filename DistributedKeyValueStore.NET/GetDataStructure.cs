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
        public List<string> NodesResponse { get; private set; }

        public GetDataStructure(uint key, string nodeName) 
        {
            Key = key;
            NodeName = nodeName;
            NodesResponse = new List<string>();
        }
    }
}
