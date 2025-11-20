using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Common;

public struct Destroyed : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public float m_Cleared;

	public Destroyed(Entity _event)
	{
		m_Event = _event;
		m_Cleared = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_Event;
		writer.Write(value);
		float cleared = m_Cleared;
		writer.Write(cleared);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		if (reader.context.version >= Version.destroyedCleared)
		{
			ref float cleared = ref m_Cleared;
			reader.Read(out cleared);
		}
	}
}
