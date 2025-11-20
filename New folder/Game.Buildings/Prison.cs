using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Prison : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public PrisonFlags m_Flags;

	public sbyte m_PrisonerWellbeing;

	public sbyte m_PrisonerHealth;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		PrisonFlags flags = m_Flags;
		writer.Write((byte)flags);
		sbyte prisonerWellbeing = m_PrisonerWellbeing;
		writer.Write(prisonerWellbeing);
		sbyte prisonerHealth = m_PrisonerHealth;
		writer.Write(prisonerHealth);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out byte value);
		m_Flags = (PrisonFlags)value;
		if (reader.context.version >= Version.happinessAdjustRefactoring)
		{
			ref sbyte prisonerWellbeing = ref m_PrisonerWellbeing;
			reader.Read(out prisonerWellbeing);
			ref sbyte prisonerHealth = ref m_PrisonerHealth;
			reader.Read(out prisonerHealth);
		}
	}
}
