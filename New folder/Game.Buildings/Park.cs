using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Park : IComponentData, IQueryTypeParameter, ISerializable
{
	public short m_Maintenance;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Maintenance);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Maintenance);
	}
}
