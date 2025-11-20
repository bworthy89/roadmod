using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct TransportStopData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_ComfortFactor;

	public float m_LoadingFactor;

	public float m_AccessDistance;

	public float m_BoardingTime;

	public TransportType m_TransportType;

	public bool m_PassengerTransport;

	public bool m_CargoTransport;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)m_TransportType;
		writer.Write(value);
		bool passengerTransport = m_PassengerTransport;
		writer.Write(passengerTransport);
		bool cargoTransport = m_CargoTransport;
		writer.Write(cargoTransport);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		ref bool passengerTransport = ref m_PassengerTransport;
		reader.Read(out passengerTransport);
		ref bool cargoTransport = ref m_CargoTransport;
		reader.Read(out cargoTransport);
		m_TransportType = (TransportType)value;
	}
}
