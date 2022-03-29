// This temporary utility fixes an esoteric issue in XNA Framework where deserialization depends on
// the order of fields returned by Type.GetFields, but that order changes after Harmony/MonoMod use
// reflection to access the fields due to an issue in .NET Framework.
// https://twitter.com/0x0ade/status/1414992316964687873
//
// This will be removed when Harmony/MonoMod are updated to incorporate the fix.
//
// Special thanks to 0x0ade for submitting this workaround! Copy/pasted and adapted from MonoMod.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

// ReSharper disable once CheckNamespace -- Temporary hotfix submitted by the MonoMod author.
namespace MonoMod.Utils
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Temporary hotfix submitted by the MonoMod author.")]
    [SuppressMessage("ReSharper", "PossibleNullReferenceException", Justification = "Temporary hotfix submitted by the MonoMod author.")]
    static class MiniMonoModHotfix
    {
        // .NET Framework can break member ordering if using Module.Resolve* on certain members.

        private static readonly object[] _NoArgs = Array.Empty<object>();
        private static readonly object[] _CacheGetterArgs = { /* MemberListType.All */ 0, /* name apparently always null? */ null };

        private static readonly Type t_RuntimeType =
            typeof(Type).Assembly
            .GetType("System.RuntimeType");

        private static readonly PropertyInfo p_RuntimeType_Cache =
            typeof(Type).Assembly
            .GetType("System.RuntimeType")
            ?.GetProperty("Cache", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo m_RuntimeTypeCache_GetFieldList =
            typeof(Type).Assembly
            .GetType("System.RuntimeType+RuntimeTypeCache")
            ?.GetMethod("GetFieldList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo m_RuntimeTypeCache_GetPropertyList =
            typeof(Type).Assembly
            .GetType("System.RuntimeType+RuntimeTypeCache")
            ?.GetMethod("GetPropertyList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly ConditionalWeakTable<Type, CacheFixEntry> _CacheFixed = new ConditionalWeakTable<Type, CacheFixEntry>();

        public static void Apply()
        {
            var harmony = new Harmony("MiniMonoModHotfix");

            harmony.Patch(
                original: typeof(Harmony).Assembly
                    .GetType("HarmonyLib.MethodBodyReader")
                    .GetMethod("ReadOperand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                transpiler: new HarmonyMethod(typeof(MiniMonoModHotfix), nameof(ResolveTokenFix))
            );

            harmony.Patch(
                original: typeof(MonoMod.Utils.ReflectionHelper).Assembly
                    .GetType("MonoMod.Utils.DynamicMethodDefinition+<>c__DisplayClass3_0")
                    .GetMethod("<_CopyMethodToDefinition>g__ResolveTokenAs|1", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                transpiler: new HarmonyMethod(typeof(MiniMonoModHotfix), nameof(ResolveTokenFix))
            );

        }

        private static IEnumerable<CodeInstruction> ResolveTokenFix(IEnumerable<CodeInstruction> instrs)
        {
            MethodInfo getdecl = typeof(MiniMonoModHotfix).GetMethod(nameof(GetRealDeclaringType));
            MethodInfo fixup = typeof(MiniMonoModHotfix).GetMethod(nameof(FixReflectionCache));

            foreach (CodeInstruction instr in instrs)
            {
                yield return instr;

                if (instr.operand is MethodInfo called)
                {
                    switch (called.Name)
                    {
                        case "ResolveType":
                            // type.FixReflectionCache();
                            yield return new CodeInstruction(OpCodes.Dup);
                            yield return new CodeInstruction(OpCodes.Call, fixup);
                            break;

                        case "ResolveMember":
                        case "ResolveMethod":
                        case "ResolveField":
                            // member.GetRealDeclaringType().FixReflectionCache();
                            yield return new CodeInstruction(OpCodes.Dup);
                            yield return new CodeInstruction(OpCodes.Call, getdecl);
                            yield return new CodeInstruction(OpCodes.Call, fixup);
                            break;
                    }
                }
            }
        }

        public static Type GetRealDeclaringType(this MemberInfo member)
        {
            return member.DeclaringType ?? member.Module?.GetModuleType();
        }

        public static void FixReflectionCache(this Type type)
        {
            if (t_RuntimeType == null ||
                p_RuntimeType_Cache == null ||
                m_RuntimeTypeCache_GetFieldList == null ||
                m_RuntimeTypeCache_GetPropertyList == null)
                return;

            for (; type != null; type = type.DeclaringType)
            {
                // All types SHOULD inherit RuntimeType, including those built at runtime.
                // One might never know what awaits us in the depths of reflection hell though.
                if (!t_RuntimeType.IsInstanceOfType(type))
                    continue;

                CacheFixEntry entry = _CacheFixed.GetValue(type, rt =>
                {
                    CacheFixEntry entryNew = new CacheFixEntry();
                    object cache;
                    Array properties, fields;

                    // All RuntimeTypes MUST have a cache, the getter is non-virtual, it creates on demand and asserts non-null.
                    entryNew.Cache = cache = p_RuntimeType_Cache.GetValue(rt, _NoArgs);
                    entryNew.Properties = properties = _GetArray(cache, m_RuntimeTypeCache_GetPropertyList);
                    entryNew.Fields = fields = _GetArray(cache, m_RuntimeTypeCache_GetFieldList);

                    _FixReflectionCacheOrder<PropertyInfo>(properties);
                    _FixReflectionCacheOrder<FieldInfo>(fields);

                    entryNew.NeedsVerify = false;
                    return entryNew;
                });

                if (entry.NeedsVerify && !_Verify(entry, type))
                {
                    lock (entry)
                    {
                        _FixReflectionCacheOrder<PropertyInfo>(entry.Properties);
                        _FixReflectionCacheOrder<FieldInfo>(entry.Fields);
                    }
                }

                entry.NeedsVerify = true;
            }
        }

        private static bool _Verify(CacheFixEntry entry, Type type)
        {
            object cache;
            Array properties, fields;

            // The cache can sometimes be invalidated.
            // TODO: Figure out if only the arrays get replaced or if the entire cache object gets replaced!
            if (entry.Cache != (cache = p_RuntimeType_Cache.GetValue(type, _NoArgs)))
            {
                entry.Cache = cache;
                entry.Properties = _GetArray(cache, m_RuntimeTypeCache_GetPropertyList);
                entry.Fields = _GetArray(cache, m_RuntimeTypeCache_GetFieldList);
                return false;

            }
            else if (entry.Properties != (properties = _GetArray(cache, m_RuntimeTypeCache_GetPropertyList)))
            {
                entry.Properties = properties;
                entry.Fields = _GetArray(cache, m_RuntimeTypeCache_GetFieldList);
                return false;

            }
            else if (entry.Fields != (fields = _GetArray(cache, m_RuntimeTypeCache_GetFieldList)))
            {
                entry.Fields = fields;
                return false;

            }
            else
            {
                // Cache should still be the same, no re-fix necessary.
                return true;
            }
        }

        private static Array _GetArray(object cache, MethodInfo getter)
        {
            // Get and discard once, otherwise we might not be getting the actual backing array.
            getter.Invoke(cache, _CacheGetterArgs);
            return (Array)getter.Invoke(cache, _CacheGetterArgs);
        }

        private static void _FixReflectionCacheOrder<T>(Array orig) where T : MemberInfo
        {
            // Sort using a short-lived list.
            List<T> list = new List<T>(orig.Length);
            for (int i = 0; i < orig.Length; i++)
                list.Add((T)orig.GetValue(i));

            list.Sort((a, b) => a.MetadataToken - b.MetadataToken);

            for (int i = orig.Length - 1; i >= 0; --i)
                orig.SetValue(list[i], i);
        }

        private class CacheFixEntry
        {
            public object Cache;
            public Array Properties;
            public Array Fields;
            public bool NeedsVerify;
        }
    }
}
