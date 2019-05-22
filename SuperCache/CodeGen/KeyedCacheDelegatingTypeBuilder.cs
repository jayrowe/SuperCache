using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace SuperCache.CodeGen
{
    public abstract class KeyedCacheDelegatingTypeBuilder
    {
        private readonly ModuleBuilder _moduleBuilder;
        protected readonly TransparentCacheTypeBuilder _cacheTypeBuilder;
        protected TypeBuilder _typeBuilder;

        public KeyedCacheDelegatingTypeBuilder(ModuleBuilder moduleBuilder, TransparentCacheTypeBuilder cacheTypeBuilder)
        {
            _moduleBuilder = moduleBuilder;
            _cacheTypeBuilder = cacheTypeBuilder;
        }

        protected virtual string TypeName => TargetMethod;

        protected virtual Type TargetType => typeof(KeyedCacheBase<,,>);

        protected virtual Type TargetTypeAsync => TargetType;

        protected abstract string TargetMethod { get; }

        protected virtual string TargetMethodAsync => TargetMethod;

        public TypeBuilder Type
        {
            get
            {
                if (_typeBuilder == null)
                {
                    BuildType();
                }

                return _typeBuilder;
            }
        }

        private ConstructorBuilder _constructor;

        public ConstructorBuilder Constructor
        {
            get
            {
                if (_typeBuilder == null)
                {
                    BuildType();
                }

                return _constructor;
            }
        }

        protected FieldBuilder CacheField
        {
            get; private set;
        }

        private void BuildType()
        {
            _typeBuilder = _cacheTypeBuilder.CacheType.DefineNestedType(TypeName, TypeAttributes.NestedPrivate, typeof(object), new Type[] { _cacheTypeBuilder.SourceType });

            CacheField = _typeBuilder.DefineField("_cache", _cacheTypeBuilder.CacheType, FieldAttributes.Private | FieldAttributes.InitOnly);
            _constructor = BuildConstructor();

            foreach (var method in _cacheTypeBuilder.SourceType.GetMethods())
            {
                var methodBuilder = _typeBuilder.DefineMethod(
                   method.Name,
                   MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                   method.ReturnType,
                   method.GetParameters().Select(p => p.ParameterType).ToArray());

                var methodGenerator = methodBuilder.GetILGenerator();

                if (method.ReturnType == typeof(void) ||
                    method.GetParameters().Any(p => p.ParameterType.IsByRef) ||
                    method.ReturnType == typeof(Task))
                {
                    EmitUncachedCall(methodGenerator, method);
                }
                else
                {
                    var cacheKeyType = _cacheTypeBuilder.CacheKeyMap[method];
                    var field = _cacheTypeBuilder.FieldMap[method];

                    EmitPreCollectionLoad(methodGenerator, field);

                    methodGenerator.Emit(OpCodes.Ldarg_0);
                    methodGenerator.Emit(OpCodes.Ldfld, CacheField);
                    methodGenerator.Emit(OpCodes.Ldfld, field);

                    EmitLoadParameters(methodGenerator, method);
                    
                    methodGenerator.Emit(OpCodes.Newobj, cacheKeyType.Constructor);

                    if (method.ReturnType.IsConstructedGenericType &&
                        method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        methodGenerator.Emit(OpCodes.Call,
                            TypeBuilder.GetMethod(
                                TargetTypeAsync.MakeGenericType(_cacheTypeBuilder.SourceType, cacheKeyType.TypeBuilder, method.ReturnType.GetGenericArguments()[0]),
                                TargetTypeAsync.GetMethod(TargetMethodAsync)));
                    }
                    else
                    {
                        methodGenerator.Emit(OpCodes.Call,
                            TypeBuilder.GetMethod(
                                TargetType.MakeGenericType(_cacheTypeBuilder.SourceType, cacheKeyType.TypeBuilder, method.ReturnType),
                                TargetType.GetMethod(TargetMethod)));
                    }

                    methodGenerator.Emit(OpCodes.Ret);
                }
            }
        }

        protected virtual ConstructorBuilder BuildConstructor()
        {
            var constructor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { _cacheTypeBuilder.CacheType });
            var ctorGenerator = constructor.GetILGenerator();
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Ldarg_1);
            ctorGenerator.Emit(OpCodes.Stfld, CacheField);
            ctorGenerator.Emit(OpCodes.Ret);
            return constructor;
        }

        protected virtual void EmitLoadParameters(ILGenerator methodGenerator, MethodInfo method)
        {
            var parameters = method.GetParameters();

            for (int index = 0; index < parameters.Length; index++)
            {
                switch (index)
                {
                    case 0:
                        methodGenerator.Emit(OpCodes.Ldarg_1);
                        break;
                    case 1:
                        methodGenerator.Emit(OpCodes.Ldarg_2);
                        break;
                    case 2:
                        methodGenerator.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        methodGenerator.Emit(OpCodes.Ldarg, index + 1);
                        break;
                }
            }
        }

        protected virtual void EmitUncachedCall(ILGenerator methodGenerator, MethodInfo method)
        {
            // not cached; do nothing
            if (method.ReturnType != typeof(void))
            {
                methodGenerator.DeclareLocal(method.ReturnType);
                methodGenerator.Emit(OpCodes.Ldloca, 0);
                methodGenerator.Emit(OpCodes.Initobj, method.ReturnType);
                methodGenerator.Emit(OpCodes.Ldloc_0);
            }

            methodGenerator.Emit(OpCodes.Ret);
        }

        protected virtual void EmitPreCollectionLoad(ILGenerator methodGenerator, FieldBuilder field)
        {
        }
    }
}
