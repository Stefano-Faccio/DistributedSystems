using Akka.Actor;
using Akka.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    //Funzioni aggiungive generiche e di test
    internal partial class Node : UntypedActor, IWithTimers
    {
        private List<uint> FindNodesThatKeepKey(uint key)
        {
            //Prendo di default la lista dei nodi attuali
            //Converto l'albero autobilanciante in una lista ordinata
            return FindNodesThatKeepKey(key, nodes.ToList());
        }
        private List<uint> FindNodesThatKeepKey(uint key, uint newNode)
        {
            //Prendo di default la lista dei nodi attuali e aggiungo il nuovo elemento
            //Converto l'albero autobilanciante in una lista ordinata

            SortedSet<uint> nodesTmp = new(nodes)
            {
                newNode
            };

            return FindNodesThatKeepKey(key, nodesTmp.ToList());
        }

        private List<uint> FindNodesThatKeepKey(uint key, List<uint> sortedList)
        {
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

        private void RemoveElementsNoResponsible()
        {
            //Per ogni chiave che ho
            data.KeyCollection().ForEach(key =>
            {
                //Prendo tutti i nodi che hanno quella chiave e controllo se "io" sono uno di quelli
                if (!FindNodesThatKeepKey(key).Any(node => node == this.Id))
                    data.Remove(key);//Se non dovrei avere questa chiave la rimuovo dal db
            });
        }

        private uint NextNode()
        {
            List<uint> nodesTmp = nodes.ToList();
            int meIndex = nodesTmp.IndexOf(this.Id);
           
            return nodesTmp[(meIndex + 1) % nodesTmp.Count];
        }

        private uint PreviousNode()
        {
            List<uint> nodesTmp = nodes.ToList();
            int meIndex = nodesTmp.IndexOf(this.Id);
            return meIndex > 0 ? nodesTmp[meIndex - 1] : nodesTmp[^1];
        }

        protected void Test(TestMessage message)
        {
            
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"****** {Self.Path.Name} ****** ");
                Console.ResetColor();

                //Stampo tutti i nodi che ho aggiunto
                Console.Write($"Count nodes ({nodes.Count}): [");
                foreach (var foo in nodes)
                {
                    Console.Write(foo.ToString() + " ");
                }
                Console.WriteLine("]");

                //Stampo tutti i valori che ho nel db 
                List<uint> keyCollection = data.KeyCollection();
                Console.WriteLine($"Data ({keyCollection.Count}) : ");
                keyCollection.ForEach(key =>
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write($"\t{key}");
                    Console.ResetColor();
                    Console.WriteLine($" -> {data[key]}");
                });
            }
        }
    }
}
