using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PrisonData : IComponentData, IQueryTypeParameter, ICombineData<PrisonData>, ISerializable
{
	public int m_PrisonVanCapacity;

	public int m_PrisonerCapacity;

	public sbyte m_PrisonerWellbeing;

	public sbyte m_PrisonerHealth;

	public void Combine(PrisonData otherData)
	{
		m_PrisonVanCapacity += otherData.m_PrisonVanCapacity;
		m_PrisonerCapacity += otherData.m_PrisonerCapacity;
		m_PrisonerWellbeing += otherData.m_PrisonerWellbeing;
		m_PrisonerHealth += otherData.m_PrisonerHealth;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int prisonVanCapacity = m_PrisonVanCapacity;
		writer.Write(prisonVanCapacity);
		int prisonerCapacity = m_PrisonerCapacity;
		writer.Write(prisonerCapacity);
		sbyte prisonerWellbeing = m_PrisonerWellbeing;
		writer.Write(prisonerWellbeing);
		sbyte prisonerHealth = m_PrisonerHealth;
		writer.Write(prisonerHealth);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int prisonVanCapacity = ref m_PrisonVanCapacity;
		reader.Read(out prisonVanCapacity);
		ref int prisonerCapacity = ref m_PrisonerCapacity;
		reader.Read(out prisonerCapacity);
		if (reader.context.version >= Version.happinessAdjustRefactoring)
		{
			ref sbyte prisonerWellbeing = ref m_PrisonerWellbeing;
			reader.Read(out prisonerWellbeing);
			ref sbyte prisonerHealth = ref m_PrisonerHealth;
			reader.Read(out prisonerHealth);
		}
	}
}
