using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct HomelessHousehold : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TempHome;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_TempHome);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_TempHome);
	}
}
