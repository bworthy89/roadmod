using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

public struct CurrentDistrict : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_District;

	public CurrentDistrict(Entity district)
	{
		m_District = district;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_District);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_District);
	}
}
