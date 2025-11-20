using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Buildings;

public struct ModifiedServiceCoverage : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Range;

	public float m_Capacity;

	public float m_Magnitude;

	public void ReplaceData(ref CoverageData coverage)
	{
		coverage.m_Capacity = m_Capacity;
		coverage.m_Range = m_Range;
		coverage.m_Magnitude = m_Magnitude;
	}

	public ModifiedServiceCoverage(CoverageData coverage)
	{
		m_Capacity = coverage.m_Capacity;
		m_Range = coverage.m_Range;
		m_Magnitude = coverage.m_Magnitude;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float capacity = m_Capacity;
		writer.Write(capacity);
		float range = m_Range;
		writer.Write(range);
		float magnitude = m_Magnitude;
		writer.Write(magnitude);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out float value);
		m_Capacity = value;
		reader.Read(out value);
		m_Range = value;
		reader.Read(out value);
		m_Magnitude = value;
	}
}
