using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms
{
	internal interface IControlFlowGuard
	{
		bool IsSupported { get; }

		int TargetAlignmentRequirement { get; }

		unsafe void RegisterValidIndirectCallTargets(void* memoryRegionStart, [NativeInteger] IntPtr memoryRegionLength, [NativeInteger] System.ReadOnlySpan<IntPtr> validTargetsInMemoryRegion);
	}
}
