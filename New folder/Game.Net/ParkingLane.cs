using Colossal.Serialization.Entities;
using Game.Pathfind;
using Unity.Entities;

namespace Game.Net;

public struct ParkingLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_AccessRestriction;

	public PathNode m_AdditionalStartNode;

	public ParkingLaneFlags m_Flags;

	public float m_FreeSpace;

	public ushort m_ParkingFee;

	public ushort m_ComfortFactor;

	public ushort m_TaxiAvailability;

	public ushort m_TaxiFee;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity accessRestriction = m_AccessRestriction;
		writer.Write(accessRestriction);
		PathNode additionalStartNode = m_AdditionalStartNode;
		writer.Write(additionalStartNode);
		ParkingLaneFlags flags = m_Flags;
		writer.Write((uint)flags);
		float freeSpace = m_FreeSpace;
		writer.Write(freeSpace);
		ushort parkingFee = m_ParkingFee;
		writer.Write(parkingFee);
		ushort comfortFactor = m_ComfortFactor;
		writer.Write(comfortFactor);
		ushort taxiAvailability = m_TaxiAvailability;
		writer.Write(taxiAvailability);
		ushort taxiFee = m_TaxiFee;
		writer.Write(taxiFee);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.pathfindAccessRestriction)
		{
			ref Entity accessRestriction = ref m_AccessRestriction;
			reader.Read(out accessRestriction);
		}
		if (reader.context.version >= Version.parkingLaneImprovement)
		{
			ref PathNode additionalStartNode = ref m_AdditionalStartNode;
			reader.Read(out additionalStartNode);
		}
		reader.Read(out uint value);
		ref float freeSpace = ref m_FreeSpace;
		reader.Read(out freeSpace);
		if (reader.context.version >= Version.parkingLaneImprovement2)
		{
			ref ushort parkingFee = ref m_ParkingFee;
			reader.Read(out parkingFee);
			ref ushort comfortFactor = ref m_ComfortFactor;
			reader.Read(out comfortFactor);
		}
		if (reader.context.version >= Version.taxiDispatchCenter)
		{
			ref ushort taxiAvailability = ref m_TaxiAvailability;
			reader.Read(out taxiAvailability);
		}
		if (reader.context.version >= Version.taxiFee)
		{
			ref ushort taxiFee = ref m_TaxiFee;
			reader.Read(out taxiFee);
		}
		m_Flags = (ParkingLaneFlags)value;
	}
}
