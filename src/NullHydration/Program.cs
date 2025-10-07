namespace NullHydration
{
    #region Classes that will be hydrated upon construction.
    public enum TestEnum
    {
        First,
        Second
    }

    public class DeepObject
    {
        public List<string>? DeepList { get; set; }
        public Dictionary<string, int>? DeepDict { get; set; }
    }

    public class EmbeddedObject
    {
        public List<string>? EmbeddedList { get; set; }
        public Dictionary<string, int>? EmbeddedDict { get; set; }
        public DeepObject? Deep { get; set; }
    }

    public class OuterRecord
    {
        public TestEnum? TestVal { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public Guid? Id { get; set; }
        public DateTime? Time { get; set; }
        public List<string>? OuterList { get; set; }
        public Dictionary<string, int>? OuterDict { get; set; }
        public EmbeddedObject? Embedded { get; set; }

        public OuterRecord()
        {
        }

        public void EnsureTrackedState()
        {
            new NullPropertyHydrator<OuterRecord>(this);
        }
    }
    #endregion

    internal class Program
    {
        static void Main(string[] args)
        {
            // EF will generate a record, but anything missing fromt the document is set to 
            // null as no parameterless constructor is called, so we can't track state there.
            var outerRecord = new OuterRecord();

            // EF populates what it can find. 
            outerRecord.OuterList = new List<string>() { "Some value" };

            // Using the EnsureTrackedState methodology we call it either after it's constructed or
            // at least before it's stored. This will only hydrate any property that is null and leaves all
            // other properties as is.
            outerRecord.EnsureTrackedState();

            // Now iterate over the properties to ensure nothing is null. 
            var propInfo = typeof(OuterRecord).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach(var prop in propInfo)
            {
                var val = prop.GetValue(outerRecord);
                Console.WriteLine($"Property {prop.Name} null: {(val == null ? true : false)}");
            }
        }
    }
}
