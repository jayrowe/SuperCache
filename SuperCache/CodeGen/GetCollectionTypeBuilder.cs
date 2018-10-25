using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SuperCache.CodeGen
{
    class GetCollectionTypeBuilder : KeyedCacheDelegatingTypeBuilder
    {
        public GetCollectionTypeBuilder(ModuleBuilder moduleBuilder, TransparentCacheTypeBuilder cacheTypeBuilder)
            : base(moduleBuilder, cacheTypeBuilder)
        {
        }

        public FieldBuilder ResultField { get; private set; }

        protected override string TypeName
        {
            get
            {
                return "CollectionFinder";
            }
        }

        protected override string TargetMethod
        {
            get
            {
                return "NoOp";
            }
        }

        protected override ConstructorBuilder BuildConstructor()
        {
            ResultField = _typeBuilder.DefineField("##KeyedCacheFetcher", typeof(KeyedCacheFetcher), FieldAttributes.Public);

            var constructor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { _cacheTypeBuilder.CacheType, typeof(KeyedCacheFetcher) });
            var ctorGenerator = constructor.GetILGenerator();
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Ldarg_1);
            ctorGenerator.Emit(OpCodes.Stfld, CacheField);
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Ldarg_2);
            ctorGenerator.Emit(OpCodes.Stfld, ResultField);
            ctorGenerator.Emit(OpCodes.Ret);
            return constructor;
        }

        protected override void EmitPreCollectionLoad(ILGenerator methodGenerator, FieldBuilder field)
        {
            base.EmitPreCollectionLoad(methodGenerator, field);

            methodGenerator.Emit(OpCodes.Ldarg_0);
            methodGenerator.Emit(OpCodes.Ldfld, ResultField);

            methodGenerator.Emit(OpCodes.Ldarg_0);
            methodGenerator.Emit(OpCodes.Ldfld, CacheField);
            methodGenerator.Emit(OpCodes.Ldfld, field);

            methodGenerator.Emit(OpCodes.Stfld, typeof(KeyedCacheFetcher).GetField("KeyedCache"));
        }
    }
}
