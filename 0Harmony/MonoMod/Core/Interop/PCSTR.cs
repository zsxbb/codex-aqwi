using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace MonoMod.Core.Interop
{
	[DebuggerDisplay("{DebuggerDisplay}")]
	internal readonly struct PCSTR : IEquatable<PCSTR>
	{
		internal unsafe PCSTR(byte* value)
		{
			this.Value = value;
		}

		public unsafe static implicit operator byte*(PCSTR value)
		{
			return value.Value;
		}

		public unsafe static explicit operator PCSTR(byte* value)
		{
			return new PCSTR(value);
		}

		public bool Equals(PCSTR other)
		{
			return this.Value == other.Value;
		}

		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			if (obj is PCSTR)
			{
				PCSTR other = (PCSTR)obj;
				return this.Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return this.Value;
		}

		internal unsafe int Length
		{
			get
			{
				byte* ptr = this.Value;
				if (ptr == null)
				{
					return 0;
				}
				while (*ptr != 0)
				{
					ptr++;
				}
				return checked((int)(unchecked((long)(ptr - this.Value))));
			}
		}

		[NullableContext(2)]
		public unsafe override string ToString()
		{
			if (this.Value != null)
			{
				return new string((sbyte*)this.Value, 0, this.Length, Encoding.UTF8);
			}
			return null;
		}

		[Nullable(2)]
		private string DebuggerDisplay
		{
			[NullableContext(2)]
			get
			{
				return this.ToString();
			}
		}

		internal unsafe System.ReadOnlySpan<byte> AsSpan()
		{
			if (this.Value != null)
			{
				return new System.ReadOnlySpan<byte>((void*)this.Value, this.Length);
			}
			return default(System.ReadOnlySpan<byte>);
		}

		internal unsafe readonly byte* Value;
	}
}
