using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct BuildingEfficiency : IComponentData, IQueryTypeParameter, ISerializable
{
	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)0);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte _);
	}
}
