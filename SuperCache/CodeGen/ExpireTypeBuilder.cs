using System.Reflection.Emit;

namespace SuperCache.CodeGen
{
    public class ExpireTypeBuilder : KeyedCacheDelegatingTypeBuilder
    {
        public ExpireTypeBuilder(ModuleBuilder moduleBuilder, TransparentCacheTypeBuilder cacheTypeBuilder)
            : base(moduleBuilder, cacheTypeBuilder)
        {
        }

        protected override string TargetMethod
        {
            get
            {
                return "Expire";
            }
        }
    }
}
