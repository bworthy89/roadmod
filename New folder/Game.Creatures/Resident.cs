using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

public struct Resident : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Citizen;

	public ResidentFlags m_Flags;

	public int m_Timer;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		ResidentFlags flags = m_Flags;
		writer.Write((uint)flags);
		int timer = m_Timer;
		writer.Write(timer);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		if (reader.context.version >= Version.transportWaitTimer)
		{
			ref int timer = ref m_Timer;
			reader.Read(out timer);
		}
		m_Flags = (ResidentFlags)value;
		if (reader.context.version < Version.yogaAreaFix)
		{
			m_Flags &= ~ResidentFlags.IgnoreAreas;
		}
	}
}
