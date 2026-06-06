using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace HarmonyLib
{
	internal static class PatchInfoSerialization
	{
		internal static byte[] Serialize(this PatchInfo patchInfo)
		{
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				PatchInfoSerialization.binaryFormatter.Serialize(memoryStream, patchInfo);
				result = memoryStream.ToArray();
			}
			return result;
		}

		internal static PatchInfo Deserialize(byte[] bytes)
		{
			PatchInfo result;
			using (MemoryStream memoryStream = new MemoryStream(bytes))
			{
				result = (PatchInfo)PatchInfoSerialization.binaryFormatter.Deserialize(memoryStream);
			}
			return result;
		}

		internal static int PriorityComparer(object obj, int index, int priority)
		{
			Traverse traverse = Traverse.Create(obj);
			int value = traverse.Field("priority").GetValue<int>();
			int value2 = traverse.Field("index").GetValue<int>();
			if (priority != value)
			{
				return -priority.CompareTo(value);
			}
			return index.CompareTo(value2);
		}

		internal static readonly BinaryFormatter binaryFormatter = new BinaryFormatter
		{
			Binder = new PatchInfoSerialization.Binder()
		};

		private class Binder : SerializationBinder
		{
			public override Type BindToType(string assemblyName, string typeName)
			{
				Type[] array = new Type[]
				{
					typeof(PatchInfo),
					typeof(Patch[]),
					typeof(Patch)
				};
				foreach (Type type in array)
				{
					if (typeName == type.FullName)
					{
						return type;
					}
				}
				return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName));
			}
		}
	}
}
