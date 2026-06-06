using System;

namespace MonoMod.Logs
{
	internal interface IDebugFormattable
	{
		bool TryFormatInto(System.Span<char> span, out int wrote);
	}
}
