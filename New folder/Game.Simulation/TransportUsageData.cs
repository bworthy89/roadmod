using Colossal.Serialization.Entities;

namespace Game.Simulation;

public struct TransportUsageData : ISerializable
{
	public long m_TrainTransportCargo;

	public long m_TrainTransportPassenger;

	public long m_ShipTransportCargo;

	public long m_ShipTransportPassenger;

	public long m_AirplaneTransportCargo;

	public long m_AirplaneTransportPassenger;

	public long GetTotalPassenger()
	{
		return m_ShipTransportPassenger + m_AirplaneTransportPassenger + m_TrainTransportPassenger;
	}

	public long GetTotalCargo()
	{
		return m_ShipTransportCargo + m_AirplaneTransportCargo + m_TrainTransportCargo;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		long trainTransportCargo = m_TrainTransportCargo;
		writer.Write(trainTransportCargo);
		long trainTransportPassenger = m_TrainTransportPassenger;
		writer.Write(trainTransportPassenger);
		long shipTransportCargo = m_ShipTransportCargo;
		writer.Write(shipTransportCargo);
		long shipTransportPassenger = m_ShipTransportPassenger;
		writer.Write(shipTransportPassenger);
		long airplaneTransportCargo = m_AirplaneTransportCargo;
		writer.Write(airplaneTransportCargo);
		long airplaneTransportPassenger = m_AirplaneTransportPassenger;
		writer.Write(airplaneTransportPassenger);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref long trainTransportCargo = ref m_TrainTransportCargo;
		reader.Read(out trainTransportCargo);
		ref long trainTransportPassenger = ref m_TrainTransportPassenger;
		reader.Read(out trainTransportPassenger);
		ref long shipTransportCargo = ref m_ShipTransportCargo;
		reader.Read(out shipTransportCargo);
		ref long shipTransportPassenger = ref m_ShipTransportPassenger;
		reader.Read(out shipTransportPassenger);
		ref long airplaneTransportCargo = ref m_AirplaneTransportCargo;
		reader.Read(out airplaneTransportCargo);
		ref long airplaneTransportPassenger = ref m_AirplaneTransportPassenger;
		reader.Read(out airplaneTransportPassenger);
	}
}
