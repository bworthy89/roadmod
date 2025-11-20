using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

public struct Domesticated : IComponentData, IQueryTypeParameter, ISerializable
{
	public DomesticatedFlags m_Flags;

	public ushort m_StateTime;

	public ushort m_LifeTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		DomesticatedFlags flags = m_Flags;
		writer.Write((uint)flags);
		ushort stateTime = m_StateTime;
		writer.Write(stateTime);
		ushort lifeTime = m_LifeTime;
		writer.Write(lifeTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		ref ushort stateTime = ref m_StateTime;
		reader.Read(out stateTime);
		ref ushort lifeTime = ref m_LifeTime;
		reader.Read(out lifeTime);
		m_Flags = (DomesticatedFlags)value;
	}
}
