using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SuperCache.CodeGen
{
    public class TransparentCacheTypeBuilder
    {
        public readonly ModuleBuilder _moduleBuilder;
        public readonly Type _sourceType;
        private FieldBuilder _sourceField;
        private ILGenerator _ctorGenerator;
        private TypeBuilder _cacheType;
        private TypeInfo _finishedType;

        public TransparentCacheTypeBuilder(ModuleBuilder moduleBuilder, Type sourceType)
        {
            _moduleBuilder = moduleBuilder;
            _sourceType = sourceType;
        }

        public Type SourceType
        {
            get
            {
                return _sourceType;
            }
        }

        public TypeBuilder CacheType
        {
            get
            {
                if (_cacheType == null)
                {
                    CreateCacheType();
                }

                return _cacheType;
            }
        }

        public TypeInfo FinishedType
        {
            get
            {
                if (_finishedType == null)
                {
                    CreateCacheType();
                }

                return _finishedType;
            }
        }

        private Dictionary<MethodInfo, FieldBuilder> _fieldMap = new Dictionary<MethodInfo, FieldBuilder>();

        public IDictionary<MethodInfo, FieldBuilder> FieldMap
        {
            get
            {
                if (_cacheType == null)
                {
                    CreateCacheType();
                }

                return new ReadOnlyDictionary<MethodInfo, FieldBuilder>(_fieldMap);
            }
        }

        private Dictionary<MethodInfo, CacheKeyTypeBuilder> _cacheKeyMap = new Dictionary<MethodInfo, CacheKeyTypeBuilder>();

        public IDictionary<MethodInfo, CacheKeyTypeBuilder> CacheKeyMap
        {
            get
            {
                if (_cacheKeyMap == null)
                {
                    CreateCacheType();
                }

                return new ReadOnlyDictionary<MethodInfo, CacheKeyTypeBuilder>(_cacheKeyMap);
            }
        }

        private void CreateCacheType()
        {
            _cacheType = _moduleBuilder.DefineType(
                "SuperCacheDyn." + _sourceType.FullName,
                TypeAttributes.Public,
                typeof(object),
                new Type[] { typeof(ITransparentCacheInternal<>).MakeGenericType(_sourceType) });

            BeginCtor();

            foreach (var method in _sourceType.GetMethods())
            {
                if (method.ReturnType == typeof(void) || method.GetParameters().Any(p => p.ParameterType.IsByRef))
                {
                    continue;
                }

                var cacheKeyType = GenerateCacheKeyType(method);
                _cacheKeyMap[method] = cacheKeyType;
                var cacheSlotType = typeof(KeyedCacheSlot);

                var cacheType = typeof(KeyedCache<,,>).MakeGenericType(_sourceType, cacheKeyType.TypeBuilder, method.ReturnType);
                var cacheField = _cacheType.DefineField("_" + cacheSlotType.Name, cacheType, FieldAttributes.Private);

                _fieldMap[method] = cacheField;

                // initialize a KeyedCache instance in the ctor
                _ctorGenerator.Emit(OpCodes.Ldarg_0);
                _ctorGenerator.Emit(OpCodes.Ldarg_1);
                _ctorGenerator.Emit(OpCodes.Newobj, TypeBuilder.GetConstructor(cacheType, typeof(KeyedCache<,,>).GetConstructors()[0]));
                _ctorGenerator.Emit(OpCodes.Stfld, cacheField);
            }

            CloseCtor();

            var purgeTypeBuilder = new PurgeTypeBuilder(_moduleBuilder, this);

            var getPurgeInterfaceMethod = _cacheType.DefineMethod(
                "GetPurgeInterface",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                _sourceType,
                new Type[0]);
            var getPurgeInterfaceBuilder = getPurgeInterfaceMethod.GetILGenerator();
            getPurgeInterfaceBuilder.Emit(OpCodes.Ldarg_0);
            getPurgeInterfaceBuilder.Emit(OpCodes.Newobj, purgeTypeBuilder.Constructor);
            getPurgeInterfaceBuilder.Emit(OpCodes.Ret);

            var expireTypeBuilder = new ExpireTypeBuilder(_moduleBuilder, this);

            var getExpireInterfaceMethod = _cacheType.DefineMethod(
                "GetExpireInterface",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                _sourceType,
                new Type[0]);
            var getExpireInterfaceBuilder = getExpireInterfaceMethod.GetILGenerator();
            getExpireInterfaceBuilder.Emit(OpCodes.Ldarg_0);
            getExpireInterfaceBuilder.Emit(OpCodes.Newobj, expireTypeBuilder.Constructor);
            getExpireInterfaceBuilder.Emit(OpCodes.Ret);

            var cacheTypeBuilder = new CachedTypeBuilder(_moduleBuilder, this);

            var getCacheInterfaceMethod = _cacheType.DefineMethod(
               "GetCachedInterface",
               MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
               _sourceType,
               new Type[0]);
            var getCacheInterfaceBuilder = getCacheInterfaceMethod.GetILGenerator();
            getCacheInterfaceBuilder.Emit(OpCodes.Ldarg_0);
            getCacheInterfaceBuilder.Emit(OpCodes.Newobj, cacheTypeBuilder.Constructor);
            getCacheInterfaceBuilder.Emit(OpCodes.Ret);

            var insertTypeBuilder = new InsertTypeBuilder(_moduleBuilder, this);

            var getInsertInterface = _cacheType.DefineMethod(
                "GetInsertInterface",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                _sourceType,
                new Type[] { typeof(object) });

            var getInsertInterfaceBuilder = getInsertInterface.GetILGenerator();

            getInsertInterfaceBuilder.Emit(OpCodes.Ldarg_0);
            getInsertInterfaceBuilder.Emit(OpCodes.Ldarg_1);
            getInsertInterfaceBuilder.Emit(OpCodes.Newobj, insertTypeBuilder.Constructor);
            getInsertInterfaceBuilder.Emit(OpCodes.Ret);

            var getCollectionInterfaceMethod = _cacheType.DefineMethod(
                "GetCollectionFinderInterface",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                _sourceType,
                new Type[] { typeof(KeyedCacheFetcher) });

            var getCollectionTypeBuilder = new GetCollectionTypeBuilder(_moduleBuilder, this);
            var getCollectionInterfaceBuilder = getCollectionInterfaceMethod.GetILGenerator();

            getCollectionInterfaceBuilder.Emit(OpCodes.Ldarg_0);
            getCollectionInterfaceBuilder.Emit(OpCodes.Ldarg_1);
            getCollectionInterfaceBuilder.Emit(OpCodes.Newobj, getCollectionTypeBuilder.Constructor);
            getCollectionInterfaceBuilder.Emit(OpCodes.Ret);

            var getAllCollectionsMethod = _cacheType.DefineMethod(
                "GetAllCollections",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(IKeyedCache[]),
                new Type[0]);

            var getAllCollectionsBuilder = getAllCollectionsMethod.GetILGenerator();
            getAllCollectionsBuilder.Emit(OpCodes.Ldc_I4, _fieldMap.Values.Count);
            getAllCollectionsBuilder.Emit(OpCodes.Newarr, typeof(IKeyedCache));

            int index = 0;
            foreach (var cacheField in _fieldMap.Values)
            {
                getAllCollectionsBuilder.Emit(OpCodes.Dup);
                getAllCollectionsBuilder.Emit(OpCodes.Ldc_I4, index);
                getAllCollectionsBuilder.Emit(OpCodes.Ldarg_0);
                getAllCollectionsBuilder.Emit(OpCodes.Ldfld, cacheField);
                getAllCollectionsBuilder.Emit(OpCodes.Stelem, typeof(IKeyedCache));
                index++;
            }
            getAllCollectionsBuilder.Emit(OpCodes.Ret);

            expireTypeBuilder.Type.CreateTypeInfo();
            purgeTypeBuilder.Type.CreateTypeInfo();
            insertTypeBuilder.Type.CreateTypeInfo();
            getCollectionTypeBuilder.Type.CreateTypeInfo();
            cacheTypeBuilder.Type.CreateTypeInfo();
            _finishedType = _cacheType.CreateTypeInfo();
        }

        public FieldBuilder SourceField
        {
            get
            {
                if (_sourceField == null)
                {
                    _sourceField = _cacheType.DefineField("_source", _sourceType, FieldAttributes.Private | FieldAttributes.InitOnly);
                }

                return _sourceField;
            }
        }

        private void BeginCtor()
        {
            var ctor = _cacheType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { _sourceType });
            _ctorGenerator = ctor.GetILGenerator();
            _ctorGenerator.Emit(OpCodes.Ldarg_0);
            _ctorGenerator.Emit(OpCodes.Ldarg_1);
            _ctorGenerator.Emit(OpCodes.Stfld, SourceField);
        }

        private void CloseCtor()
        {
            _ctorGenerator.Emit(OpCodes.Ret);
        }

        private CacheKeyTypeBuilder GenerateCacheKeyType(MethodInfo method)
        {
            return new CacheKeyTypeBuilder(this, method);
        }
    }
}
