using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	internal interface IMemoryOwner<[Nullable(2)] T> : IDisposable
	{
		[Nullable(new byte[]
		{
			0,
			1
		})]
		System.Memory<T> Memory { [return: Nullable(new byte[]
		{
			0,
			1
		})] get; }
	}
}
