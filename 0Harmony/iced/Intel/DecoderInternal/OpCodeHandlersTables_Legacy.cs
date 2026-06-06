using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	internal static class OpCodeHandlersTables_Legacy
	{
		static OpCodeHandlersTables_Legacy()
		{
			LegacyOpCodeHandlerReader handlerReader = new LegacyOpCodeHandlerReader();
			TableDeserializer tableDeserializer = new TableDeserializer(handlerReader, 82, OpCodeHandlersTables_Legacy.GetSerializedTables());
			tableDeserializer.Deserialize();
			OpCodeHandlersTables_Legacy.Handlers_MAP0 = tableDeserializer.GetTable(81U);
		}

		private unsafe static System.ReadOnlySpan<byte> GetSerializedTables()
		{
			return new System.ReadOnlySpan<byte>((void*)(&<b37590d4-39fb-478a-88de-d293f3364852><PrivateImplementationDetails>.626B29EAFFA7CEB2E50FEB451C5916CC67638E9FBCCC6EB9056377A2DEE5A09E), 6392);
		}

		[Nullable(1)]
		internal static readonly OpCodeHandler[] Handlers_MAP0;

		private const int MaxIdNames = 82;

		private const uint Handlers_MAP0Index = 81U;
	}
}
