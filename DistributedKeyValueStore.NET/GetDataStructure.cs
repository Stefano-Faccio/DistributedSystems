using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal class GetDataStructure
    {
        
        public List<string> NodesName { get; private set; }
        public List<Tuple<string, string>> NodesResponse { get; private set; }

        public GetDataStructure() 
        { 
            NodesName = new List<string>();
            NodesResponse = new List<Tuple<string, string>>();
        }
        //Lista nodi da cui ho richiesto la read per una data key e le risposte ritornate
        GetDataStructure getRequestsData = new GetDataStructure();



        //Dictionary<uint, Tuple<List<string>, >> getList = new Dictionary<uint, Tuple<List<string>, >>();

        (key => ([lista di chi ha richiesto la read][lista di chi]) )
    }
}
