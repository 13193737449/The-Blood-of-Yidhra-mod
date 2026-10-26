using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;

// The obsolete AssemblyName overload is intentional: the recommended Assembly overload triggers the Mono encoding bug.
#pragma warning disable 618
namespace YidhraTrainer
{
    // Unity Mono can fail in Assembly.GetName() when CodeBase contains a non-UTF8 path.
    // Retain MonoMod's importer and runtime hashes; populate its existing cache using FullName.
    public static class UnicodePathCompatibility
    {
        private static readonly FieldInfo CacheField = typeof(MMReflectionImporter).GetField("CachedAsms", BindingFlags.NonPublic | BindingFlags.Instance);
        private static NativeDetour factoryDetour;

        public static void Install()
        {
            if (factoryDetour != null) return;
            if (CacheField == null) throw new NotSupportedException("Unsupported MonoMod reflection importer version.");
            // Mono folds static readonly provider references into already compiled DMD methods.
            // Hook the provider factory itself through MonoMod, so preloader warmup is supported.
            MethodInfo source = MMReflectionImporter.ProviderNoDefault.GetType().GetMethod("GetReflectionImporter");
            MethodInfo target = typeof(UnicodePathCompatibility).GetMethod("CreateImporter", BindingFlags.NonPublic | BindingFlags.Static);
            if (source == null) throw new MissingMethodException("MonoMod reflection importer factory");
            factoryDetour = new NativeDetour(source, target);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IReflectionImporter CreateImporter(object provider, ModuleDefinition module)
        {
            return new SafeImporter(module);
        }

        public static void Restore()
        {
            if (factoryDetour == null) return;
            factoryDetour.Dispose();
            factoryDetour = null;
        }

        private static void CacheAssembly(MMReflectionImporter importer, Assembly assembly)
        {
            var cache = (Dictionary<Assembly, AssemblyNameReference>)CacheField.GetValue(importer);
            if (cache.ContainsKey(assembly)) return;
            var reference = importer.ImportReference(new AssemblyName(assembly.FullName));
            ReflectionHelper.ApplyRuntimeHash(reference, assembly);
            cache.Add(assembly, reference);
        }

        private sealed class SafeImporter : IReflectionImporter
        {
            private readonly MMReflectionImporter inner;

            public SafeImporter(ModuleDefinition module)
            {
                inner = new MMReflectionImporter(module) { UseDefault = false };
                RefreshCache();
            }

            private void RefreshCache()
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    CacheAssembly(inner, assembly);
            }

            public AssemblyNameReference ImportReference(AssemblyName reference)
            {
                return inner.ImportReference(reference);
            }

            public TypeReference ImportReference(Type type, IGenericParameterProvider context)
            {
                RefreshCache();
                return inner.ImportReference(type, context);
            }

            public FieldReference ImportReference(FieldInfo field, IGenericParameterProvider context)
            {
                RefreshCache();
                return inner.ImportReference(field, context);
            }

            public MethodReference ImportReference(MethodBase method, IGenericParameterProvider context)
            {
                RefreshCache();
                return inner.ImportReference(method, context);
            }
        }
    }
}



