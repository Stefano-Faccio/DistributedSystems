namespace DistributedKeyValueStore.NET
{
    internal class Document : IEquatable<Document?>
    {
        private uint version;
        private string value;
        private bool preWriteBlock;
        public string Value => value;
        public uint Version => version;

        public Document(string value)
        {
            this.value = value ?? throw new ArgumentNullException(nameof(value));
            this.version = 0;
            this.preWriteBlock = false;
        }

        public Document(string value, uint version) : this(value)
        {
            this.version = version;
        }

        public Document(string value, uint version, bool preWriteBlock) : this(value, version)
        {
            this.preWriteBlock = preWriteBlock;
        }

        public bool GetPreWriteBlock() { return this.preWriteBlock; }

        public void ClearPreWriteBlock() { this.preWriteBlock = false; }

        public void SetPreWriteBlock() { this.preWriteBlock = true; }

        public Document Update(string value, uint version)
        {
            if (version <= this.version)
                throw new Exception("It is not possible to update a value with a less recent one");

            this.value = value;
            this.version = version;

            //Pulisco il pre-write block poichè la write è avvenuta con successo
            this.ClearPreWriteBlock();

            return this;
        }

        public Document Update(string value)
        {
            return Update(value, this.version + 1);
        }

        public override string? ToString()
        {
            return $"[Value:{this.Value}, Version:{this.version}, PreWriteBlock:{this.preWriteBlock}]";
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Document);
        }

        public bool Equals(Document? other)
        {
            return other is not null &&
                   version == other.version &&
                   value == other.value &&
                   preWriteBlock == other.preWriteBlock;
        }

        public static bool operator ==(Document? left, Document? right)
        {
            //Chiama Equals()
            return EqualityComparer<Document>.Default.Equals(left, right);
        }

        public static bool operator !=(Document? left, Document? right)
        {
            return !(left == right);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(version, value, preWriteBlock);
        }

        public static void Main(string[] args)
        {
            Document foo1 = new Document("Ciao");
            Document foo2 = new Document("Bella", 5);
            Console.WriteLine(foo1);
            Console.WriteLine(foo2);

            foo1.SetPreWriteBlock();
            Console.WriteLine(foo1);
            foo1.Update("Balla", 3);
            Console.WriteLine(foo1);
            try
            {
                foo1.Update("Danza", 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine(foo1);

            try
            {
                foo2.Update("Pop", 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine(foo2);
            foo2.SetPreWriteBlock();
            Console.WriteLine(foo2);
            foo2.ClearPreWriteBlock();
            Console.WriteLine(foo2);
            foo2.Update("Pop");
            Console.WriteLine(foo2);

            Console.WriteLine("foo1.Equals(foo2): " + foo1.Equals(foo2));
            Console.WriteLine("foo2.Equals(foo1): " + foo2.Equals(foo1));
            Console.WriteLine($"foo1 == foo2: {foo1 == foo2}");
            Console.WriteLine($"foo1 != foo2: {foo1 != foo2}");

            Console.WriteLine("foo1 Hashcode: " + foo1.GetHashCode());
            Console.WriteLine("foo2 Hashcode: " + foo2.GetHashCode());

            Console.ReadKey();
        }
    }
}
