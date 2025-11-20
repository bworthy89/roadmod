using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct GarageLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public ushort m_ParkingFee;

	public ushort m_ComfortFactor;

	public ushort m_VehicleCount;

	public ushort m_VehicleCapacity;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		ushort parkingFee = m_ParkingFee;
		writer.Write(parkingFee);
		ushort comfortFactor = m_ComfortFactor;
		writer.Write(comfortFactor);
		ushort vehicleCount = m_VehicleCount;
		writer.Write(vehicleCount);
		ushort vehicleCapacity = m_VehicleCapacity;
		writer.Write(vehicleCapacity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref ushort parkingFee = ref m_ParkingFee;
		reader.Read(out parkingFee);
		ref ushort comfortFactor = ref m_ComfortFactor;
		reader.Read(out comfortFactor);
		ref ushort vehicleCount = ref m_VehicleCount;
		reader.Read(out vehicleCount);
		ref ushort vehicleCapacity = ref m_VehicleCapacity;
		reader.Read(out vehicleCapacity);
	}
}
