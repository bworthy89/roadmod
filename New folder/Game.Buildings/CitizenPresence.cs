using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct CitizenPresence : IComponentData, IQueryTypeParameter, ISerializable
{
	public sbyte m_Delta;

	public byte m_Presence;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte delta = m_Delta;
		writer.Write(delta);
		byte presence = m_Presence;
		writer.Write(presence);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref sbyte delta = ref m_Delta;
		reader.Read(out delta);
		ref byte presence = ref m_Presence;
		reader.Read(out presence);
	}
}
