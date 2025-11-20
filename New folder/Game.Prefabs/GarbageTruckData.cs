using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct GarbageTruckData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_GarbageCapacity;

	public int m_UnloadRate;

	public GarbageTruckData(int garbageCapacity, int unloadRate)
	{
		m_GarbageCapacity = garbageCapacity;
		m_UnloadRate = unloadRate;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int garbageCapacity = m_GarbageCapacity;
		writer.Write(garbageCapacity);
		int unloadRate = m_UnloadRate;
		writer.Write(unloadRate);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int garbageCapacity = ref m_GarbageCapacity;
		reader.Read(out garbageCapacity);
		ref int unloadRate = ref m_UnloadRate;
		reader.Read(out unloadRate);
	}
}
