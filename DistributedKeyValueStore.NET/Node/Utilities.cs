using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    //Funzioni aggiungive generiche e di test
    internal partial class Node : UntypedActor
    {
        private List<uint> FindNodesThatKeepKey(uint key)
        {
            //Converto l'albero autobilanciante in una lista ordinata
            List<uint> sortedList = nodes.ToList();
            //Creo la lista dei nodi da ritornare
            List<uint> returnList = new List<uint>(N);
            //Rappresenta il numero di nodi che devono ancora essere inseriti nella lista di ritorno
            int nodesToFind = Math.Min(N, sortedList.Count);

            for (int i = 0; i < sortedList.Count && nodesToFind > 0; i++)
            {
                if (sortedList[i] >= key)
                {
                    returnList.Add(sortedList[i]);
                    nodesToFind--;
                }
            }
            //Riparto dall'inizio dell'anello
            for (int i = 0; i < sortedList.Count && nodesToFind > 0; i++)
            {
                returnList.Add(sortedList[i]);
                nodesToFind--;
            }
            return returnList;
        }

        protected void Test(TestMessage message)
        {
            //Stampo tutti i nodi che ho aggiunto
            lock (Console.Out)
            {
                Console.Write($"\tCount nodes ({nodes.Count}): [");
                foreach (var foo in nodes)
                {
                    Console.Write(foo.ToString() + " ");
                }
                Console.WriteLine("]");
            }

            //Stampo tutti i valori che ho nel db 
            lock (Console.Out)
            {
                List<uint> keyCollection = data.KeyCollection();
                Console.WriteLine($"\tData ({keyCollection.Count}) : ");
                keyCollection.ForEach(key =>
                {
                    Console.WriteLine($"\t\t{key} -> {data[key]}");
                });
            }
        }
    }
}
