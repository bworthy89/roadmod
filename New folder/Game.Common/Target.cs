using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Common;

public struct Target : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public Target(Entity target)
	{
		m_Target = target;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Target);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Target);
	}
}
