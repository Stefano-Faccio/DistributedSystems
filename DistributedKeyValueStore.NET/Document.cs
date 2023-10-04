using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal class Document
    {
        private readonly uint key;
        private uint version;
        private string value;
        private bool preWriteBlock;
        public uint Key => key;
        public string Value => value;
        public uint Version => version;

        public Document(uint key, string value)
        {
            this.key = key;
            this.value = value;
            this.version = 0;
            this.preWriteBlock = false;
        }

        public Document(uint key, string value, uint version) : this(key, value)
        {
            this.version = version;
        }

        public void ClearPreWriteBlock() { this.preWriteBlock = false; }

        public void SetPreWriteBlock() { this.preWriteBlock = true; }

        public void Update(string value, uint version)
        {
            if (version <= this.version)
                throw new Exception("It is not possible to update a value with a less recent one");

            this.value = value;
            this.version = version;

            //Pulisco il pre-write block poichè la write è avvenuta con successo
            this.ClearPreWriteBlock();
        }

        public override string? ToString()
        {
            return $"[Key:{this.key}, Value:{this.Value}, Version:{this.version}, PreWriteBlock:{this.preWriteBlock}]";
        }
    }
}
