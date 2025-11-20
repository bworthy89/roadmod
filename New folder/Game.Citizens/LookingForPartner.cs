using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct LookingForPartner : IBufferElementData, ISerializable
{
	public Entity m_Citizen;

	public PartnerType m_PartnerType;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity citizen = ref m_Citizen;
		reader.Read(out citizen);
		reader.Read(out int value);
		m_PartnerType = (PartnerType)value;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity citizen = m_Citizen;
		writer.Write(citizen);
		PartnerType partnerType = m_PartnerType;
		writer.Write((int)partnerType);
	}
}
