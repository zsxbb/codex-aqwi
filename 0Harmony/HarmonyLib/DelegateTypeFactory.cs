using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	public class DelegateTypeFactory
	{
		public DelegateTypeFactory()
		{
			DelegateTypeFactory.counter++;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
			defaultInterpolatedStringHandler.AppendLiteral("HarmonyDTFAssembly");
			defaultInterpolatedStringHandler.AppendFormatted<int>(DelegateTypeFactory.counter);
			string name = defaultInterpolatedStringHandler.ToStringAndClear();
			AssemblyBuilder assemblyBuilder = PatchTools.DefineDynamicAssembly(name);
			AssemblyBuilder assemblyBuilder2 = assemblyBuilder;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(16, 1);
			defaultInterpolatedStringHandler2.AppendLiteral("HarmonyDTFModule");
			defaultInterpolatedStringHandler2.AppendFormatted<int>(DelegateTypeFactory.counter);
			this.module = assemblyBuilder2.DefineDynamicModule(defaultInterpolatedStringHandler2.ToStringAndClear());
		}

		public Type CreateDelegateType(MethodInfo method)
		{
			TypeAttributes attr = TypeAttributes.Public | TypeAttributes.Sealed;
			ModuleBuilder moduleBuilder = this.module;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
			defaultInterpolatedStringHandler.AppendLiteral("HarmonyDTFType");
			defaultInterpolatedStringHandler.AppendFormatted<int>(DelegateTypeFactory.counter);
			TypeBuilder typeBuilder = moduleBuilder.DefineType(defaultInterpolatedStringHandler.ToStringAndClear(), attr, typeof(MulticastDelegate));
			ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, CallingConventions.Standard, new Type[]
			{
				typeof(object),
				typeof(IntPtr)
			});
			constructorBuilder.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
			ParameterInfo[] parameters = method.GetParameters();
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig, method.ReturnType, parameters.Types());
			methodBuilder.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
			for (int i = 0; i < parameters.Length; i++)
			{
				methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);
			}
			return typeBuilder.CreateType();
		}

		private readonly ModuleBuilder module;

		private static int counter;
	}
}
