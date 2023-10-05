using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DistributedKeyValueStore.NET
{
    //https://stackoverflow.com/questions/1273139/c-sharp-java-hashmap-equivalent
    internal class Collection 
    {
        private Dictionary<uint, Document> dictionary;

        //Todo implementare il comparer

        public Collection()
        {
            dictionary = new Dictionary<uint, Document>();
        }

        //Ritorna il documento inserito
        public Document? Add(uint key, Document? document)
        {
            //Sintassi interessante per controllare che un valore non sia null
            _ = document ?? throw new ArgumentNullException(nameof(document));
            //Aggiunta nuovo documento
            dictionary.Add(key, document);
            return document;
        }

        public Document? Add(uint key, string value, uint version, bool preWriteBlock)
        {
            return Add(key, new Document(value, version, preWriteBlock));
        }

        public Document? Add(uint key, string value, uint version)
        {
            return Add(key, value, version, false);
        }

        public Document? Add(uint key, string value)
        {
            return Add(key, value, 0);
        }

        //Ritorna il documento aggiornato
        public Document? Update(uint key, string value)
        {
            return dictionary[key].Update(value);
        }

        public Document? Update(uint key, string value, uint version)
        {
            return dictionary[key].Update(value, version);
        }

        public void ClearPreWriteBlock(uint key) 
        {
            dictionary[key].ClearPreWriteBlock(); 
        }

        public void SetPreWriteBlock(uint key) 
        {
            dictionary[key].SetPreWriteBlock();
        }

        //Ritorna il documento rimosso
        public Document? Remove(uint key)
        {
            dictionary.Remove(key, out Document? document);

            return document;
        }

        public Document this[uint key]
        {
            get => dictionary[key];
            set
            {   
                if(value is null)
                {
                    //Rimozione documento
                    Remove(key);
                }
                else if(dictionary.ContainsKey(key))
                {
                    //Aggiornamento dell'intero documento
                    //I.E. Cambio di riferimento. Non permesso
                    throw new Exception("It is not possible to change the document associated with a key!");
                }
                else {
                    //Aggiunta nuovo documento
                    Add(key, value);
                }
            }
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public override string? ToString()
        {
            string str = "";
            foreach (KeyValuePair<uint, Document> kvp in dictionary)
            {
                str += kvp.Key + ": "+ kvp.Value.ToString() + "\n";
            }
            return str;
        }

        public static void Main(string[] args)
        {
            Collection coll = new Collection();
            try
            {
                Console.WriteLine(coll[5]);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            coll[5] = new Document("Ciao");
            Console.WriteLine(coll);
            coll[5].Update("Bella");
            Console.WriteLine(coll);
            try
            {
                coll[5] = new Document("Male");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine(coll);
            coll.Add(7, "Mela");
            coll[7].SetPreWriteBlock();
            Console.WriteLine(coll);

            coll.Add(10, "Pera", 10, true);
            coll[10].ClearPreWriteBlock();
            Console.WriteLine(coll);

            Console.ReadKey();
        }

    }
}
