using System;
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

        private ModuleBuilder _moduleBuilder { get { return _parent._moduleBuilder; } }
        private Type _sourceType { get { return _parent._sourceType; } }
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
            var hashCode = getHashCodeGenerator.DeclareLocal(typeof(int));

            if (parameterTypes.Length == 0)
            {
                getHashCodeGenerator.Emit(OpCodes.Ldc_I4_0);
            }

            var equals = typeBuilder.DefineMethod(
                "Equals",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                typeof(bool),
                new Type[] { typeof(object) });
            var equalsGenerator = equals.GetILGenerator();
            var fieldNotEqual = equalsGenerator.DefineLabel();

            for (int index = 0; index < parameterTypes.Length; index++)
            {
                cacheKeyFields[index] = typeBuilder.DefineField("_" + parameters[index].Name, parameterTypes[index], FieldAttributes.Public | FieldAttributes.InitOnly);

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

                ctorGenerator.Emit(OpCodes.Stfld, cacheKeyFields[index]);

                equalsGenerator.Emit(OpCodes.Ldarg_0);
                equalsGenerator.Emit(OpCodes.Ldfld, cacheKeyFields[index]);
                equalsGenerator.Emit(OpCodes.Ldarg_1);
                equalsGenerator.Emit(OpCodes.Ldfld, cacheKeyFields[index]);

                switch (Type.GetTypeCode(cacheKeyFields[index].FieldType))
                {
                    case TypeCode.String:
                        equalsGenerator.Emit(OpCodes.Call, typeof(string).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(string) }, null));
                        equalsGenerator.Emit(OpCodes.Brfalse, fieldNotEqual);
                        break;
                    case TypeCode.Object:
                        equalsGenerator.Emit(OpCodes.Call, typeof(object).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(object), typeof(object) }, null));
                        equalsGenerator.Emit(OpCodes.Brfalse, fieldNotEqual);
                        break;
                    default:
                        equalsGenerator.Emit(OpCodes.Bne_Un, fieldNotEqual);
                        break;
                }

                // GetHashCode method
                var skipGetHashCode = getHashCodeGenerator.DefineLabel();

                if (index > 0)
                {
                    getHashCodeGenerator.Emit(OpCodes.Dup);
                    getHashCodeGenerator.Emit(OpCodes.Stloc_0);
                }

                // TODO: do smart things with integer types
                getHashCodeGenerator.Emit(OpCodes.Ldarg_0);
                getHashCodeGenerator.Emit(OpCodes.Ldfld, cacheKeyFields[index]);
                getHashCodeGenerator.Emit(OpCodes.Dup);
                getHashCodeGenerator.Emit(OpCodes.Brfalse, skipGetHashCode);
                if (cacheKeyFields[index].FieldType.IsValueType)
                {
                    getHashCodeGenerator.Emit(OpCodes.Box, cacheKeyFields[index].FieldType);
                }
                getHashCodeGenerator.Emit(OpCodes.Callvirt, typeof(object).GetMethod("GetHashCode"));
                getHashCodeGenerator.MarkLabel(skipGetHashCode);
                // at this point we either have left a zero for this round or the actual hash code

                if (index > 0)
                {
                    getHashCodeGenerator.Emit(OpCodes.Xor);
                    getHashCodeGenerator.Emit(OpCodes.Ldloc_0);
                    getHashCodeGenerator.Emit(OpCodes.Ldc_I4_5);
                    getHashCodeGenerator.Emit(OpCodes.Shl);
                    getHashCodeGenerator.Emit(OpCodes.Add);
                }

                fetchGenerator.Emit(OpCodes.Ldarg_0);
                fetchGenerator.Emit(OpCodes.Ldfld, cacheKeyFields[index]);
            }

            equalsGenerator.Emit(OpCodes.Ldc_I4_1);
            equalsGenerator.Emit(OpCodes.Ret);

            equalsGenerator.MarkLabel(fieldNotEqual);

            // result of arg1.field1.Equals(this.field1) is false; return false
            equalsGenerator.Emit(OpCodes.Ldc_I4_0);
            equalsGenerator.Emit(OpCodes.Ret);

            getHashCodeGenerator.Emit(OpCodes.Ret);

            fetchGenerator.Emit(OpCodes.Callvirt, method);
            fetchGenerator.Emit(OpCodes.Ret);

            ctorGenerator.Emit(OpCodes.Ret);

            typeBuilder.CreateTypeInfo();
        }
    }
}
