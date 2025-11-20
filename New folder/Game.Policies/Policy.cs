using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Policies;

[InternalBufferCapacity(0)]
public struct Policy : IBufferElementData, ISerializable
{
	public Entity m_Policy;

	public PolicyFlags m_Flags;

	public float m_Adjustment;

	public Policy(Entity policy, PolicyFlags flags, float adjustment)
	{
		m_Policy = policy;
		m_Flags = flags;
		m_Adjustment = adjustment;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity policy = m_Policy;
		writer.Write(policy);
		PolicyFlags flags = m_Flags;
		writer.Write((byte)flags);
		float adjustment = m_Adjustment;
		writer.Write(adjustment);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity policy = ref m_Policy;
		reader.Read(out policy);
		reader.Read(out byte value);
		ref float adjustment = ref m_Adjustment;
		reader.Read(out adjustment);
		m_Flags = (PolicyFlags)value;
	}
}
