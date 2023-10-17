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
        public List<string?> NodesResponse { get; private set; }

        public GetDataStructure(uint key, string nodeName) 
        {
            Key = key;
            NodeName = nodeName;
            NodesResponse = new List<string?>();
        }

        public string GetMajorityValue()
        {
            //Sintassi interessante...
            return (from i in NodesResponse
                    group i by i into grp
                    orderby grp.Count() descending
                    select grp.Key).First();

            /*
            string str = "";
            Dictionary<string, uint> counters = new Dictionary<string, uint>();

            foreach(string strNow in this)
                counters[strNow] = 0;

            foreach (string strNow in this)
                counters[strNow]++;

            var keyOfMaxValue = counters.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

            return str;
            */
        }
    }
}
