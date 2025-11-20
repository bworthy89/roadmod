using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct CoverageData : IComponentData, IQueryTypeParameter, ISerializable
{
	public CoverageService m_Service;

	public float m_Range;

	public float m_Capacity;

	public float m_Magnitude;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float range = m_Range;
		writer.Write(range);
		float capacity = m_Capacity;
		writer.Write(capacity);
		float magnitude = m_Magnitude;
		writer.Write(magnitude);
		CoverageService service = m_Service;
		writer.Write((byte)service);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float range = ref m_Range;
		reader.Read(out range);
		ref float capacity = ref m_Capacity;
		reader.Read(out capacity);
		ref float magnitude = ref m_Magnitude;
		reader.Read(out magnitude);
		reader.Read(out byte value);
		m_Service = (CoverageService)value;
	}
}
