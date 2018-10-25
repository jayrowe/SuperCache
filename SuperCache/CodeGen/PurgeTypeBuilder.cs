using System.Reflection.Emit;

namespace SuperCache.CodeGen
{
    public class PurgeTypeBuilder : KeyedCacheDelegatingTypeBuilder
    {
        public PurgeTypeBuilder(ModuleBuilder moduleBuilder, TransparentCacheTypeBuilder cacheTypeBuilder)
            : base(moduleBuilder, cacheTypeBuilder)
        {
        }

        protected override string TargetMethod
        {
            get
            {
                return "Purge";
            }
        }
    }
}
