using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoMod.Utils.Interop
{
	internal static class Unix
	{
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "uname", SetLastError = true)]
		public unsafe static extern int Uname(byte* buf);

		[DllImport("dl", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlopen")]
		private unsafe static extern IntPtr DL1dlopen(byte* filename, Unix.DlopenFlags flags);

		[DllImport("dl", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlclose")]
		private static extern int DL1dlclose(IntPtr handle);

		[DllImport("dl", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlsym")]
		private unsafe static extern IntPtr DL1dlsym(IntPtr handle, byte* symbol);

		[DllImport("dl", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlerror")]
		private static extern IntPtr DL1dlerror();

		[DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlopen")]
		private unsafe static extern IntPtr DL2dlopen(byte* filename, Unix.DlopenFlags flags);

		[DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlclose")]
		private static extern int DL2dlclose(IntPtr handle);

		[DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlsym")]
		private unsafe static extern IntPtr DL2dlsym(IntPtr handle, byte* symbol);

		[DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlerror")]
		private static extern IntPtr DL2dlerror();

		[NullableContext(2)]
		internal static byte[] MarshalToUtf8(string str)
		{
			if (str == null)
			{
				return null;
			}
			int byteCount = Encoding.UTF8.GetByteCount(str);
			byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(byteCount + 1);
			array.AsSpan<byte>().Clear();
			Encoding.UTF8.GetBytes(str, 0, str.Length, array, 0);
			return array;
		}

		[NullableContext(2)]
		internal static void FreeMarshalledArray(byte[] arr)
		{
			if (arr == null)
			{
				return;
			}
			System.Buffers.ArrayPool<byte>.Shared.Return(arr, false);
		}

		[NullableContext(2)]
		public unsafe static IntPtr DlOpen(string filename, Unix.DlopenFlags flags)
		{
			byte[] array = Unix.MarshalToUtf8(filename);
			IntPtr result;
			try
			{
				for (;;)
				{
					try
					{
						try
						{
							byte[] array2;
							byte* filename2;
							if ((array2 = array) == null || array2.Length == 0)
							{
								filename2 = null;
							}
							else
							{
								filename2 = &array2[0];
							}
							int num = Unix.dlVersion;
							if (num != 0 && num == 1)
							{
								result = Unix.DL2dlopen(filename2, flags);
								break;
							}
							result = Unix.DL1dlopen(filename2, flags);
							break;
						}
						finally
						{
							byte[] array2 = null;
						}
					}
					catch (DllNotFoundException obj) when (Unix.dlVersion > 0)
					{
						Unix.dlVersion--;
					}
				}
			}
			finally
			{
				Unix.FreeMarshalledArray(array);
			}
			return result;
		}

		public static bool DlClose(IntPtr handle)
		{
			bool result;
			for (;;)
			{
				try
				{
					int num = Unix.dlVersion;
					if (num != 0 && num == 1)
					{
						result = (Unix.DL2dlclose(handle) == 0);
					}
					else
					{
						result = (Unix.DL1dlclose(handle) == 0);
					}
				}
				catch (DllNotFoundException obj) when (Unix.dlVersion > 0)
				{
					Unix.dlVersion--;
					continue;
				}
				break;
			}
			return result;
		}

		[NullableContext(1)]
		public unsafe static IntPtr DlSym(IntPtr handle, string symbol)
		{
			byte[] array = Unix.MarshalToUtf8(symbol);
			IntPtr result;
			try
			{
				for (;;)
				{
					try
					{
						try
						{
							byte[] array2;
							byte* symbol2;
							if ((array2 = array) == null || array2.Length == 0)
							{
								symbol2 = null;
							}
							else
							{
								symbol2 = &array2[0];
							}
							int num = Unix.dlVersion;
							if (num != 0 && num == 1)
							{
								result = Unix.DL2dlsym(handle, symbol2);
								break;
							}
							result = Unix.DL1dlsym(handle, symbol2);
							break;
						}
						finally
						{
							byte[] array2 = null;
						}
					}
					catch (DllNotFoundException obj) when (Unix.dlVersion > 0)
					{
						Unix.dlVersion--;
					}
				}
			}
			finally
			{
				Unix.FreeMarshalledArray(array);
			}
			return result;
		}

		public static IntPtr DlError()
		{
			IntPtr result;
			for (;;)
			{
				try
				{
					int num = Unix.dlVersion;
					if (num != 0 && num == 1)
					{
						result = Unix.DL2dlerror();
					}
					else
					{
						result = Unix.DL1dlerror();
					}
				}
				catch (DllNotFoundException obj) when (Unix.dlVersion > 0)
				{
					Unix.dlVersion--;
					continue;
				}
				break;
			}
			return result;
		}

		[Nullable(1)]
		public const string LibC = "libc";

		[Nullable(1)]
		public const string DL1 = "dl";

		[Nullable(1)]
		public const string DL2 = "libdl.so.2";

		public const int AT_PLATFORM = 15;

		private static int dlVersion = 1;

		public struct LinuxAuxvEntry
		{
			[NativeInteger]
			public IntPtr Key;

			[NativeInteger]
			public IntPtr Value;
		}

		public enum DlopenFlags
		{
			RTLD_LAZY = 1,
			RTLD_NOW,
			RTLD_LOCAL = 0,
			RTLD_GLOBAL = 256
		}
	}
}
