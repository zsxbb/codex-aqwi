using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod;

namespace HarmonyLib
{
	public class Harmony
	{
		public string Id { get; private set; }

		public Harmony(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				throw new ArgumentException("id cannot be null or empty");
			}
			try
			{
				string text = Environment.GetEnvironmentVariable("HARMONY_DEBUG");
				if (text != null && text.Length > 0)
				{
					text = text.Trim();
					Harmony.DEBUG = (text == "1" || bool.Parse(text));
				}
			}
			catch
			{
			}
			if (Harmony.DEBUG)
			{
				Assembly assembly = typeof(Harmony).Assembly;
				Version version = assembly.GetName().Version;
				string text2 = assembly.Location;
				string value = Environment.Version.ToString();
				string value2 = Environment.OSVersion.Platform.ToString();
				if (string.IsNullOrEmpty(text2))
				{
					text2 = new Uri(assembly.CodeBase).LocalPath;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 5);
				defaultInterpolatedStringHandler.AppendLiteral("### Harmony id=");
				defaultInterpolatedStringHandler.AppendFormatted(id);
				defaultInterpolatedStringHandler.AppendLiteral(", version=");
				defaultInterpolatedStringHandler.AppendFormatted<Version>(version);
				defaultInterpolatedStringHandler.AppendLiteral(", location=");
				defaultInterpolatedStringHandler.AppendFormatted(text2);
				defaultInterpolatedStringHandler.AppendLiteral(", env/clr=");
				defaultInterpolatedStringHandler.AppendFormatted(value);
				defaultInterpolatedStringHandler.AppendLiteral(", platform=");
				defaultInterpolatedStringHandler.AppendFormatted(value2);
				FileLog.Log(defaultInterpolatedStringHandler.ToStringAndClear());
				MethodBase outsideCaller = AccessTools.GetOutsideCaller();
				if (outsideCaller.DeclaringType != null)
				{
					Assembly assembly2 = outsideCaller.DeclaringType.Assembly;
					text2 = assembly2.Location;
					if (string.IsNullOrEmpty(text2))
					{
						text2 = new Uri(assembly2.CodeBase).LocalPath;
					}
					FileLog.Log("### Started from " + outsideCaller.FullDescription() + ", location " + text2);
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(7, 1);
					defaultInterpolatedStringHandler2.AppendLiteral("### At ");
					defaultInterpolatedStringHandler2.AppendFormatted<DateTime>(DateTime.Now, "yyyy-MM-dd hh.mm.ss");
					FileLog.Log(defaultInterpolatedStringHandler2.ToStringAndClear());
				}
			}
			this.Id = id;
		}

		public void PatchAll()
		{
			MethodBase method = new StackTrace().GetFrame(1).GetMethod();
			Assembly assembly = method.ReflectedType.Assembly;
			this.PatchAll(assembly);
		}

		public PatchProcessor CreateProcessor(MethodBase original)
		{
			return new PatchProcessor(this, original);
		}

		public PatchClassProcessor CreateClassProcessor(Type type)
		{
			return new PatchClassProcessor(this, type);
		}

		public ReversePatcher CreateReversePatcher(MethodBase original, HarmonyMethod standin)
		{
			return new ReversePatcher(this, original, standin);
		}

		public void PatchAll(Assembly assembly)
		{
			AccessTools.GetTypesFromAssembly(assembly).DoIf((Type type) => type.HasHarmonyAttribute(), delegate(Type type)
			{
				this.CreateClassProcessor(type).Patch();
			});
		}

		public void PatchAllUncategorized()
		{
			MethodBase method = new StackTrace().GetFrame(1).GetMethod();
			Assembly assembly = method.ReflectedType.Assembly;
			this.PatchAllUncategorized(assembly);
		}

		public void PatchAllUncategorized(Assembly assembly)
		{
			PatchClassProcessor[] sequence = (from type in AccessTools.GetTypesFromAssembly(assembly)
			where type.HasHarmonyAttribute()
			select type).Select(new Func<Type, PatchClassProcessor>(this.CreateClassProcessor)).ToArray<PatchClassProcessor>();
			sequence.DoIf((PatchClassProcessor patchClass) => string.IsNullOrEmpty(patchClass.Category), delegate(PatchClassProcessor patchClass)
			{
				patchClass.Patch();
			});
		}

		public void PatchCategory(string category)
		{
			MethodBase method = new StackTrace().GetFrame(1).GetMethod();
			Assembly assembly = method.ReflectedType.Assembly;
			this.PatchCategory(assembly, category);
		}

		public void PatchCategory(Assembly assembly, string category)
		{
			ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>> assemblyCachedCategories = Harmony.AssemblyCachedCategories;
			ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback createValueCallback;
			if ((createValueCallback = Harmony.<>O.<0>__BuildCategoryCache) == null)
			{
				createValueCallback = (Harmony.<>O.<0>__BuildCategoryCache = new ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback(Harmony.BuildCategoryCache));
			}
			Dictionary<string, List<Type>> value = assemblyCachedCategories.GetValue(assembly, createValueCallback);
			List<Type> sequence;
			if (value.TryGetValue(category, out sequence))
			{
				sequence.Do(delegate(Type type)
				{
					this.CreateClassProcessor(type).Patch();
				});
			}
		}

		private static Dictionary<string, List<Type>> BuildCategoryCache(Assembly assembly)
		{
			Dictionary<string, List<Type>> dictionary = new Dictionary<string, List<Type>>();
			foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
			{
				List<HarmonyMethod> fromType = HarmonyMethodExtensions.GetFromType(type);
				if (fromType.Count != 0)
				{
					HarmonyMethod harmonyMethod = HarmonyMethod.Merge(fromType);
					string category = harmonyMethod.category;
					if (!string.IsNullOrEmpty(category))
					{
						List<Type> list;
						if (!dictionary.TryGetValue(category, out list) && list == null)
						{
							list = new List<Type>();
						}
						list.Add(type);
						dictionary[category] = list;
					}
				}
			}
			return dictionary;
		}

		public MethodInfo Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
		{
			PatchProcessor patchProcessor = this.CreateProcessor(original);
			patchProcessor.AddPrefix(prefix);
			patchProcessor.AddPostfix(postfix);
			patchProcessor.AddTranspiler(transpiler);
			patchProcessor.AddFinalizer(finalizer);
			return patchProcessor.Patch();
		}

		public static MethodInfo ReversePatch(MethodBase original, HarmonyMethod standin, MethodInfo transpiler = null)
		{
			return PatchFunctions.ReversePatch(standin, original, transpiler);
		}

		public void UnpatchAll(string harmonyID = null)
		{
			Harmony.<>c__DisplayClass19_0 CS$<>8__locals1 = new Harmony.<>c__DisplayClass19_0();
			CS$<>8__locals1.harmonyID = harmonyID;
			CS$<>8__locals1.<>4__this = this;
			List<MethodBase> list = Harmony.GetAllPatchedMethods().ToList<MethodBase>();
			using (List<MethodBase>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					MethodBase original = enumerator.Current;
					bool flag = original.HasMethodBody();
					Patches patchInfo2 = Harmony.GetPatchInfo(original);
					if (flag)
					{
						patchInfo2.Postfixes.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
						patchInfo2.Prefixes.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
						patchInfo2.InnerPostfixes.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
						patchInfo2.InnerPrefixes.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
					}
					patchInfo2.Transpilers.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
					{
						CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
					});
					if (flag)
					{
						patchInfo2.Finalizers.DoIf(new Func<Patch, bool>(CS$<>8__locals1.<UnpatchAll>g__IDCheck|0), delegate(Patch patchInfo)
						{
							CS$<>8__locals1.<>4__this.Unpatch(original, patchInfo.PatchMethod);
						});
					}
				}
			}
		}

		public void Unpatch(MethodBase original, HarmonyPatchType type, string harmonyID = "*")
		{
			PatchProcessor patchProcessor = this.CreateProcessor(original);
			patchProcessor.Unpatch(type, harmonyID);
		}

		public void Unpatch(MethodBase original, MethodInfo patch)
		{
			PatchProcessor patchProcessor = this.CreateProcessor(original);
			patchProcessor.Unpatch(patch);
		}

		public void UnpatchCategory(string category)
		{
			MethodBase method = new StackTrace().GetFrame(1).GetMethod();
			Assembly assembly = method.ReflectedType.Assembly;
			this.UnpatchCategory(assembly, category);
		}

		public void UnpatchCategory(Assembly assembly, string category)
		{
			ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>> assemblyCachedCategories = Harmony.AssemblyCachedCategories;
			ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback createValueCallback;
			if ((createValueCallback = Harmony.<>O.<0>__BuildCategoryCache) == null)
			{
				createValueCallback = (Harmony.<>O.<0>__BuildCategoryCache = new ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback(Harmony.BuildCategoryCache));
			}
			Dictionary<string, List<Type>> value = assemblyCachedCategories.GetValue(assembly, createValueCallback);
			List<Type> sequence;
			if (value.TryGetValue(category, out sequence))
			{
				sequence.Do(delegate(Type type)
				{
					this.CreateClassProcessor(type).Unpatch();
				});
			}
		}

		public static bool HasAnyPatches(string harmonyID)
		{
			IEnumerable<MethodBase> allPatchedMethods = Harmony.GetAllPatchedMethods();
			Func<MethodBase, Patches> selector;
			if ((selector = Harmony.<>O.<1>__GetPatchInfo) == null)
			{
				selector = (Harmony.<>O.<1>__GetPatchInfo = new Func<MethodBase, Patches>(Harmony.GetPatchInfo));
			}
			return allPatchedMethods.Select(selector).Any((Patches info) => info.Owners.Contains(harmonyID));
		}

		public static Patches GetPatchInfo(MethodBase method)
		{
			return PatchProcessor.GetPatchInfo(method);
		}

		public IEnumerable<MethodBase> GetPatchedMethods()
		{
			return from original in Harmony.GetAllPatchedMethods()
			where Harmony.GetPatchInfo(original).Owners.Contains(this.Id)
			select original;
		}

		public static IEnumerable<MethodBase> GetAllPatchedMethods()
		{
			return PatchProcessor.GetAllPatchedMethods();
		}

		public static MethodBase GetOriginalMethod(MethodInfo replacement)
		{
			if (replacement == null)
			{
				throw new ArgumentNullException("replacement");
			}
			return HarmonySharedState.GetRealMethod(replacement, false);
		}

		public static MethodBase GetMethodFromStackframe(StackFrame frame)
		{
			if (frame == null)
			{
				throw new ArgumentNullException("frame");
			}
			return HarmonySharedState.GetStackFrameMethod(frame, true);
		}

		public static MethodBase GetOriginalMethodFromStackframe(StackFrame frame)
		{
			if (frame == null)
			{
				throw new ArgumentNullException("frame");
			}
			return HarmonySharedState.GetStackFrameMethod(frame, false);
		}

		public static Dictionary<string, Version> VersionInfo(out Version currentVersion)
		{
			return PatchProcessor.VersionInfo(out currentVersion);
		}

		public static void SetSwitch(string name, object value)
		{
			Switches.SetSwitchValue(name, value);
		}

		public static void ClearSwitch(string name)
		{
			Switches.ClearSwitchValue(name);
		}

		public static bool TryGetSwitch(string name, out object value)
		{
			return Switches.TryGetSwitchValue(name, out value);
		}

		public static bool TryIsSwitchEnabled(string name, out bool isEnabled)
		{
			return Switches.TryGetSwitchEnabled(name, out isEnabled);
		}

		public static bool DEBUG;

		private static readonly ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>> AssemblyCachedCategories = new ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>();

		[CompilerGenerated]
		private static class <>O
		{
			public static ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>.CreateValueCallback <0>__BuildCategoryCache;

			public static Func<MethodBase, Patches> <1>__GetPatchInfo;
		}
	}
}
