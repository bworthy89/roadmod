using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Hospital : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public HospitalFlags m_Flags;

	public byte m_TreatmentBonus;

	public byte m_MinHealth;

	public byte m_MaxHealth;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		HospitalFlags flags = m_Flags;
		writer.Write((byte)flags);
		byte treatmentBonus = m_TreatmentBonus;
		writer.Write(treatmentBonus);
		byte minHealth = m_MinHealth;
		writer.Write(minHealth);
		byte maxHealth = m_MaxHealth;
		writer.Write(maxHealth);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out byte value);
		if (reader.context.version >= Version.healthcareImprovement)
		{
			ref byte treatmentBonus = ref m_TreatmentBonus;
			reader.Read(out treatmentBonus);
		}
		if (reader.context.version >= Version.healthcareImprovement2)
		{
			ref byte minHealth = ref m_MinHealth;
			reader.Read(out minHealth);
			ref byte maxHealth = ref m_MaxHealth;
			reader.Read(out maxHealth);
		}
		m_Flags = (HospitalFlags)value;
	}
}
