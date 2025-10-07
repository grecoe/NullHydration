namespace NullHydration
{
    #region Classes that will be hydrated upon construction.

    public class BaseDocument
    {
        public Guid? RecordId { get; set; }

        /// <summary>
        /// Collection classes inherit from this BaseDocument and no additional work is required by 
        /// the deriving class, the functionality comes for free.
        /// </summary>
        public void EnsureTrackedState() 
        {
            var type = this.GetType();

            var wrapperType = typeof(NullPropertyHydrator<>).MakeGenericType(type);
            var returnValidator = Activator.CreateInstance(wrapperType, this);
            if (returnValidator != null)
            {
                dynamic var = (dynamic)returnValidator;
                var.HydrateNullProperties();
            }
        }
    }

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


    public class NonDerivedCollectionObject
    {
        public Dictionary<string, int>? OuterDict { get; set; }
        public EmbeddedObject? Embedded { get; set; }

        /// <summary>
        /// Other classes that do not derive from BaseDocument can implement this on 
        /// thier own.
        /// </summary>
        public void EnsureTrackedState()
        {
            new NullPropertyHydrator<NonDerivedCollectionObject>(this);
        }
    }

    // Class derives from BaseDocument and does not need to implement EnsureTrackedState
    public class DerivedCollectionObject : BaseDocument
    {
        public TestEnum? TestVal { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public Guid? Id { get; set; }
        public DateTime? Time { get; set; }
        public List<string>? OuterList { get; set; }
        public Dictionary<string, int>? OuterDict { get; set; }
        public EmbeddedObject? Embedded { get; set; }
    }
    #endregion

    internal class Program
    {
        static void Main(string[] args)
        {
            // EF will generate a record, but anything missing fromt the document is set to 
            // null as no parameterless constructor is called, so we can't track state there.
            var derivedObject = new DerivedCollectionObject();
            var nonDerivedObject = new NonDerivedCollectionObject();

            // EF populates what it can find. 
            derivedObject.OuterList = new List<string>() { "Some value" };
            nonDerivedObject.OuterDict = new Dictionary<string, int>();

            // Using the EnsureTrackedState methodology we call it either after it's constructed or
            // at least before it's stored. This will only hydrate any property that is null and leaves all
            // other properties as is.

            // Call on both derived and non derived classes
            derivedObject.EnsureTrackedState();
            nonDerivedObject.EnsureTrackedState();

            // Iterate both objects to ensure non null properties.
            object[] objects = [derivedObject, nonDerivedObject];

            foreach( var obj in objects)
            {
                var objType = obj.GetType();
                Console.WriteLine(objType.Name);

                var propInfo = objType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var prop in propInfo)
                {
                    var val = prop.GetValue(obj);
                    Console.WriteLine($"Property {prop.Name} null: {(val == null ? true : false)}");
                }
            }
        }
    }
}
