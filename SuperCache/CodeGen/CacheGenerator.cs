using System;
using System.Reflection.Emit;

namespace SuperCache.CodeGen
{
    public class CacheGenerator
    {
        private readonly Type _type;

        private readonly AssemblyBuilder _assemblyBuilder;
        private ModuleBuilder _moduleBuilder;
        private TransparentCacheTypeBuilder _cacheTypeBuilder;

        public CacheGenerator(Type type)
        {
            _type = type;
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new System.Reflection.AssemblyName(type.FullName + ".SuperCache"),
                AssemblyBuilderAccess.RunAndCollect);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("_module");
        }

        public Type CacheType
        {
            get
            {
                if (_cacheTypeBuilder == null)
                {
                    _cacheTypeBuilder = new TransparentCacheTypeBuilder(_moduleBuilder, _type);
                }

                return _cacheTypeBuilder.FinishedType;
            }
        }
    }
}
