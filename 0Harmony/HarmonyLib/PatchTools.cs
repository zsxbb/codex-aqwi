using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using MonoMod.Core;
using MonoMod.Utils;

namespace HarmonyLib
{
	internal static class PatchTools
	{
		internal static void DetourMethod(MethodBase method, MethodBase replacement)
		{
			Dictionary<MethodBase, ICoreDetour> obj = PatchTools.detours;
			lock (obj)
			{
				ICoreDetour coreDetour;
				if (PatchTools.detours.TryGetValue(method, out coreDetour))
				{
					coreDetour.Dispose();
				}
				PatchTools.detours[method] = DetourFactory.Current.CreateDetour(method, replacement, true);
			}
		}

		private static Assembly GetExecutingAssemblyReplacement()
		{
			StackFrame[] frames = new StackTrace().GetFrames();
			StackFrame stackFrame = (frames != null) ? frames.Skip(1).FirstOrDefault<StackFrame>() : null;
			if (stackFrame != null)
			{
				MethodBase methodFromStackframe = Harmony.GetMethodFromStackframe(stackFrame);
				if (methodFromStackframe != null)
				{
					return methodFromStackframe.Module.Assembly;
				}
			}
			return Assembly.GetExecutingAssembly();
		}

		internal static IEnumerable<CodeInstruction> GetExecutingAssemblyTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions.MethodReplacer(PatchTools.m_GetExecutingAssembly, PatchTools.m_GetExecutingAssemblyReplacement);
		}

		public static MethodInfo CreateMethod(string name, Type returnType, List<KeyValuePair<string, Type>> parameters, Action<ILGenerator> generator)
		{
			Type[] parameterTypes = (from p in parameters
			select p.Value).ToArray<Type>();
			if (AccessTools.IsMonoRuntime && !Tools.isWindows)
			{
				AssemblyName name2 = new AssemblyName("TempAssembly");
				AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name2, AssemblyBuilderAccess.Run);
				ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("TempModule");
				TypeBuilder typeBuilder = moduleBuilder.DefineType("TempType", TypeAttributes.Public);
				MethodBuilder methodBuilder = typeBuilder.DefineMethod(name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static, returnType, parameterTypes);
				for (int i = 0; i < parameters.Count; i++)
				{
					methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Key);
				}
				generator(methodBuilder.GetILGenerator());
				Type type = typeBuilder.CreateType();
				return type.GetMethod(name, BindingFlags.Static | BindingFlags.Public);
			}
			DynamicMethodDefinition dynamicMethodDefinition = new DynamicMethodDefinition(name, returnType, parameterTypes);
			for (int j = 0; j < parameters.Count; j++)
			{
				dynamicMethodDefinition.Definition.Parameters[j].Name = parameters[j].Key;
			}
			generator(dynamicMethodDefinition.GetILGenerator());
			return dynamicMethodDefinition.Generate();
		}

		internal static MethodInfo GetPatchMethod(Type patchType, string attributeName)
		{
			Func<object, bool> <>9__1;
			MethodInfo methodInfo = patchType.GetMethods(AccessTools.all).FirstOrDefault(delegate(MethodInfo m)
			{
				IEnumerable<object> customAttributes = m.GetCustomAttributes(true);
				Func<object, bool> predicate;
				if ((predicate = <>9__1) == null)
				{
					predicate = (<>9__1 = ((object a) => a.GetType().FullName == attributeName));
				}
				return customAttributes.Any(predicate);
			});
			if (methodInfo == null)
			{
				string name = attributeName.Replace("HarmonyLib.Harmony", "");
				methodInfo = patchType.GetMethod(name, AccessTools.all);
			}
			return methodInfo;
		}

		internal static AssemblyBuilder DefineDynamicAssembly(string name)
		{
			AssemblyName name2 = new AssemblyName(name);
			return AppDomain.CurrentDomain.DefineDynamicAssembly(name2, AssemblyBuilderAccess.Run);
		}

		internal static List<AttributePatch> GetPatchMethods(Type type)
		{
			IEnumerable<MethodInfo> declaredMethods = AccessTools.GetDeclaredMethods(type);
			Func<MethodInfo, AttributePatch> selector;
			if ((selector = PatchTools.<>O.<0>__Create) == null)
			{
				selector = (PatchTools.<>O.<0>__Create = new Func<MethodInfo, AttributePatch>(AttributePatch.Create));
			}
			return (from attributePatch in declaredMethods.Select(selector)
			where attributePatch != null
			select attributePatch).ToList<AttributePatch>();
		}

		internal static MethodBase GetOriginalMethod(this HarmonyMethod attr)
		{
			try
			{
				MethodType? methodType = attr.methodType;
				if (methodType != null)
				{
					switch (methodType.GetValueOrDefault())
					{
					case MethodType.Normal:
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return null;
						}
						return AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes, null);
					case MethodType.Getter:
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return AccessTools.DeclaredIndexerGetter(attr.declaringType, attr.argumentTypes);
						}
						return AccessTools.DeclaredPropertyGetter(attr.declaringType, attr.methodName);
					case MethodType.Setter:
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return AccessTools.DeclaredIndexerSetter(attr.declaringType, attr.argumentTypes);
						}
						return AccessTools.DeclaredPropertySetter(attr.declaringType, attr.methodName);
					case MethodType.Constructor:
						return AccessTools.DeclaredConstructor(attr.declaringType, attr.argumentTypes, false);
					case MethodType.StaticConstructor:
						return (from c in AccessTools.GetDeclaredConstructors(attr.declaringType, null)
						where c.IsStatic
						select c).FirstOrDefault<ConstructorInfo>();
					case MethodType.Enumerator:
					{
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return null;
						}
						MethodInfo method = AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes, null);
						return AccessTools.EnumeratorMoveNext(method);
					}
					case MethodType.Async:
					{
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return null;
						}
						MethodInfo method2 = AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes, null);
						return AccessTools.AsyncMoveNext(method2);
					}
					case MethodType.Finalizer:
						return AccessTools.DeclaredFinalizer(attr.declaringType);
					case MethodType.EventAdd:
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return null;
						}
						return AccessTools.DeclaredEventAdder(attr.declaringType, attr.methodName);
					case MethodType.EventRemove:
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return null;
						}
						return AccessTools.DeclaredEventRemover(attr.declaringType, attr.methodName);
					case MethodType.OperatorImplicit:
					case MethodType.OperatorExplicit:
					case MethodType.OperatorUnaryPlus:
					case MethodType.OperatorUnaryNegation:
					case MethodType.OperatorLogicalNot:
					case MethodType.OperatorOnesComplement:
					case MethodType.OperatorIncrement:
					case MethodType.OperatorDecrement:
					case MethodType.OperatorTrue:
					case MethodType.OperatorFalse:
					case MethodType.OperatorAddition:
					case MethodType.OperatorSubtraction:
					case MethodType.OperatorMultiply:
					case MethodType.OperatorDivision:
					case MethodType.OperatorModulus:
					case MethodType.OperatorBitwiseAnd:
					case MethodType.OperatorBitwiseOr:
					case MethodType.OperatorExclusiveOr:
					case MethodType.OperatorLeftShift:
					case MethodType.OperatorRightShift:
					case MethodType.OperatorEquality:
					case MethodType.OperatorInequality:
					case MethodType.OperatorGreaterThan:
					case MethodType.OperatorLessThan:
					case MethodType.OperatorGreaterThanOrEqual:
					case MethodType.OperatorLessThanOrEqual:
					case MethodType.OperatorComma:
					{
						string name = "op_" + attr.methodType.ToString().Replace("Operator", "");
						return AccessTools.DeclaredMethod(attr.declaringType, name, attr.argumentTypes, null);
					}
					}
				}
			}
			catch (AmbiguousMatchException ex)
			{
				throw new HarmonyException("Ambiguous match for HarmonyMethod[" + attr.Description() + "]", ex.InnerException ?? ex);
			}
			return null;
		}

		private static readonly Dictionary<MethodBase, ICoreDetour> detours = new Dictionary<MethodBase, ICoreDetour>();

		internal static readonly string harmonyMethodFullName = typeof(HarmonyMethod).FullName;

		internal static readonly string harmonyAttributeFullName = typeof(HarmonyAttribute).FullName;

		internal static readonly string harmonyPatchAllFullName = typeof(HarmonyPatchAll).FullName;

		internal static readonly MethodInfo m_GetExecutingAssemblyReplacementTranspiler = SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(PatchTools.GetExecutingAssemblyTranspiler(IEnumerable<CodeInstruction>)), new Expression[]
		{
			Expression.Constant(null, typeof(IEnumerable<CodeInstruction>))
		}), Array.Empty<ParameterExpression>()));

		internal static readonly MethodInfo m_GetExecutingAssembly = SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(Assembly.GetExecutingAssembly()), Array.Empty<Expression>()), Array.Empty<ParameterExpression>()));

		internal static readonly MethodInfo m_GetExecutingAssemblyReplacement = SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(PatchTools.GetExecutingAssemblyReplacement()), Array.Empty<Expression>()), Array.Empty<ParameterExpression>()));

		[CompilerGenerated]
		private static class <>O
		{
			public static Func<MethodInfo, AttributePatch> <0>__Create;
		}
	}
}
