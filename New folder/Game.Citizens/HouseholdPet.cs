using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct HouseholdPet : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Household;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Household);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Household);
	}
}
