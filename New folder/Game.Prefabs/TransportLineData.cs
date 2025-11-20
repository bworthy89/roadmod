using Colossal.Serialization.Entities;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

public struct TransportLineData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_PathfindPrefab;

	public TransportType m_TransportType;

	public float m_DefaultVehicleInterval;

	public float m_DefaultUnbunchingFactor;

	public float m_StopDuration;

	public SizeClass m_SizeClass;

	public bool m_PassengerTransport;

	public bool m_CargoTransport;

	public Entity m_VehicleNotification;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity pathfindPrefab = m_PathfindPrefab;
		writer.Write(pathfindPrefab);
		sbyte value = (sbyte)m_TransportType;
		writer.Write(value);
		float defaultVehicleInterval = m_DefaultVehicleInterval;
		writer.Write(defaultVehicleInterval);
		float defaultUnbunchingFactor = m_DefaultUnbunchingFactor;
		writer.Write(defaultUnbunchingFactor);
		float stopDuration = m_StopDuration;
		writer.Write(stopDuration);
		bool passengerTransport = m_PassengerTransport;
		writer.Write(passengerTransport);
		bool cargoTransport = m_CargoTransport;
		writer.Write(cargoTransport);
		SizeClass sizeClass = m_SizeClass;
		writer.Write((byte)sizeClass);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity pathfindPrefab = ref m_PathfindPrefab;
		reader.Read(out pathfindPrefab);
		reader.Read(out sbyte value);
		ref float defaultVehicleInterval = ref m_DefaultVehicleInterval;
		reader.Read(out defaultVehicleInterval);
		ref float defaultUnbunchingFactor = ref m_DefaultUnbunchingFactor;
		reader.Read(out defaultUnbunchingFactor);
		ref float stopDuration = ref m_StopDuration;
		reader.Read(out stopDuration);
		ref bool passengerTransport = ref m_PassengerTransport;
		reader.Read(out passengerTransport);
		ref bool cargoTransport = ref m_CargoTransport;
		reader.Read(out cargoTransport);
		m_TransportType = (TransportType)value;
		if (reader.context.format.Has(FormatTags.BpPrefabData))
		{
			reader.Read(out byte value2);
			m_SizeClass = (SizeClass)value2;
		}
		else
		{
			m_SizeClass = SizeClass.Large;
		}
	}
}
