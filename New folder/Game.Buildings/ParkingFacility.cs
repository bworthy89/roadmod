using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct ParkingFacility : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_ComfortFactor;

	public ParkingFacilityFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float comfortFactor = m_ComfortFactor;
		writer.Write(comfortFactor);
		ParkingFacilityFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float comfortFactor = ref m_ComfortFactor;
		reader.Read(out comfortFactor);
		reader.Read(out byte value);
		m_Flags = (ParkingFacilityFlags)value;
	}
}
