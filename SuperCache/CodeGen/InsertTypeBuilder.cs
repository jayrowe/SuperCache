using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SuperCache.CodeGen
{
    class InsertTypeBuilder : KeyedCacheDelegatingTypeBuilder
    {
        public InsertTypeBuilder(ModuleBuilder moduleBuilder, TransparentCacheTypeBuilder cacheTypeBuilder)
            : base(moduleBuilder, cacheTypeBuilder)
        {
        }

        public FieldBuilder ResultField { get; private set; }

        protected override string TypeName
        {
            get
            {
                return "Insert";
            }
        }

        protected override string TargetMethod
        {
            get
            {
                return "Insert";
            }
        }

        protected override ConstructorBuilder BuildConstructor()
        {
            ResultField = _typeBuilder.DefineField("##Result", typeof(object), FieldAttributes.Private);

            var constructor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { _cacheTypeBuilder.CacheType, typeof(object) });
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

        protected override void EmitLoadParameters(ILGenerator methodGenerator, MethodInfo method)
        {
            methodGenerator.Emit(OpCodes.Ldarg_0);
            methodGenerator.Emit(OpCodes.Ldfld, ResultField);

            base.EmitLoadParameters(methodGenerator, method);
        }
    }
}
