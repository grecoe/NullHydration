using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;

namespace NullHydration
{

    public class NullPropertyHydrator<T> : DynamicObject
    {
        public T _inner { get; private set; }

        public NullPropertyHydrator(T inner)
        {
            _inner = inner ?? Activator.CreateInstance<T>();
            this.HydrateNullProperties();
        }

        protected void HydrateNullProperties()
        {
            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                var binder = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(
                    CSharpBinderFlags.None,
                    prop.Name,
                    typeof(T),
                    new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) }
                    );

                var callSite = CallSite<Func<CallSite, object, object>>.Create(binder);

                // Invoke the binder to get the property value which will trigger the TryGetMember and 
                // populate any null property.
                _ = callSite.Target(callSite, this);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            var prop = typeof(T).GetProperty(binder.Name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                bool isCreated = false;
                var value = prop.GetValue(_inner);
                result = value ?? GetDefaultValue(prop.PropertyType, out isCreated);
                if (isCreated && result != null)
                {
                    prop.SetValue(_inner, result);
                }
                return true;
            }

            result = null;
            return false;
        }

        private object? GetDefaultValue(Type type, out bool isCreated)
        {

            isCreated = true;
            // Handle strings
            if (type == typeof(string))
                return string.Empty;

            object? returnValue = null;
            if (type.IsAssignableFrom(typeof(Guid)))
            {
                returnValue = Guid.Empty;
            }
            else if (type.IsAssignableFrom(typeof(DateTime)))
            {
                returnValue = DateTime.UtcNow;
            }
            else if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
            {
                var elementType = type.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                returnValue = Activator.CreateInstance(listType);
            }
            else if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
            {
                var elementTypes = type.GetGenericArguments();
                var dictType = typeof(Dictionary<,>).MakeGenericType(elementTypes[0], elementTypes[1]);
                returnValue = Activator.CreateInstance(dictType);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
            {
                var elementType = type.GetGenericArguments()[0];
                var enumType = typeof(List<>).MakeGenericType(elementType);
                returnValue = Activator.CreateInstance(enumType);
            }
            else if (!type.IsValueType && type != typeof(string))
            {
                // Handle nested classes (reference types)
                var instance = Activator.CreateInstance(type);
                var wrapperType = typeof(NullPropertyHydrator<>).MakeGenericType(type);
                var returnValidator = Activator.CreateInstance(wrapperType, instance);
                if (returnValidator != null)
                {
                    dynamic var = (dynamic)returnValidator;
                    var.HydrateNullProperties();
                    returnValue = var._inner;
                }
            }
            else
            {
                // Handle value types, if it's also nullable then get the underlying type.
                var underlyingType = Nullable.GetUnderlyingType(type);
                returnValue = Activator.CreateInstance(underlyingType ?? type);
            }

            isCreated = returnValue != null;
            return returnValue;
        }
    }
}
