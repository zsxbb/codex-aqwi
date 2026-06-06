using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerTypeProxy(typeof(ReadOnlySequenceDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	internal readonly struct ReadOnlySequence<[Nullable(2)] T>
	{
		public long Length
		{
			get
			{
				return this.GetLength();
			}
		}

		public bool IsEmpty
		{
			get
			{
				return this.Length == 0L;
			}
		}

		public bool IsSingleSegment
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._sequenceStart.GetObject() == this._sequenceEnd.GetObject();
			}
		}

		[Nullable(new byte[]
		{
			0,
			1
		})]
		public System.ReadOnlyMemory<T> First
		{
			[return: Nullable(new byte[]
			{
				0,
				1
			})]
			get
			{
				return this.GetFirstBuffer();
			}
		}

		public System.SequencePosition Start
		{
			get
			{
				return this._sequenceStart;
			}
		}

		public System.SequencePosition End
		{
			get
			{
				return this._sequenceEnd;
			}
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ReadOnlySequence(object startSegment, int startIndexAndFlags, object endSegment, int endIndexAndFlags)
		{
			this._sequenceStart = new System.SequencePosition(startSegment, startIndexAndFlags);
			this._sequenceEnd = new System.SequencePosition(endSegment, endIndexAndFlags);
		}

		public ReadOnlySequence(ReadOnlySequenceSegment<T> startSegment, int startIndex, ReadOnlySequenceSegment<T> endSegment, int endIndex)
		{
			if (startSegment == null || endSegment == null || (startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex) || startSegment.Memory.Length < startIndex || endSegment.Memory.Length < endIndex || (startSegment == endSegment && endIndex < startIndex))
			{
				ThrowHelper.ThrowArgumentValidationException<T>(startSegment, startIndex, endSegment);
			}
			this._sequenceStart = new System.SequencePosition(startSegment, ReadOnlySequence.SegmentToSequenceStart(startIndex));
			this._sequenceEnd = new System.SequencePosition(endSegment, ReadOnlySequence.SegmentToSequenceEnd(endIndex));
		}

		public ReadOnlySequence(T[] array)
		{
			ThrowHelper.ThrowIfArgumentNull(array, ExceptionArgument.array);
			this._sequenceStart = new System.SequencePosition(array, ReadOnlySequence.ArrayToSequenceStart(0));
			this._sequenceEnd = new System.SequencePosition(array, ReadOnlySequence.ArrayToSequenceEnd(array.Length));
		}

		public ReadOnlySequence(T[] array, int start, int length)
		{
			if (array == null || start > array.Length || length > array.Length - start)
			{
				ThrowHelper.ThrowArgumentValidationException(array, start);
			}
			this._sequenceStart = new System.SequencePosition(array, ReadOnlySequence.ArrayToSequenceStart(start));
			this._sequenceEnd = new System.SequencePosition(array, ReadOnlySequence.ArrayToSequenceEnd(start + length));
		}

		public unsafe ReadOnlySequence([Nullable(new byte[]
		{
			0,
			1
		})] System.ReadOnlyMemory<T> memory)
		{
			MemoryManager<T> @object;
			int startIndex;
			int num;
			if (System.Runtime.InteropServices.MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out @object, out startIndex, out num))
			{
				this._sequenceStart = new System.SequencePosition(@object, ReadOnlySequence.MemoryManagerToSequenceStart(startIndex));
				this._sequenceEnd = new System.SequencePosition(@object, ReadOnlySequence.MemoryManagerToSequenceEnd(num));
				return;
			}
			ArraySegment<T> arraySegment;
			if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray<T>(memory, out arraySegment))
			{
				T[] array = arraySegment.Array;
				int offset = arraySegment.Offset;
				this._sequenceStart = new System.SequencePosition(array, ReadOnlySequence.ArrayToSequenceStart(offset));
				this._sequenceEnd = new System.SequencePosition(array, ReadOnlySequence.ArrayToSequenceEnd(offset + arraySegment.Count));
				return;
			}
			if (typeof(T) == typeof(char))
			{
				string object2;
				int num2;
				if (!System.Runtime.InteropServices.MemoryMarshal.TryGetString(*Unsafe.As<System.ReadOnlyMemory<T>, System.ReadOnlyMemory<char>>(ref memory), out object2, out num2, out num))
				{
					ThrowHelper.ThrowInvalidOperationException();
				}
				this._sequenceStart = new System.SequencePosition(object2, ReadOnlySequence.StringToSequenceStart(num2));
				this._sequenceEnd = new System.SequencePosition(object2, ReadOnlySequence.StringToSequenceEnd(num2 + num));
				return;
			}
			ThrowHelper.ThrowInvalidOperationException();
			this._sequenceStart = default(System.SequencePosition);
			this._sequenceEnd = default(System.SequencePosition);
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public ReadOnlySequence<T> Slice(long start, long length)
		{
			if (start < 0L || length < 0L)
			{
				ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
			}
			int num = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			int index = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object @object = this._sequenceStart.GetObject();
			object object2 = this._sequenceEnd.GetObject();
			System.SequencePosition sequencePosition;
			System.SequencePosition endPosition;
			if (@object != object2)
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				int num2 = readOnlySequenceSegment.Memory.Length - num;
				if ((long)num2 > start)
				{
					num += (int)start;
					sequencePosition = new System.SequencePosition(@object, num);
					endPosition = ReadOnlySequence<T>.GetEndPosition(readOnlySequenceSegment, @object, num, object2, index, length);
				}
				else
				{
					if (num2 < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					sequencePosition = ReadOnlySequence<T>.SeekMultiSegment(readOnlySequenceSegment.Next, object2, index, start - (long)num2, ExceptionArgument.start);
					int index2 = ReadOnlySequence<T>.GetIndex(sequencePosition);
					object object3 = sequencePosition.GetObject();
					if (object3 != object2)
					{
						endPosition = ReadOnlySequence<T>.GetEndPosition((ReadOnlySequenceSegment<T>)object3, object3, index2, object2, index, length);
					}
					else
					{
						if ((long)(index - index2) < length)
						{
							ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
						}
						endPosition = new System.SequencePosition(object3, index2 + (int)length);
					}
				}
			}
			else
			{
				if ((long)(index - num) < start)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(-1L);
				}
				num += (int)start;
				sequencePosition = new System.SequencePosition(@object, num);
				if ((long)(index - num) < length)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
				endPosition = new System.SequencePosition(@object, num + (int)length);
			}
			return this.SliceImpl(sequencePosition, endPosition);
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public ReadOnlySequence<T> Slice(long start, System.SequencePosition end)
		{
			if (start < 0L)
			{
				ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
			}
			uint index = (uint)ReadOnlySequence<T>.GetIndex(end);
			object @object = end.GetObject();
			uint index2 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			object object2 = this._sequenceStart.GetObject();
			uint index3 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object object3 = this._sequenceEnd.GetObject();
			if (object2 == object3)
			{
				if (!ReadOnlySequence<T>.InRange(index, index2, index3))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if ((ulong)(index - index2) < (ulong)start)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(-1L);
				}
			}
			else
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)object2;
				ulong num = (ulong)(readOnlySequenceSegment.RunningIndex + (long)((ulong)index2));
				ulong num2 = (ulong)(((ReadOnlySequenceSegment<T>)@object).RunningIndex + (long)((ulong)index));
				if (!ReadOnlySequence<T>.InRange(num2, num, (ulong)(((ReadOnlySequenceSegment<T>)object3).RunningIndex + (long)((ulong)index3))))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (num + (ulong)start > num2)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
				}
				int num3 = readOnlySequenceSegment.Memory.Length - (int)index2;
				if ((long)num3 <= start)
				{
					if (num3 < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					System.SequencePosition sequencePosition = ReadOnlySequence<T>.SeekMultiSegment(readOnlySequenceSegment.Next, @object, (int)index, start - (long)num3, ExceptionArgument.start);
					return this.SliceImpl(sequencePosition, end);
				}
			}
			System.SequencePosition sequencePosition2 = new System.SequencePosition(object2, (int)(index2 + (uint)((int)start)));
			return this.SliceImpl(sequencePosition2, end);
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public ReadOnlySequence<T> Slice(System.SequencePosition start, long length)
		{
			uint index = (uint)ReadOnlySequence<T>.GetIndex(start);
			object @object = start.GetObject();
			uint index2 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			object object2 = this._sequenceStart.GetObject();
			uint index3 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object object3 = this._sequenceEnd.GetObject();
			if (object2 == object3)
			{
				if (!ReadOnlySequence<T>.InRange(index, index2, index3))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (length < 0L)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
				if ((ulong)(index3 - index) < (ulong)length)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
			}
			else
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				long num = readOnlySequenceSegment.RunningIndex + (long)((ulong)index);
				ulong start2 = (ulong)(((ReadOnlySequenceSegment<T>)object2).RunningIndex + (long)((ulong)index2));
				ulong num2 = (ulong)(((ReadOnlySequenceSegment<T>)object3).RunningIndex + (long)((ulong)index3));
				if (!ReadOnlySequence<T>.InRange((ulong)num, start2, num2))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (length < 0L)
				{
					ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
				}
				if (num + length > (long)num2)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
				}
				int num3 = readOnlySequenceSegment.Memory.Length - (int)index;
				if ((long)num3 < length)
				{
					if (num3 < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					System.SequencePosition sequencePosition = ReadOnlySequence<T>.SeekMultiSegment(readOnlySequenceSegment.Next, object3, (int)index3, length - (long)num3, ExceptionArgument.length);
					return this.SliceImpl(start, sequencePosition);
				}
			}
			System.SequencePosition sequencePosition2 = new System.SequencePosition(@object, (int)(index + (uint)((int)length)));
			return this.SliceImpl(start, sequencePosition2);
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public ReadOnlySequence<T> Slice(int start, int length)
		{
			return this.Slice((long)start, (long)length);
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public ReadOnlySequence<T> Slice(int start, System.SequencePosition end)
		{
			return this.Slice((long)start, end);
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public ReadOnlySequence<T> Slice(System.SequencePosition start, int length)
		{
			return this.Slice(start, (long)length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public ReadOnlySequence<T> Slice(System.SequencePosition start, System.SequencePosition end)
		{
			this.BoundsCheck((uint)ReadOnlySequence<T>.GetIndex(start), start.GetObject(), (uint)ReadOnlySequence<T>.GetIndex(end), end.GetObject());
			return this.SliceImpl(start, end);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public ReadOnlySequence<T> Slice(System.SequencePosition start)
		{
			this.BoundsCheck(start);
			return this.SliceImpl(start, this._sequenceEnd);
		}

		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		public ReadOnlySequence<T> Slice(long start)
		{
			if (start < 0L)
			{
				ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
			}
			if (start == 0L)
			{
				return this;
			}
			System.SequencePosition sequencePosition = ReadOnlySequence<T>.Seek(this._sequenceStart, this._sequenceEnd, start, ExceptionArgument.start);
			return this.SliceImpl(sequencePosition, this._sequenceEnd);
		}

		public unsafe override string ToString()
		{
			if (typeof(T) == typeof(char))
			{
				ReadOnlySequence<T> readOnlySequence = this;
				ReadOnlySequence<char> sequence = *Unsafe.As<ReadOnlySequence<T>, ReadOnlySequence<char>>(ref readOnlySequence);
				string text;
				int startIndex;
				int length;
				if (System.Runtime.InteropServices.SequenceMarshal.TryGetString(sequence, out text, out startIndex, out length))
				{
					return text.Substring(startIndex, length);
				}
				if (this.Length < 2147483647L)
				{
					return new string(sequence.ToArray<char>());
				}
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
			defaultInterpolatedStringHandler.AppendLiteral("System.Buffers.ReadOnlySequence<");
			defaultInterpolatedStringHandler.AppendFormatted(typeof(T).Name);
			defaultInterpolatedStringHandler.AppendLiteral(">[");
			defaultInterpolatedStringHandler.AppendFormatted<long>(this.Length);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		[NullableContext(0)]
		public ReadOnlySequence<T>.Enumerator GetEnumerator()
		{
			return new ReadOnlySequence<T>.Enumerator(ref this);
		}

		public System.SequencePosition GetPosition(long offset)
		{
			return this.GetPosition(offset, this._sequenceStart);
		}

		public System.SequencePosition GetPosition(long offset, System.SequencePosition origin)
		{
			if (offset < 0L)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();
			}
			return ReadOnlySequence<T>.Seek(origin, this._sequenceEnd, offset, ExceptionArgument.offset);
		}

		public bool TryGet(ref System.SequencePosition position, [Nullable(new byte[]
		{
			0,
			1
		})] out System.ReadOnlyMemory<T> memory, bool advance = true)
		{
			System.SequencePosition sequencePosition;
			bool result = this.TryGetBuffer(position, out memory, out sequencePosition);
			if (advance)
			{
				position = sequencePosition;
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGetBuffer(in System.SequencePosition position, [Nullable(new byte[]
		{
			0,
			1
		})] out System.ReadOnlyMemory<T> memory, out System.SequencePosition next)
		{
			object @object = position.GetObject();
			next = default(System.SequencePosition);
			if (@object == null)
			{
				memory = default(System.ReadOnlyMemory<T>);
				return false;
			}
			ReadOnlySequence<T>.SequenceType sequenceType = this.GetSequenceType();
			object object2 = this._sequenceEnd.GetObject();
			int index = ReadOnlySequence<T>.GetIndex(position);
			int index2 = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			if (sequenceType == ReadOnlySequence<T>.SequenceType.MultiSegment)
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				if (readOnlySequenceSegment != object2)
				{
					ReadOnlySequenceSegment<T> next2 = readOnlySequenceSegment.Next;
					if (next2 == null)
					{
						ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
					}
					next = new System.SequencePosition(next2, 0);
					memory = readOnlySequenceSegment.Memory.Slice(index);
				}
				else
				{
					memory = readOnlySequenceSegment.Memory.Slice(index, index2 - index);
				}
			}
			else
			{
				if (@object != object2)
				{
					ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
				}
				if (sequenceType == ReadOnlySequence<T>.SequenceType.Array)
				{
					memory = new System.ReadOnlyMemory<T>((T[])@object, index, index2 - index);
				}
				else if (typeof(T) == typeof(char) && sequenceType == ReadOnlySequence<T>.SequenceType.String)
				{
					memory = (System.ReadOnlyMemory<T>)((string)@object).AsMemory(index, index2 - index);
				}
				else
				{
					memory = ((MemoryManager<T>)@object).Memory.Slice(index, index2 - index);
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		private System.ReadOnlyMemory<T> GetFirstBuffer()
		{
			object @object = this._sequenceStart.GetObject();
			if (@object == null)
			{
				return default(System.ReadOnlyMemory<T>);
			}
			int num = this._sequenceStart.GetInteger();
			int integer = this._sequenceEnd.GetInteger();
			bool flag = @object != this._sequenceEnd.GetObject();
			if (num >= 0)
			{
				if (integer < 0)
				{
					if (flag)
					{
						ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
					}
					return new System.ReadOnlyMemory<T>((T[])@object, num, (integer & int.MaxValue) - num);
				}
				System.ReadOnlyMemory<T> memory = ((ReadOnlySequenceSegment<T>)@object).Memory;
				if (flag)
				{
					return memory.Slice(num);
				}
				return memory.Slice(num, integer - num);
			}
			else
			{
				if (flag)
				{
					ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
				}
				if (typeof(T) == typeof(char) && integer < 0)
				{
					return (System.ReadOnlyMemory<T>)((string)@object).AsMemory(num & int.MaxValue, integer - num);
				}
				num &= int.MaxValue;
				return ((MemoryManager<T>)@object).Memory.Slice(num, integer - num);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static System.SequencePosition Seek(in System.SequencePosition start, in System.SequencePosition end, long offset, ExceptionArgument argument)
		{
			int index = ReadOnlySequence<T>.GetIndex(start);
			int index2 = ReadOnlySequence<T>.GetIndex(end);
			object @object = start.GetObject();
			object object2 = end.GetObject();
			if (@object != object2)
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				int num = readOnlySequenceSegment.Memory.Length - index;
				if ((long)num <= offset)
				{
					if (num < 0)
					{
						ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					}
					return ReadOnlySequence<T>.SeekMultiSegment(readOnlySequenceSegment.Next, object2, index2, offset - (long)num, argument);
				}
			}
			else if ((long)(index2 - index) < offset)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(argument);
			}
			return new System.SequencePosition(@object, index + (int)offset);
		}

		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static System.SequencePosition SeekMultiSegment([Nullable(new byte[]
		{
			2,
			1
		})] ReadOnlySequenceSegment<T> currentSegment, object endObject, int endIndex, long offset, ExceptionArgument argument)
		{
			while (currentSegment != null && currentSegment != endObject)
			{
				int length = currentSegment.Memory.Length;
				if ((long)length > offset)
				{
					IL_3A:
					return new System.SequencePosition(currentSegment, (int)offset);
				}
				offset -= (long)length;
				currentSegment = currentSegment.Next;
			}
			if (currentSegment == null || (long)endIndex < offset)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(argument);
				goto IL_3A;
			}
			goto IL_3A;
		}

		private void BoundsCheck(in System.SequencePosition position)
		{
			uint index = (uint)ReadOnlySequence<T>.GetIndex(position);
			uint index2 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			uint index3 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object @object = this._sequenceStart.GetObject();
			object object2 = this._sequenceEnd.GetObject();
			if (@object == object2)
			{
				if (!ReadOnlySequence<T>.InRange(index, index2, index3))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					return;
				}
			}
			else
			{
				ulong start = (ulong)(((ReadOnlySequenceSegment<T>)@object).RunningIndex + (long)((ulong)index2));
				if (!ReadOnlySequence<T>.InRange((ulong)(((ReadOnlySequenceSegment<T>)position.GetObject()).RunningIndex + (long)((ulong)index)), start, (ulong)(((ReadOnlySequenceSegment<T>)object2).RunningIndex + (long)((ulong)index3))))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
			}
		}

		[NullableContext(2)]
		private void BoundsCheck(uint sliceStartIndex, object sliceStartObject, uint sliceEndIndex, object sliceEndObject)
		{
			uint index = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			uint index2 = (uint)ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object @object = this._sequenceStart.GetObject();
			object object2 = this._sequenceEnd.GetObject();
			if (@object == object2)
			{
				if (sliceStartObject != sliceEndObject || sliceStartObject != @object || sliceStartIndex > sliceEndIndex || sliceStartIndex < index || sliceEndIndex > index2)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
					return;
				}
			}
			else
			{
				long num = ((ReadOnlySequenceSegment<T>)sliceStartObject).RunningIndex + (long)((ulong)sliceStartIndex);
				ulong num2 = (ulong)(((ReadOnlySequenceSegment<T>)sliceEndObject).RunningIndex + (long)((ulong)sliceEndIndex));
				if (num > (long)num2)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				if (num < ((ReadOnlySequenceSegment<T>)@object).RunningIndex + (long)((ulong)index) || num2 > (ulong)(((ReadOnlySequenceSegment<T>)object2).RunningIndex + (long)((ulong)index2)))
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
			}
		}

		private static System.SequencePosition GetEndPosition(ReadOnlySequenceSegment<T> startSegment, object startObject, int startIndex, object endObject, int endIndex, long length)
		{
			int num = startSegment.Memory.Length - startIndex;
			if ((long)num > length)
			{
				return new System.SequencePosition(startObject, startIndex + (int)length);
			}
			if (num < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
			}
			return ReadOnlySequence<T>.SeekMultiSegment(startSegment.Next, endObject, endIndex, length - (long)num, ExceptionArgument.length);
		}

		[NullableContext(0)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ReadOnlySequence<T>.SequenceType GetSequenceType()
		{
			return (ReadOnlySequence<T>.SequenceType)(-(ReadOnlySequence<T>.SequenceType)(2 * (this._sequenceStart.GetInteger() >> 31) + (this._sequenceEnd.GetInteger() >> 31)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int GetIndex(in System.SequencePosition position)
		{
			return position.GetInteger() & int.MaxValue;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[]
		{
			0,
			1
		})]
		private ReadOnlySequence<T> SliceImpl(in System.SequencePosition start, in System.SequencePosition end)
		{
			return new ReadOnlySequence<T>(start.GetObject(), ReadOnlySequence<T>.GetIndex(start) | (this._sequenceStart.GetInteger() & int.MinValue), end.GetObject(), ReadOnlySequence<T>.GetIndex(end) | (this._sequenceEnd.GetInteger() & int.MinValue));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long GetLength()
		{
			int index = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			int index2 = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			object @object = this._sequenceStart.GetObject();
			object object2 = this._sequenceEnd.GetObject();
			if (@object != object2)
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
				return ((ReadOnlySequenceSegment<T>)object2).RunningIndex + (long)index2 - (readOnlySequenceSegment.RunningIndex + (long)index);
			}
			return (long)(index2 - index);
		}

		internal bool TryGetReadOnlySequenceSegment([Nullable(new byte[]
		{
			2,
			1
		})] out ReadOnlySequenceSegment<T> startSegment, out int startIndex, [Nullable(new byte[]
		{
			2,
			1
		})] out ReadOnlySequenceSegment<T> endSegment, out int endIndex)
		{
			object @object = this._sequenceStart.GetObject();
			if (@object == null || this.GetSequenceType() != ReadOnlySequence<T>.SequenceType.MultiSegment)
			{
				startSegment = null;
				startIndex = 0;
				endSegment = null;
				endIndex = 0;
				return false;
			}
			startSegment = (ReadOnlySequenceSegment<T>)@object;
			startIndex = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			endSegment = (ReadOnlySequenceSegment<T>)this._sequenceEnd.GetObject();
			endIndex = ReadOnlySequence<T>.GetIndex(this._sequenceEnd);
			return true;
		}

		internal bool TryGetArray([Nullable(new byte[]
		{
			0,
			1
		})] out ArraySegment<T> segment)
		{
			if (this.GetSequenceType() != ReadOnlySequence<T>.SequenceType.Array)
			{
				segment = default(ArraySegment<T>);
				return false;
			}
			int index = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			segment = new ArraySegment<T>((T[])this._sequenceStart.GetObject(), index, ReadOnlySequence<T>.GetIndex(this._sequenceEnd) - index);
			return true;
		}

		internal bool TryGetString([<1c2fb156-e9ba-45cc-af54-d5335bdb59af>MaybeNullWhen(false)] out string text, out int start, out int length)
		{
			if (typeof(T) != typeof(char) || this.GetSequenceType() != ReadOnlySequence<T>.SequenceType.String)
			{
				start = 0;
				length = 0;
				text = null;
				return false;
			}
			start = ReadOnlySequence<T>.GetIndex(this._sequenceStart);
			length = ReadOnlySequence<T>.GetIndex(this._sequenceEnd) - start;
			text = (string)this._sequenceStart.GetObject();
			return true;
		}

		private static bool InRange(uint value, uint start, uint end)
		{
			return value - start <= end - start;
		}

		private static bool InRange(ulong value, ulong start, ulong end)
		{
			return value - start <= end - start;
		}

		private readonly System.SequencePosition _sequenceStart;

		private readonly System.SequencePosition _sequenceEnd;

		[Nullable(new byte[]
		{
			0,
			1
		})]
		public static readonly ReadOnlySequence<T> Empty = new ReadOnlySequence<T>(SpanHelpers.PerTypeValues<T>.EmptyArray);

		[NullableContext(0)]
		public struct Enumerator
		{
			public Enumerator([Nullable(new byte[]
			{
				0,
				1
			})] in ReadOnlySequence<T> sequence)
			{
				this._currentMemory = default(System.ReadOnlyMemory<T>);
				this._next = sequence.Start;
				this._sequence = sequence;
			}

			[Nullable(new byte[]
			{
				0,
				1
			})]
			public System.ReadOnlyMemory<T> Current
			{
				[return: Nullable(new byte[]
				{
					0,
					1
				})]
				get
				{
					return this._currentMemory;
				}
			}

			public bool MoveNext()
			{
				return this._next.GetObject() != null && this._sequence.TryGet(ref this._next, out this._currentMemory, true);
			}

			[Nullable(new byte[]
			{
				0,
				1
			})]
			private readonly ReadOnlySequence<T> _sequence;

			private System.SequencePosition _next;

			[Nullable(new byte[]
			{
				0,
				1
			})]
			private System.ReadOnlyMemory<T> _currentMemory;
		}

		[NullableContext(0)]
		private enum SequenceType
		{
			MultiSegment,
			Array,
			MemoryManager,
			String,
			Empty
		}
	}
}
