using System;

namespace MonoMod.Core.Platforms
{
	internal interface IAllocatedMemory : IDisposable
	{
		bool IsExecutable { get; }

		IntPtr BaseAddress { get; }

		int Size { get; }

		System.Span<byte> Memory { get; }
	}
}
