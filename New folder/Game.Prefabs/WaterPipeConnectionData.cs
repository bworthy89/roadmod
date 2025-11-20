using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct WaterPipeConnectionData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_FreshCapacity;

	public int m_SewageCapacity;

	public int m_StormCapacity;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int freshCapacity = m_FreshCapacity;
		writer.Write(freshCapacity);
		int sewageCapacity = m_SewageCapacity;
		writer.Write(sewageCapacity);
		int stormCapacity = m_StormCapacity;
		writer.Write(stormCapacity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int freshCapacity = ref m_FreshCapacity;
		reader.Read(out freshCapacity);
		ref int sewageCapacity = ref m_SewageCapacity;
		reader.Read(out sewageCapacity);
		ref int stormCapacity = ref m_StormCapacity;
		reader.Read(out stormCapacity);
	}
}
