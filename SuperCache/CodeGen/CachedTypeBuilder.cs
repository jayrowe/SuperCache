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

        protected override string TypeName
        {
            get
            {
                return "Cached";
            }
        }

        protected override string TargetMethod
        {
            get
            {
                return "Get";
            }
        }

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
