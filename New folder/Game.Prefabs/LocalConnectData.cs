using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct LocalConnectData : IComponentData, IQueryTypeParameter, ISerializable
{
	public LocalConnectFlags m_Flags;

	public Layer m_Layers;

	public Bounds1 m_HeightRange;

	public float m_SearchDistance;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		LocalConnectFlags flags = m_Flags;
		writer.Write((uint)flags);
		Layer layers = m_Layers;
		writer.Write((uint)layers);
		float min = m_HeightRange.min;
		writer.Write(min);
		float max = m_HeightRange.max;
		writer.Write(max);
		float searchDistance = m_SearchDistance;
		writer.Write(searchDistance);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		reader.Read(out uint value2);
		ref float min = ref m_HeightRange.min;
		reader.Read(out min);
		ref float max = ref m_HeightRange.max;
		reader.Read(out max);
		ref float searchDistance = ref m_SearchDistance;
		reader.Read(out searchDistance);
		m_Flags = (LocalConnectFlags)value;
		m_Layers = (Layer)value2;
	}
}
