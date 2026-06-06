using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace HarmonyLib
{
	[Serializable]
	public class PatchInfo
	{
		public bool Debugging
		{
			get
			{
				if (!this.prefixes.Any((Patch p) => p.debug))
				{
					if (!this.postfixes.Any((Patch p) => p.debug))
					{
						if (!this.transpilers.Any((Patch p) => p.debug))
						{
							if (!this.finalizers.Any((Patch p) => p.debug))
							{
								if (!this.innerprefixes.Any((Patch p) => p.debug))
								{
									return this.innerpostfixes.Any((Patch p) => p.debug);
								}
							}
						}
					}
				}
				return true;
			}
		}

		internal void AddPrefixes(string owner, params HarmonyMethod[] methods)
		{
			this.prefixes = PatchInfo.Add(owner, methods, this.prefixes);
		}

		[Obsolete("This method only exists for backwards compatibility since the class is public.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void AddPrefix(MethodInfo patch, string owner, int priority, string[] before, string[] after, bool debug)
		{
			this.AddPrefixes(owner, new HarmonyMethod[]
			{
				new HarmonyMethod(patch, priority, before, after, new bool?(debug))
			});
		}

		public void RemovePrefix(string owner)
		{
			this.prefixes = PatchInfo.Remove(owner, this.prefixes);
		}

		internal void AddPostfixes(string owner, params HarmonyMethod[] methods)
		{
			this.postfixes = PatchInfo.Add(owner, methods, this.postfixes);
		}

		[Obsolete("This method only exists for backwards compatibility since the class is public.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void AddPostfix(MethodInfo patch, string owner, int priority, string[] before, string[] after, bool debug)
		{
			this.AddPostfixes(owner, new HarmonyMethod[]
			{
				new HarmonyMethod(patch, priority, before, after, new bool?(debug))
			});
		}

		public void RemovePostfix(string owner)
		{
			this.postfixes = PatchInfo.Remove(owner, this.postfixes);
		}

		internal void AddTranspilers(string owner, params HarmonyMethod[] methods)
		{
			this.transpilers = PatchInfo.Add(owner, methods, this.transpilers);
		}

		[Obsolete("This method only exists for backwards compatibility since the class is public.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void AddTranspiler(MethodInfo patch, string owner, int priority, string[] before, string[] after, bool debug)
		{
			this.AddTranspilers(owner, new HarmonyMethod[]
			{
				new HarmonyMethod(patch, priority, before, after, new bool?(debug))
			});
		}

		public void RemoveTranspiler(string owner)
		{
			this.transpilers = PatchInfo.Remove(owner, this.transpilers);
		}

		internal void AddFinalizers(string owner, params HarmonyMethod[] methods)
		{
			this.finalizers = PatchInfo.Add(owner, methods, this.finalizers);
		}

		[Obsolete("This method only exists for backwards compatibility since the class is public.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void AddFinalizer(MethodInfo patch, string owner, int priority, string[] before, string[] after, bool debug)
		{
			this.AddFinalizers(owner, new HarmonyMethod[]
			{
				new HarmonyMethod(patch, priority, before, after, new bool?(debug))
			});
		}

		public void RemoveFinalizer(string owner)
		{
			this.finalizers = PatchInfo.Remove(owner, this.finalizers);
		}

		internal void AddInnerPrefixes(string owner, params HarmonyMethod[] methods)
		{
			this.innerprefixes = PatchInfo.Add(owner, methods, this.innerprefixes);
		}

		public void RemoveInnerPrefix(string owner)
		{
			this.innerprefixes = PatchInfo.Remove(owner, this.innerprefixes);
		}

		internal void AddInnerPostfixes(string owner, params HarmonyMethod[] methods)
		{
			this.innerpostfixes = PatchInfo.Add(owner, methods, this.innerpostfixes);
		}

		public void RemoveInnerPostfix(string owner)
		{
			this.innerpostfixes = PatchInfo.Remove(owner, this.innerpostfixes);
		}

		public void RemovePatch(MethodInfo patch)
		{
			this.prefixes = (from p in this.prefixes
			where p.PatchMethod != patch
			select p).ToArray<Patch>();
			this.postfixes = (from p in this.postfixes
			where p.PatchMethod != patch
			select p).ToArray<Patch>();
			this.transpilers = (from p in this.transpilers
			where p.PatchMethod != patch
			select p).ToArray<Patch>();
			this.finalizers = (from p in this.finalizers
			where p.PatchMethod != patch
			select p).ToArray<Patch>();
			this.innerprefixes = (from p in this.innerprefixes
			where p.PatchMethod != patch
			select p).ToArray<Patch>();
			this.innerpostfixes = (from p in this.innerpostfixes
			where p.PatchMethod != patch
			select p).ToArray<Patch>();
		}

		private static Patch[] Add(string owner, HarmonyMethod[] add, Patch[] current)
		{
			if (add.Length == 0)
			{
				return current;
			}
			int initialIndex = current.Length;
			List<Patch> list = new List<Patch>();
			list.AddRange(current);
			list.AddRange((from method in add
			where method != null
			select method).Select((HarmonyMethod method, int i) => new Patch(method, i + initialIndex, owner)));
			return list.ToArray();
		}

		private static Patch[] Remove(string owner, Patch[] current)
		{
			if (!(owner == "*"))
			{
				return (from patch in current
				where patch.owner != owner
				select patch).ToArray<Patch>();
			}
			return Array.Empty<Patch>();
		}

		public Patch[] prefixes = Array.Empty<Patch>();

		public Patch[] postfixes = Array.Empty<Patch>();

		public Patch[] transpilers = Array.Empty<Patch>();

		public Patch[] finalizers = Array.Empty<Patch>();

		public Patch[] innerprefixes = Array.Empty<Patch>();

		public Patch[] innerpostfixes = Array.Empty<Patch>();

		public int VersionCount;
	}
}
