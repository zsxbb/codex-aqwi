using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MonoMod;

namespace System
{
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(SpanDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	internal readonly ref struct Span<[Nullable(2)] T>
	{
		public int Length
		{
			get
			{
				return this._length;
			}
		}

		public bool IsEmpty
		{
			get
			{
				return this._length == 0;
			}
		}

		public static bool operator !=([Nullable(new byte[]
		{
			0,
			1
		})] System.Span<T> left, [Nullable(new byte[]
		{
			0,
			1
		})] System.Span<T> right)
		{
			return !(left == right);
		}

		[NullableContext(2)]
		[Obsolete("Equals() on Span will always throw an exception. Use == instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			throw new NotSupportedException("Cannot call Equals on Span");
		}

		[Obsolete("GetHashCode() on Span will always throw an exception.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			throw new NotSupportedException("Cannot call GetHashCode on Span");
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static implicit operator System.Span<T>([Nullable(new byte[]
		{
			2,
			1
		})] T[] array)
		{
			return new System.Span<T>(array);
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static implicit operator System.Span<T>([Nullable(new byte[]
		{
			0,
			1
		})] ArraySegment<T> segment)
		{
			return new System.Span<T>(segment.Array, segment.Offset, segment.Count);
		}

		[Nullable(new byte[]
		{
			0,
			1
		})]
		public static System.Span<T> Empty
		{
			[return: Nullable(new byte[]
			{
				0,
				1
			})]
			get
			{
				return default(System.Span<T>);
			}
		}

		[NullableContext(0)]
		public System.Span<T>.Enumerator GetEnumerator()
		{
			return new System.Span<T>.Enumerator(this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span([Nullable(new byte[]
		{
			2,
			1
		})] T[] array)
		{
			if (array == null)
			{
				this = default(System.Span<T>);
				return;
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			this._length = array.Length;
			this._pinnable = array;
			this._byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		internal static System.Span<T> Create([Nullable(new byte[]
		{
			2,
			1
		})] T[] array, int start)
		{
			if (array == null)
			{
				if (start != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				return default(System.Span<T>);
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			if (start > array.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment.Add(start);
			int length = array.Length - start;
			return new System.Span<T>(array, byteOffset, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span([Nullable(new byte[]
		{
			2,
			1
		})] T[] array, int start, int length)
		{
			if (array == null)
			{
				if (start != 0 || length != 0)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				this = default(System.Span<T>);
				return;
			}
			if (default(T) == null && array.GetType() != typeof(T[]))
			{
				ThrowHelper.ThrowArrayTypeMismatchException();
			}
			if (start > array.Length || length > array.Length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			this._length = length;
			this._pinnable = array;
			this._byteOffset = SpanHelpers.PerTypeValues<T>.ArrayAdjustment.Add(start);
		}

		[NullableContext(0)]
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Span(void* pointer, int length)
		{
			if (SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
			}
			if (length < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			this._length = length;
			this._pinnable = null;
			this._byteOffset = new IntPtr(pointer);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Span(object pinnable, IntPtr byteOffset, int length)
		{
			this._length = length;
			this._pinnable = pinnable;
			this._byteOffset = byteOffset;
		}

		public T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (index >= this._length)
				{
					ThrowHelper.ThrowIndexOutOfRangeException();
				}
				return Unsafe.Add<T>(this.DangerousGetPinnableReference(), index);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public ref T GetPinnableReference()
		{
			if (this._length != 0)
			{
				return this.DangerousGetPinnableReference();
			}
			return Unsafe.NullRef<T>();
		}

		public unsafe void Clear()
		{
			int length = this._length;
			if (length == 0)
			{
				return;
			}
			UIntPtr byteLength = (UIntPtr)((ulong)length * (ulong)((long)Unsafe.SizeOf<T>()));
			if ((Unsafe.SizeOf<T>() & sizeof(IntPtr) - 1) != 0)
			{
				if (this._pinnable == null)
				{
					byte* ptr = (byte*)this._byteOffset.ToPointer();
					SpanHelpers.ClearLessThanPointerSized(ptr, byteLength);
					return;
				}
				SpanHelpers.ClearLessThanPointerSized(Unsafe.As<T, byte>(this.DangerousGetPinnableReference()), byteLength);
				return;
			}
			else
			{
				if (SpanHelpers.IsReferenceOrContainsReferences<T>())
				{
					UIntPtr pointerSizeLength = (UIntPtr)((ulong)((long)(length * Unsafe.SizeOf<T>() / sizeof(IntPtr))));
					SpanHelpers.ClearPointerSizedWithReferences(Unsafe.As<T, IntPtr>(this.DangerousGetPinnableReference()), pointerSizeLength);
					return;
				}
				SpanHelpers.ClearPointerSizedWithoutReferences(Unsafe.As<T, byte>(this.DangerousGetPinnableReference()), byteLength);
				return;
			}
		}

		public unsafe void Fill(T value)
		{
			int length = this._length;
			if (length == 0)
			{
				return;
			}
			if (Unsafe.SizeOf<T>() == 1)
			{
				byte value2 = *Unsafe.As<T, byte>(ref value);
				Unsafe.InitBlockUnaligned(Unsafe.As<T, byte>(this.DangerousGetPinnableReference()), value2, (uint)length);
				return;
			}
			ref T source = ref this.DangerousGetPinnableReference();
			int i;
			for (i = 0; i < (length & -8); i += 8)
			{
				*Unsafe.Add<T>(ref source, i) = value;
				*Unsafe.Add<T>(ref source, i + 1) = value;
				*Unsafe.Add<T>(ref source, i + 2) = value;
				*Unsafe.Add<T>(ref source, i + 3) = value;
				*Unsafe.Add<T>(ref source, i + 4) = value;
				*Unsafe.Add<T>(ref source, i + 5) = value;
				*Unsafe.Add<T>(ref source, i + 6) = value;
				*Unsafe.Add<T>(ref source, i + 7) = value;
			}
			if (i < (length & -4))
			{
				*Unsafe.Add<T>(ref source, i) = value;
				*Unsafe.Add<T>(ref source, i + 1) = value;
				*Unsafe.Add<T>(ref source, i + 2) = value;
				*Unsafe.Add<T>(ref source, i + 3) = value;
				i += 4;
			}
			while (i < length)
			{
				*Unsafe.Add<T>(ref source, i) = value;
				i++;
			}
		}

		public void CopyTo([Nullable(new byte[]
		{
			0,
			1
		})] System.Span<T> destination)
		{
			if (!this.TryCopyTo(destination))
			{
				ThrowHelper.ThrowArgumentException_DestinationTooShort();
			}
		}

		public bool TryCopyTo([Nullable(new byte[]
		{
			0,
			1
		})] System.Span<T> destination)
		{
			int length = this._length;
			int length2 = destination._length;
			if (length == 0)
			{
				return true;
			}
			if (length > length2)
			{
				return false;
			}
			ref T src = ref this.DangerousGetPinnableReference();
			SpanHelpers.CopyTo<T>(destination.DangerousGetPinnableReference(), length2, ref src, length);
			return true;
		}

		public static bool operator ==([Nullable(new byte[]
		{
			0,
			1
		})] System.Span<T> left, [Nullable(new byte[]
		{
			0,
			1
		})] System.Span<T> right)
		{
			return left._length == right._length && Unsafe.AreSame<T>(left.DangerousGetPinnableReference(), right.DangerousGetPinnableReference());
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public static implicit operator System.ReadOnlySpan<T>([Nullable(new byte[]
		{
			0,
			1
		})] System.Span<T> span)
		{
			return new System.ReadOnlySpan<T>(span._pinnable, span._byteOffset, span._length);
		}

		public unsafe override string ToString()
		{
			if (typeof(T) == typeof(char))
			{
				fixed (char* ptr = Unsafe.As<T, char>(this.DangerousGetPinnableReference()))
				{
					return new string(ptr, 0, this._length);
				}
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 2);
			defaultInterpolatedStringHandler.AppendLiteral("System.Span<");
			defaultInterpolatedStringHandler.AppendFormatted(typeof(T).Name);
			defaultInterpolatedStringHandler.AppendLiteral(">[");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this._length);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public System.Span<T> Slice(int start)
		{
			if (start > this._length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr byteOffset = this._byteOffset.Add(start);
			int length = this._length - start;
			return new System.Span<T>(this._pinnable, byteOffset, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public System.Span<T> Slice(int start, int length)
		{
			if (start > this._length || length > this._length - start)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			IntPtr byteOffset = this._byteOffset.Add(start);
			return new System.Span<T>(this._pinnable, byteOffset, length);
		}

		public T[] ToArray()
		{
			if (this._length == 0)
			{
				return SpanHelpers.PerTypeValues<T>.EmptyArray;
			}
			T[] array = new T[this._length];
			this.CopyTo(array);
			return array;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ref T DangerousGetPinnableReference()
		{
			return Unsafe.AddByteOffset<T>(ILHelpers.ObjectAsRef<T>(this._pinnable), this._byteOffset);
		}

		[Nullable(2)]
		internal object Pinnable
		{
			[NullableContext(2)]
			get
			{
				return this._pinnable;
			}
		}

		internal IntPtr ByteOffset
		{
			get
			{
				return this._byteOffset;
			}
		}

		[Nullable(2)]
		private readonly object _pinnable;

		private readonly IntPtr _byteOffset;

		private readonly int _length;

		[Nullable(0)]
		public ref struct Enumerator
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal Enumerator([Nullable(new byte[]
			{
				0,
				1
			})] System.Span<T> span)
			{
				this._span = span;
				this._index = -1;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				int num = this._index + 1;
				if (num < this._span.Length)
				{
					this._index = num;
					return true;
				}
				return false;
			}

			public ref T Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return this._span[this._index];
				}
			}

			[Nullable(new byte[]
			{
				0,
				1
			})]
			private readonly System.Span<T> _span;

			private int _index;
		}
	}
}
