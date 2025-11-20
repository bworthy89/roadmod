using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct BuildingCondition : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Condition;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Condition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Condition);
	}
}
