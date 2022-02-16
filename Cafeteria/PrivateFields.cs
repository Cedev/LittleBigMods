using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cafeteria
{
    public class PrivateFields<T, TThis> where TThis: new()
    {

        public static TThis Of(T instance)
        {
            return constructor.Value(instance);
        }

        public static Func<T, TThis> MakeConstructor()
        {
            var fields = new List<Tuple<PropertyInfo, Func<T, object>>>();
            foreach (var property in typeof(TThis).GetProperties())
            {
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(IField<>))
                {
                    var fieldType = property.PropertyType.GetGenericArguments()[0];
                    var field = typeof(Yoink<T>).GetMethod("Field").MakeGenericMethod(fieldType).Invoke(null, new object[] { property.Name });
                    fields.Add(Tuple.Create(property, (Func<T, object>) field));
                }
            }

            return t =>
            {
                var result = new TThis();
                foreach (var field in fields)
                {
                    field.Item1.SetValue(result, field.Item2(t));
                }
                return result;
            };
        }

        public static void Compile()
        {
            _ = constructor.Value;
        }

        private static Lazy<Func<T, TThis>> constructor = new Lazy<Func<T, TThis>>(MakeConstructor);
          
    }


    public interface IField<F>
    {
        F Value { get; set; }
    }

    public class FieldProxy<T, F> : IField<F>
    {
        private readonly T instance;
        private readonly FieldInfo field;

        public FieldProxy(T instance, FieldInfo field)
        {
            this.instance = instance;
            this.field = field;
        }

        public F Value
        {
            get
            {
                return (F) field.GetValue(instance);
            }
            set
            {
                field.SetValue(instance, value);
            }
        }

        public static IField<F> Make(T i, FieldInfo fieldInfo)
        {
            if (i == null || fieldInfo == null)
            {
                return NullProxy<F>.Instance;
            }
            return new FieldProxy<T, F>(i, fieldInfo);
        }
    }

    public class NullProxy<F> : IField<F>
    {
        public F Value { get { return default(F); } set { } }

        public static NullProxy<F> Instance = new NullProxy<F>();
    }

    public static class Yoink<T>
    {
        public static Func<T, IField<F>> Field<F>(string name)
        {
            var fieldInfo = FieldInfo<F>(name);
            return i => FieldProxy<T, F>.Make(i, fieldInfo);
        }

        public static FieldInfo FieldInfo<F>(string name)
        {
            var fieldInfo = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fieldInfo == null)
            {
                Error("CafeteriaMod couldn't find {0}.{1} private field", typeof(T).FullName, name);
                return null;
            }
            if (!fieldInfo.FieldType.IsAssignableFrom(typeof(F)) || !typeof(F).IsAssignableFrom(fieldInfo.FieldType))
            {
                Error("CafeteriaMod can't convert {0}.{1} of type {2} to and from {3}", typeof(T).FullName, name, fieldInfo.FieldType.FullName, typeof(F).FullName);
                return null;
            }
            return fieldInfo;
        }

        public static void Error(string format, params object[] args)
        {
            try
            {
                MelonLogger.Error(format, args);
            }
            catch (Exception e)
            {
                Console.WriteLine(format, args);
            }
        }
    }
}
