using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    //https://stackoverflow.com/questions/1273139/c-sharp-java-hashmap-equivalent
    internal class Collection 
    {
        private Dictionary<uint, Document> dictionary;

        public Collection()
        {
            dictionary = new Dictionary<uint, Document>();
        }

        public void Add(uint key, string value, uint version) 
        {
            Document document = new Document(key, value, version);
        }

        public void Add(uint key, string value)
        {
            Add(key, value, 0);
        }

        public Document Remove(uint key)
        {
            Document tmp = this[key];

            dictionary.Remove(key);

            return tmp;
        }

        //Overload di []
        public Document this[uint key]
        {
            get => dictionary[key];
            //set => SetValue(key, value);
        }
    }
}
