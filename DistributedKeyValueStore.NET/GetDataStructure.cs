using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DistributedKeyValueStore.NET.Constants;

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
            string? response = null;

            //Se ho almeno read_quorum risposte
            if(Values.Count >= READ_QUORUM)
            {
                //Indice del valore più recente
                int mostRecent = 0;
                //Indica se c'è qualche elemento tra i più recenti in preWrite
                bool mostRecentPreWrite = false;
                //Indica il numero di risposte non null (quindi valide)
                uint validValues = 0;

                //Cerco il valore più recente e se è in preWrite
                for (int i = 0; i < Values.Count; i++)
                {
                    if (Values[i] is not null)
                    {
                        //Aggiorno il contatore delle risposte valide
                        validValues++;

                        //Se trovo una versione più recente aggiorno
                        if (Versions[i] > Versions[mostRecent])
                        {
                            mostRecent = i;
                            //Pulisco il preWrite block
                            mostRecentPreWrite = PreWriteBlocks[mostRecent];
                        }
                        else if (Versions[mostRecent] == Versions[i])//Se trovo una versione ugualmente recente aggiorno il PreWrite
                        {
                            mostRecentPreWrite |= PreWriteBlocks[i];
                        }
                    }
                }

                //Se l'elemento più recente non è in preWrite e ho raggiunto il read quorum 
                //allora è safe restituire il valore più recente
                if (!mostRecentPreWrite && validValues >= READ_QUORUM) 
                    response = Values[mostRecent];
            }
            
            //getRequestData.Values.Count >= READ_QUORUM
            return response;
        }

        public override string? ToString()
        {
            return $"NodeName: {NodeName} Key:{Key} Values: [{string.Join(",", Values)}] " +
                $"Versions: [{string.Join(",", Versions)}], PreWriteBlocks: [{string.Join(",", PreWriteBlocks)}]";
        }
    }
}
