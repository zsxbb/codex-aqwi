using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Logs
{
	internal static class LogLevelExtensions
	{
		[NullableContext(1)]
		public static string FastToString(this LogLevel level, [Nullable(2)] IFormatProvider provider = null)
		{
			string result;
			switch (level)
			{
			case LogLevel.Spam:
				result = "Spam";
				break;
			case LogLevel.Trace:
				result = "Trace";
				break;
			case LogLevel.Info:
				result = "Info";
				break;
			case LogLevel.Warning:
				result = "Warning";
				break;
			case LogLevel.Error:
				result = "Error";
				break;
			case LogLevel.Assert:
				result = "Assert";
				break;
			default:
			{
				int num = (int)level;
				result = num.ToString(provider);
				break;
			}
			}
			return result;
		}

		public const LogLevel MaxLevel = LogLevel.Assert;
	}
}
