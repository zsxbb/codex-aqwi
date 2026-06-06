using System;

namespace Mono.Cecil.Rocks
{
	internal static class MethodDefinitionRocks
	{
		public static MethodDefinition GetBaseMethod(this MethodDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			if (!self.IsVirtual)
			{
				return self;
			}
			if (self.IsNewSlot)
			{
				return self;
			}
			for (TypeDefinition type = MethodDefinitionRocks.ResolveBaseType(self.DeclaringType); type != null; type = MethodDefinitionRocks.ResolveBaseType(type))
			{
				MethodDefinition matchingMethod = MethodDefinitionRocks.GetMatchingMethod(type, self);
				if (matchingMethod != null)
				{
					return matchingMethod;
				}
			}
			return self;
		}

		public static MethodDefinition GetOriginalBaseMethod(this MethodDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			for (;;)
			{
				MethodDefinition baseMethod = self.GetBaseMethod();
				if (baseMethod == self)
				{
					break;
				}
				self = baseMethod;
			}
			return self;
		}

		private static TypeDefinition ResolveBaseType(TypeDefinition type)
		{
			if (type == null)
			{
				return null;
			}
			TypeReference baseType = type.BaseType;
			if (baseType == null)
			{
				return null;
			}
			return baseType.Resolve();
		}

		private static MethodDefinition GetMatchingMethod(TypeDefinition type, MethodDefinition method)
		{
			return MetadataResolver.GetMethod(type.Methods, method);
		}
	}
}
