using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace SuperCache.CodeGen
{
    public class CacheKeyTypeBuilder
    {
        private readonly TransparentCacheTypeBuilder _parent;
        private readonly MethodInfo method;
        private TypeBuilder typeBuilder;
        private ConstructorBuilder ctor;

        public CacheKeyTypeBuilder(TransparentCacheTypeBuilder parent, MethodInfo method)
        {
            _parent = parent;
            this.method = method;
        }

        private Type _sourceType => _parent._sourceType;
        public FieldBuilder[] cacheKeyFields { get; private set; }

        public TypeBuilder TypeBuilder
        {
            get
            {
                if (typeBuilder == null)
                {
                    BuildType();
                }

                return typeBuilder;
            }
        }

        public ConstructorBuilder Constructor
        {
            get
            {
                if (typeBuilder == null)
                {
                    BuildType();
                }

                return ctor;
            }
        }

        private void BuildType()
        {
            var iface = typeof(IFetchable<,>).MakeGenericType(_sourceType, method.ReturnType);

            if (method.ReturnType.IsConstructedGenericType &&
                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                iface = typeof(IFetchableAsync<,>).MakeGenericType(
                    _sourceType,
                    method.ReturnType.GetGenericArguments()[0]);
            }

            typeBuilder = _parent.CacheType.DefineNestedType(
               method.Name + "_Key_" + Guid.NewGuid().ToString("n"),
               TypeAttributes.NestedPublic,
               typeof(object),
               new Type[] { iface });
            var parameters = method.GetParameters();
            var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
            cacheKeyFields = new FieldBuilder[parameterTypes.Length];

            ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);
            var ctorGenerator = ctor.GetILGenerator();

            var fetch = typeBuilder.DefineMethod(
                iface.GetMethods()[0].Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
                method.ReturnType,
                new Type[] { _sourceType });
            var fetchGenerator = fetch.GetILGenerator();
            fetchGenerator.Emit(OpCodes.Ldarg_1);

            var getHashCode = typeBuilder.DefineMethod(
                "GetHashCode",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                typeof(int),
                new Type[0]);
            var getHashCodeGenerator = getHashCode.GetILGenerator();

            var equals = typeBuilder.DefineMethod(
                "Equals",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                typeof(bool),
                new Type[] { typeof(object) });
            var equalsGenerator = equals.GetILGenerator();
            var fieldNotEqual = equalsGenerator.DefineLabel();

            for (int index = 0; index < parameterTypes.Length; index++)
            {
                var cacheKeyField = typeBuilder.DefineField("_" + parameters[index].Name, parameterTypes[index], FieldAttributes.Public | FieldAttributes.InitOnly);
                cacheKeyFields[index] = cacheKeyField;

                // store field in constructor
                ctorGenerator.Emit(OpCodes.Ldarg_0);

                switch (index)
                {
                    case 0:
                        ctorGenerator.Emit(OpCodes.Ldarg_1);
                        break;
                    case 1:
                        ctorGenerator.Emit(OpCodes.Ldarg_2);
                        break;
                    case 2:
                        ctorGenerator.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        ctorGenerator.Emit(OpCodes.Ldarg, index + 1);
                        break;
                }

                ctorGenerator.Emit(OpCodes.Stfld, cacheKeyField);

                var comparer = typeof(EqualityComparer<>).MakeGenericType(cacheKeyField.FieldType);
                var comparerInterface = typeof(IEqualityComparer<>).MakeGenericType(cacheKeyField.FieldType);

                // Equals method
                equalsGenerator.Emit(
                    OpCodes.Call,
                    comparer.GetProperty("Default", BindingFlags.Static | BindingFlags.Public).GetGetMethod());

                equalsGenerator.Emit(OpCodes.Ldarg_0);
                equalsGenerator.Emit(OpCodes.Ldfld, cacheKeyField);
                equalsGenerator.Emit(OpCodes.Ldarg_1);
                equalsGenerator.Emit(OpCodes.Ldfld, cacheKeyField);

                equalsGenerator.Emit(
                    OpCodes.Callvirt,
                    comparerInterface.GetMethod("Equals", new[] { cacheKeyField.FieldType, cacheKeyField.FieldType }));

                equalsGenerator.Emit(OpCodes.Brfalse, fieldNotEqual);

                // GetHashCode
                getHashCodeGenerator.Emit(
                    OpCodes.Call,
                    comparer.GetProperty("Default", BindingFlags.Static | BindingFlags.Public).GetGetMethod());

                getHashCodeGenerator.Emit(OpCodes.Ldarg_0);
                getHashCodeGenerator.Emit(OpCodes.Ldfld, cacheKeyField);

                getHashCodeGenerator.Emit(
                    OpCodes.Callvirt,
                    comparerInterface.GetMethod("GetHashCode", new[] { cacheKeyField.FieldType }));

                if (index > 0)
                {
                    getHashCodeGenerator.Emit(
                        OpCodes.Call,
                        typeof(CacheKeyTypeBuilder).GetMethod("CombineHashCodes", BindingFlags.Static | BindingFlags.Public));
                }

                fetchGenerator.Emit(OpCodes.Ldarg_0);
                fetchGenerator.Emit(OpCodes.Ldfld, cacheKeyField);
            }

            equalsGenerator.Emit(OpCodes.Ldc_I4_1);
            equalsGenerator.Emit(OpCodes.Ret);

            equalsGenerator.MarkLabel(fieldNotEqual);

            // result of arg1.field1.Equals(this.field1) is false; return false
            equalsGenerator.Emit(OpCodes.Ldc_I4_0);
            equalsGenerator.Emit(OpCodes.Ret);

            if (cacheKeyFields.Length == 0)
            {
                getHashCodeGenerator.Emit(OpCodes.Ldc_I4_0);
            }
            getHashCodeGenerator.Emit(OpCodes.Ret);

            fetchGenerator.Emit(OpCodes.Callvirt, method);
            fetchGenerator.Emit(OpCodes.Ret);

            ctorGenerator.Emit(OpCodes.Ret);

            typeBuilder.CreateTypeInfo();
        }

        public static int CombineHashCodes(int h1, int h2)
        {
            uint num = (uint)((h1 << 5) | (int)((uint)h1 >> 27));
            return ((int)num + h1) ^ h2;
        }
    }
}
