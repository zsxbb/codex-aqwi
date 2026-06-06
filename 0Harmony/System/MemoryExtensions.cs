using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
	internal static class MemoryExtensions
	{
		public static System.ReadOnlySpan<char> Trim(this System.ReadOnlySpan<char> span)
		{
			return span.TrimStart().TrimEnd();
		}

		public unsafe static System.ReadOnlySpan<char> TrimStart(this System.ReadOnlySpan<char> span)
		{
			int num = 0;
			while (num < span.Length && char.IsWhiteSpace((char)(*span[num])))
			{
				num++;
			}
			return span.Slice(num);
		}

		public unsafe static System.ReadOnlySpan<char> TrimEnd(this System.ReadOnlySpan<char> span)
		{
			int num = span.Length - 1;
			while (num >= 0 && char.IsWhiteSpace((char)(*span[num])))
			{
				num--;
			}
			return span.Slice(0, num + 1);
		}

		public static System.ReadOnlySpan<char> Trim(this System.ReadOnlySpan<char> span, char trimChar)
		{
			return span.TrimStart(trimChar).TrimEnd(trimChar);
		}

		public unsafe static System.ReadOnlySpan<char> TrimStart(this System.ReadOnlySpan<char> span, char trimChar)
		{
			int num = 0;
			while (num < span.Length && *span[num] == (ushort)trimChar)
			{
				num++;
			}
			return span.Slice(num);
		}

		public unsafe static System.ReadOnlySpan<char> TrimEnd(this System.ReadOnlySpan<char> span, char trimChar)
		{
			int num = span.Length - 1;
			while (num >= 0 && *span[num] == (ushort)trimChar)
			{
				num--;
			}
			return span.Slice(0, num + 1);
		}

		public static System.ReadOnlySpan<char> Trim(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> trimChars)
		{
			return span.TrimStart(trimChars).TrimEnd(trimChars);
		}

		public unsafe static System.ReadOnlySpan<char> TrimStart(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> trimChars)
		{
			if (trimChars.IsEmpty)
			{
				return span.TrimStart();
			}
			int i = 0;
			IL_40:
			while (i < span.Length)
			{
				for (int j = 0; j < trimChars.Length; j++)
				{
					if (*span[i] == *trimChars[j])
					{
						i++;
						goto IL_40;
					}
				}
				break;
			}
			return span.Slice(i);
		}

		public unsafe static System.ReadOnlySpan<char> TrimEnd(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> trimChars)
		{
			if (trimChars.IsEmpty)
			{
				return span.TrimEnd();
			}
			int i = span.Length - 1;
			IL_48:
			while (i >= 0)
			{
				for (int j = 0; j < trimChars.Length; j++)
				{
					if (*span[i] == *trimChars[j])
					{
						i--;
						goto IL_48;
					}
				}
				break;
			}
			return span.Slice(0, i + 1);
		}

		public unsafe static bool IsWhiteSpace(this System.ReadOnlySpan<char> span)
		{
			for (int i = 0; i < span.Length; i++)
			{
				if (!char.IsWhiteSpace((char)(*span[i])))
				{
					return false;
				}
			}
			return true;
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOf<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, T value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, char>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, char>(ref value), span.Length);
			}
			return SpanHelpers.IndexOf<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value, span.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.IndexOf<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value), value.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOf<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, T value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, char>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, char>(ref value), span.Length);
			}
			return SpanHelpers.LastIndexOf<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value, span.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOf<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.LastIndexOf<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value), value.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SequenceEqual<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> other) where T : IEquatable<T>
		{
			int length = span.Length;
			UIntPtr uintPtr;
			if (default(T) != null && System.MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length == other.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other)), (UIntPtr)((IntPtr)length * (IntPtr)uintPtr));
			}
			return length == other.Length && SpanHelpers.SequenceEqual<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other), length);
		}

		public static int SequenceCompareTo<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> other) where T : IComparable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.SequenceCompareTo(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other)), other.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.SequenceCompareTo(Unsafe.As<T, char>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, char>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other)), other.Length);
			}
			return SpanHelpers.SequenceCompareTo<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other), other.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOf<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, T value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, char>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, char>(ref value), span.Length);
			}
			return SpanHelpers.IndexOf<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value, span.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOf(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.IndexOf<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value), value.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOf<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, T value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, char>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, char>(ref value), span.Length);
			}
			return SpanHelpers.LastIndexOf<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value, span.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOf<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOf(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value)), value.Length);
			}
			return SpanHelpers.LastIndexOf<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value), value.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOfAny<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, T value0, T value1) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), span.Length);
			}
			return SpanHelpers.IndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value0, value1, span.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOfAny<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), *Unsafe.As<T, byte>(ref value2), span.Length);
			}
			return SpanHelpers.IndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value0, value1, value2, span.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOfAny<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.IndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(values), values.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOfAny<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), span.Length);
			}
			return SpanHelpers.IndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value0, value1, span.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int IndexOfAny<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), *Unsafe.As<T, byte>(ref value2), span.Length);
			}
			return SpanHelpers.IndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value0, value1, value2, span.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOfAny<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.IndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.IndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(values), values.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, T value0, T value1) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), span.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value0, value1, span.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), *Unsafe.As<T, byte>(ref value2), span.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value0, value1, value2, span.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOfAny<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(values), values.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), span.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value0, value1, span.Length);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), *Unsafe.As<T, byte>(ref value0), *Unsafe.As<T, byte>(ref value1), *Unsafe.As<T, byte>(ref value2), span.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), value0, value1, value2, span.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LastIndexOfAny<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.LastIndexOfAny(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(values)), values.Length);
			}
			return SpanHelpers.LastIndexOfAny<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(values), values.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SequenceEqual<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> other) where T : IEquatable<T>
		{
			int length = span.Length;
			UIntPtr uintPtr;
			if (default(T) != null && System.MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length == other.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other)), (UIntPtr)((IntPtr)length * (IntPtr)uintPtr));
			}
			return length == other.Length && SpanHelpers.SequenceEqual<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other), length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SequenceCompareTo<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> other) where T : IComparable<T>
		{
			if (typeof(T) == typeof(byte))
			{
				return SpanHelpers.SequenceCompareTo(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other)), other.Length);
			}
			if (typeof(T) == typeof(char))
			{
				return SpanHelpers.SequenceCompareTo(Unsafe.As<T, char>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), span.Length, Unsafe.As<T, char>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other)), other.Length);
			}
			return SpanHelpers.SequenceCompareTo<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), span.Length, System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other), other.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int length = value.Length;
			UIntPtr uintPtr;
			if (default(T) != null && System.MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length <= span.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)length * (IntPtr)uintPtr));
			}
			return length <= span.Length && SpanHelpers.SequenceEqual<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value), length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int length = value.Length;
			UIntPtr uintPtr;
			if (default(T) != null && System.MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length <= span.Length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span)), Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)length * (IntPtr)uintPtr));
			}
			return length <= span.Length && SpanHelpers.SequenceEqual<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value), length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWith<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int length = span.Length;
			int length2 = value.Length;
			UIntPtr uintPtr;
			if (default(T) != null && System.MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length2 <= length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(Unsafe.Add<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), length - length2)), Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)length2 * (IntPtr)uintPtr));
			}
			return length2 <= length && SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), length - length2), System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value), length2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWith<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			int length = span.Length;
			int length2 = value.Length;
			UIntPtr uintPtr;
			if (default(T) != null && System.MemoryExtensions.IsTypeComparableAsBytes<T>(out uintPtr))
			{
				return length2 <= length && SpanHelpers.SequenceEqual(Unsafe.As<T, byte>(Unsafe.Add<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), length - length2)), Unsafe.As<T, byte>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value)), (UIntPtr)((IntPtr)length2 * (IntPtr)uintPtr));
			}
			return length2 <= length && SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), length - length2), System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(value), length2);
		}

		[NullableContext(2)]
		public static void Reverse<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span)
		{
			if (span.Length <= 1)
			{
				return;
			}
			ref T ptr = ref System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span);
			ref T ptr2 = ref Unsafe.Add<T>(Unsafe.Add<T>(ref ptr, span.Length), -1);
			do
			{
				T t = ptr;
				ptr = ptr2;
				ptr2 = t;
				ptr = Unsafe.Add<T>(ref ptr, 1);
				ptr2 = Unsafe.Add<T>(ref ptr2, -1);
			}
			while (Unsafe.IsAddressLessThan<T>(ref ptr, ref ptr2));
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Span<T> AsSpan<T>([Nullable(new byte[]
		{
			2,
			1
		})] this T[] array)
		{
			return new System.Span<T>(array);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Span<T> AsSpan<T>([Nullable(new byte[]
		{
			2,
			1
		})] this T[] array, int start, int length)
		{
			return new System.Span<T>(array, start, length);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Span<T> AsSpan<T>([Nullable(new byte[]
		{
			0,
			1
		})] this ArraySegment<T> segment)
		{
			return new System.Span<T>(segment.Array, segment.Offset, segment.Count);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Span<T> AsSpan<T>([Nullable(new byte[]
		{
			0,
			1
		})] this ArraySegment<T> segment, int start)
		{
			if ((ulong)start > (ulong)((long)segment.Count))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new System.Span<T>(segment.Array, segment.Offset + start, segment.Count - start);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Span<T> AsSpan<T>([Nullable(new byte[]
		{
			0,
			1
		})] this ArraySegment<T> segment, int start, int length)
		{
			if ((ulong)start > (ulong)((long)segment.Count))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			if ((ulong)length > (ulong)((long)(segment.Count - start)))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
			}
			return new System.Span<T>(segment.Array, segment.Offset + start, length);
		}

		[NullableContext(2)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Memory<T> AsMemory<T>([Nullable(new byte[]
		{
			2,
			1
		})] this T[] array)
		{
			return new System.Memory<T>(array);
		}

		[NullableContext(2)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Memory<T> AsMemory<T>([Nullable(new byte[]
		{
			2,
			1
		})] this T[] array, int start)
		{
			return new System.Memory<T>(array, start);
		}

		[NullableContext(2)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Memory<T> AsMemory<T>([Nullable(new byte[]
		{
			2,
			1
		})] this T[] array, int start, int length)
		{
			return new System.Memory<T>(array, start, length);
		}

		[NullableContext(2)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Memory<T> AsMemory<T>([Nullable(new byte[]
		{
			0,
			1
		})] this ArraySegment<T> segment)
		{
			return new System.Memory<T>(segment.Array, segment.Offset, segment.Count);
		}

		[NullableContext(2)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Memory<T> AsMemory<T>([Nullable(new byte[]
		{
			0,
			1
		})] this ArraySegment<T> segment, int start)
		{
			if ((ulong)start > (ulong)((long)segment.Count))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new System.Memory<T>(segment.Array, segment.Offset + start, segment.Count - start);
		}

		[NullableContext(2)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Memory<T> AsMemory<T>([Nullable(new byte[]
		{
			0,
			1
		})] this ArraySegment<T> segment, int start, int length)
		{
			if ((ulong)start > (ulong)((long)segment.Count))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			if ((ulong)length > (ulong)((long)(segment.Count - start)))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
			}
			return new System.Memory<T>(segment.Array, segment.Offset + start, length);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyTo<T>([Nullable(new byte[]
		{
			2,
			1
		})] this T[] source, [Nullable(new byte[]
		{
			0,
			1
		})] System.Span<T> destination)
		{
			new System.ReadOnlySpan<T>(source).CopyTo(destination);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyTo<T>([Nullable(new byte[]
		{
			2,
			1
		})] this T[] source, [Nullable(new byte[]
		{
			0,
			1
		})] System.Memory<T> destination)
		{
			source.CopyTo(destination.Span);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Overlaps<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> other)
		{
			return span.Overlaps(other);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Overlaps<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> other, out int elementOffset)
		{
			return span.Overlaps(other, out elementOffset);
		}

		[NullableContext(2)]
		public static bool Overlaps<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> other)
		{
			if (span.IsEmpty || other.IsEmpty)
			{
				return false;
			}
			IntPtr value = Unsafe.ByteOffset<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other));
			if (Unsafe.SizeOf<IntPtr>() == 4)
			{
				return (int)value < span.Length * Unsafe.SizeOf<T>() || (int)value > -(other.Length * Unsafe.SizeOf<T>());
			}
			return (long)value < (long)span.Length * (long)Unsafe.SizeOf<T>() || (long)value > -((long)other.Length * (long)Unsafe.SizeOf<T>());
		}

		[NullableContext(2)]
		public static bool Overlaps<T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, [Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlySpan<T> other, out int elementOffset)
		{
			if (span.IsEmpty || other.IsEmpty)
			{
				elementOffset = 0;
				return false;
			}
			IntPtr value = Unsafe.ByteOffset<T>(System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(span), System.Runtime.InteropServices.MemoryMarshal.GetReference<T>(other));
			if (Unsafe.SizeOf<IntPtr>() == 4)
			{
				if ((int)value < span.Length * Unsafe.SizeOf<T>() || (int)value > -(other.Length * Unsafe.SizeOf<T>()))
				{
					if ((int)value % Unsafe.SizeOf<T>() != 0)
					{
						ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();
					}
					elementOffset = (int)value / Unsafe.SizeOf<T>();
					return true;
				}
				elementOffset = 0;
				return false;
			}
			else
			{
				if ((long)value < (long)span.Length * (long)Unsafe.SizeOf<T>() || (long)value > -((long)other.Length * (long)Unsafe.SizeOf<T>()))
				{
					if ((long)value % (long)Unsafe.SizeOf<T>() != 0L)
					{
						ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();
					}
					elementOffset = (int)((long)value / (long)Unsafe.SizeOf<T>());
					return true;
				}
				elementOffset = 0;
				return false;
			}
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, IComparable<T> comparable)
		{
			return span.BinarySearch(comparable);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, TComparable comparable) where TComparable : IComparable<T>
		{
			return span.BinarySearch(comparable);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparer>([Nullable(new byte[]
		{
			0,
			1
		})] this System.Span<T> span, T value, TComparer comparer) where TComparer : IComparer<T>
		{
			return span.BinarySearch(value, comparer);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, IComparable<T> comparable)
		{
			return span.BinarySearch(comparable);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, TComparable comparable) where TComparable : IComparable<T>
		{
			return span.BinarySearch(comparable);
		}

		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparer>([Nullable(new byte[]
		{
			0,
			1
		})] this System.ReadOnlySpan<T> span, T value, TComparer comparer) where TComparer : IComparer<T>
		{
			if (comparer == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparer);
			}
			SpanHelpers.ComparerComparable<T, TComparer> comparable = new SpanHelpers.ComparerComparable<T, TComparer>(value, comparer);
			return span.BinarySearch(comparable);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsTypeComparableAsBytes<T>([NativeInteger] out UIntPtr size)
		{
			if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
			{
				size = (UIntPtr)((IntPtr)1);
				return true;
			}
			if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
			{
				size = (UIntPtr)((IntPtr)2);
				return true;
			}
			if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
			{
				size = (UIntPtr)((IntPtr)4);
				return true;
			}
			if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
			{
				size = (UIntPtr)((IntPtr)8);
				return true;
			}
			size = (UIntPtr)((IntPtr)0);
			return false;
		}

		[NullableContext(2)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Span<T> AsSpan<T>([Nullable(new byte[]
		{
			2,
			1
		})] this T[] array, int start)
		{
			return System.Span<T>.Create(array, start);
		}

		public static bool Contains(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			return span.IndexOf(value, comparisonType) >= 0;
		}

		public static bool Equals(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> other, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.SequenceEqual(other);
			}
			if (comparisonType == StringComparison.OrdinalIgnoreCase)
			{
				return span.Length == other.Length && System.MemoryExtensions.EqualsOrdinalIgnoreCase(span, other);
			}
			return span.ToString().Equals(other.ToString(), comparisonType);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool EqualsOrdinalIgnoreCase(System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> other)
		{
			return other.Length == 0 || System.MemoryExtensions.CompareToOrdinalIgnoreCase(span, other) == 0;
		}

		public static int CompareTo(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> other, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.SequenceCompareTo(other);
			}
			if (comparisonType == StringComparison.OrdinalIgnoreCase)
			{
				return System.MemoryExtensions.CompareToOrdinalIgnoreCase(span, other);
			}
			return string.Compare(span.ToString(), other.ToString(), comparisonType);
		}

		private unsafe static int CompareToOrdinalIgnoreCase(System.ReadOnlySpan<char> strA, System.ReadOnlySpan<char> strB)
		{
			int num = Math.Min(strA.Length, strB.Length);
			int num2 = num;
			fixed (char* reference = System.Runtime.InteropServices.MemoryMarshal.GetReference<char>(strA))
			{
				char* ptr = reference;
				fixed (char* reference2 = System.Runtime.InteropServices.MemoryMarshal.GetReference<char>(strB))
				{
					char* ptr2 = reference2;
					char* ptr3 = ptr;
					char* ptr4 = ptr2;
					while (num != 0 && *ptr3 <= '\u007f' && *ptr4 <= '\u007f')
					{
						int num3 = (int)(*ptr3);
						int num4 = (int)(*ptr4);
						if (num3 == num4)
						{
							ptr3++;
							ptr4++;
							num--;
						}
						else
						{
							if (num3 - 97 <= 25)
							{
								num3 -= 32;
							}
							if (num4 - 97 <= 25)
							{
								num4 -= 32;
							}
							if (num3 != num4)
							{
								return num3 - num4;
							}
							ptr3++;
							ptr4++;
							num--;
						}
					}
					if (num == 0)
					{
						return strA.Length - strB.Length;
					}
					num2 -= num;
					return string.Compare(strA.Slice(num2).ToString(), strB.Slice(num2).ToString(), StringComparison.OrdinalIgnoreCase);
				}
			}
		}

		public static int IndexOf(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.IndexOf(value);
			}
			return span.ToString().IndexOf(value.ToString(), comparisonType);
		}

		public static int ToLower(this System.ReadOnlySpan<char> source, System.Span<char> destination, [Nullable(1)] CultureInfo culture)
		{
			ThrowHelper.ThrowIfArgumentNull(culture, ExceptionArgument.culture);
			if (destination.Length < source.Length)
			{
				return -1;
			}
			string str = source.ToString();
			culture.TextInfo.ToLower(str).AsSpan().CopyTo(destination);
			return source.Length;
		}

		public static int ToLowerInvariant(this System.ReadOnlySpan<char> source, System.Span<char> destination)
		{
			return source.ToLower(destination, CultureInfo.InvariantCulture);
		}

		public static int ToUpper(this System.ReadOnlySpan<char> source, System.Span<char> destination, [Nullable(1)] CultureInfo culture)
		{
			ThrowHelper.ThrowIfArgumentNull(culture, ExceptionArgument.culture);
			if (destination.Length < source.Length)
			{
				return -1;
			}
			string str = source.ToString();
			culture.TextInfo.ToUpper(str).AsSpan().CopyTo(destination);
			return source.Length;
		}

		public static int ToUpperInvariant(this System.ReadOnlySpan<char> source, System.Span<char> destination)
		{
			return source.ToUpper(destination, CultureInfo.InvariantCulture);
		}

		public static bool EndsWith(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.EndsWith(value);
			}
			if (comparisonType == StringComparison.OrdinalIgnoreCase)
			{
				return value.Length <= span.Length && System.MemoryExtensions.EqualsOrdinalIgnoreCase(span.Slice(span.Length - value.Length), value);
			}
			string text = span.ToString();
			string value2 = value.ToString();
			return text.EndsWith(value2, comparisonType);
		}

		public static bool StartsWith(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (comparisonType == StringComparison.Ordinal)
			{
				return span.StartsWith(value);
			}
			if (comparisonType == StringComparison.OrdinalIgnoreCase)
			{
				return value.Length <= span.Length && System.MemoryExtensions.EqualsOrdinalIgnoreCase(span.Slice(0, value.Length), value);
			}
			string text = span.ToString();
			string value2 = value.ToString();
			return text.StartsWith(value2, comparisonType);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static System.ReadOnlySpan<char> AsSpan([Nullable(2)] this string text)
		{
			if (text == null)
			{
				return default(System.ReadOnlySpan<char>);
			}
			return new System.ReadOnlySpan<char>(text, (IntPtr)RuntimeHelpers.OffsetToStringData, text.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static System.ReadOnlySpan<char> AsSpan([Nullable(2)] this string text, int start)
		{
			if (text == null)
			{
				if (start != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(System.ReadOnlySpan<char>);
			}
			if (start > text.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new System.ReadOnlySpan<char>(text, (IntPtr)RuntimeHelpers.OffsetToStringData + (IntPtr)(start * 2), text.Length - start);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static System.ReadOnlySpan<char> AsSpan([Nullable(2)] this string text, int start, int length)
		{
			if (text == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(System.ReadOnlySpan<char>);
			}
			if (start > text.Length || length > text.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new System.ReadOnlySpan<char>(text, (IntPtr)RuntimeHelpers.OffsetToStringData + (IntPtr)(start * 2), length);
		}

		public static System.ReadOnlyMemory<char> AsMemory([Nullable(2)] this string text)
		{
			if (text == null)
			{
				return default(System.ReadOnlyMemory<char>);
			}
			return new System.ReadOnlyMemory<char>(text, 0, text.Length);
		}

		public static System.ReadOnlyMemory<char> AsMemory([Nullable(2)] this string text, int start)
		{
			if (text == null)
			{
				if (start != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(System.ReadOnlyMemory<char>);
			}
			if (start > text.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new System.ReadOnlyMemory<char>(text, start, text.Length - start);
		}

		public static System.ReadOnlyMemory<char> AsMemory([Nullable(2)] this string text, int start, int length)
		{
			if (text == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(System.ReadOnlyMemory<char>);
			}
			if (start > text.Length || length > text.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return new System.ReadOnlyMemory<char>(text, start, length);
		}
	}
}
