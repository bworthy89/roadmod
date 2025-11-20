using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct TakeoffLocation : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_AccessRestriction;

	public TakeoffLocationFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity accessRestriction = m_AccessRestriction;
		writer.Write(accessRestriction);
		TakeoffLocationFlags flags = m_Flags;
		writer.Write((uint)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity accessRestriction = ref m_AccessRestriction;
		reader.Read(out accessRestriction);
		if (reader.context.version >= Version.pathfindRestrictions)
		{
			reader.Read(out uint value);
			m_Flags = (TakeoffLocationFlags)value;
		}
	}
}
