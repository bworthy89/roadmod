using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct SolarPoweredData : IComponentData, IQueryTypeParameter, ICombineData<SolarPoweredData>, ISerializable
{
	public int m_Production;

	public void Combine(SolarPoweredData otherData)
	{
		m_Production += otherData.m_Production;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Production);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Production);
	}
}
