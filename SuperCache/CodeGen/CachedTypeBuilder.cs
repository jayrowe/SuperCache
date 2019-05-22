using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SuperCache.CodeGen
{
    public class CachedTypeBuilder : KeyedCacheDelegatingTypeBuilder
    {
        public CachedTypeBuilder(ModuleBuilder moduleBuilder, TransparentCacheTypeBuilder cacheTypeBuilder)
            : base(moduleBuilder, cacheTypeBuilder)
        {
        }

        protected override string TypeName => "Cached";
        protected override Type TargetType => typeof(KeyedCacheSync<,,>);
        protected override Type TargetTypeAsync => typeof(KeyedCacheAsync<,,>);
        protected override string TargetMethod => "Get";
        protected override string TargetMethodAsync => "GetAsync";

        protected override void EmitUncachedCall(ILGenerator methodGenerator, MethodInfo method)
        {
            methodGenerator.Emit(OpCodes.Ldarg_0);
            methodGenerator.Emit(OpCodes.Ldfld, CacheField);
            methodGenerator.Emit(OpCodes.Ldfld, _cacheTypeBuilder.SourceField);

            EmitLoadParameters(methodGenerator, method);

            methodGenerator.Emit(OpCodes.Callvirt, method);
            methodGenerator.Emit(OpCodes.Ret);
        }
    }
}
