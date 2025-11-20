using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Agents;

public struct MovingAway : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public MoveAwayReason m_Reason;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		MoveAwayReason reason = m_Reason;
		writer.Write((int)reason);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		if (reader.context.format.Has(FormatTags.MovingAwayReason))
		{
			reader.Read(out int value);
			m_Reason = (MoveAwayReason)value;
		}
	}
}
