using System;
using System.Runtime.CompilerServices;

namespace System
{
	internal static class StringComparerEx
	{
		[NullableContext(1)]
		public static StringComparer FromComparison(StringComparison comparisonType)
		{
			StringComparer result;
			switch (comparisonType)
			{
			case StringComparison.CurrentCulture:
				result = StringComparer.CurrentCulture;
				break;
			case StringComparison.CurrentCultureIgnoreCase:
				result = StringComparer.CurrentCultureIgnoreCase;
				break;
			case StringComparison.InvariantCulture:
				result = StringComparer.InvariantCulture;
				break;
			case StringComparison.InvariantCultureIgnoreCase:
				result = StringComparer.InvariantCultureIgnoreCase;
				break;
			case StringComparison.Ordinal:
				result = StringComparer.Ordinal;
				break;
			case StringComparison.OrdinalIgnoreCase:
				result = StringComparer.OrdinalIgnoreCase;
				break;
			default:
				throw new ArgumentException("Invalid StringComparison value", "comparisonType");
			}
			return result;
		}
	}
}
