using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct BuildingExtensionData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_Position;

	public int2 m_LotSize;

	public bool m_External;

	public bool m_HasUndergroundElements;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 position = m_Position;
		writer.Write(position);
		int2 lotSize = m_LotSize;
		writer.Write(lotSize);
		bool external = m_External;
		writer.Write(external);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 position = ref m_Position;
		reader.Read(out position);
		ref int2 lotSize = ref m_LotSize;
		reader.Read(out lotSize);
		ref bool external = ref m_External;
		reader.Read(out external);
	}
}
