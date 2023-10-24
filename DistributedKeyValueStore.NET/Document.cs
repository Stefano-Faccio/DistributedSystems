namespace DistributedKeyValueStore.NET
{
    internal class Document : IEquatable<Document?>
    {
        public string Value { get; private set; }
        public bool PreWriteBlock { get; private set; }
        public uint Version { get; private set; }

        public Document(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Version = 0;
            PreWriteBlock = false;
        }

        public Document(string value, uint version) : this(value)
        {
            Version = version;
        }

        public Document(string value, uint version, bool preWriteBlock) : this(value, version)
        {
            PreWriteBlock = preWriteBlock;
        }

        public bool GetPreWriteBlock() { return this.PreWriteBlock; }

        public void ClearPreWriteBlock() { this.PreWriteBlock = false; }

        public void SetPreWriteBlock() { this.PreWriteBlock = true; }

        public Document Update(string value, uint version)
        {
            if (version <= Version)
                throw new Exception("It is not possible to update a Value with a less recent one");

            Value = value;
            Version = version;

            //Pulisco il pre-write block poichè la write è avvenuta con successo
            ClearPreWriteBlock();

            return this;
        }

        public Document Update(string value)
        {
            return Update(value, Version + 1);
        }

        public override string? ToString()
        {
            return $"[Value:{Value}, Version:{Version}, PreWriteBlock:{PreWriteBlock}]";
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Document);
        }

        public bool Equals(Document? other)
        {
            return other is not null &&
                   Version == other.Version &&
                   Value == other.Value &&
                   PreWriteBlock == other.PreWriteBlock;
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
            return HashCode.Combine(Version, Value, PreWriteBlock);
        }

        public static void Main(string[] args)
        {
            Document foo1 = new("Ciao");
            Document foo2 = new("Bella", 5);
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
