using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Routes;

public struct TransportLine : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_VehicleRequest;

	public float m_VehicleInterval;

	public float m_UnbunchingFactor;

	public TransportLineFlags m_Flags;

	public ushort m_TicketPrice;

	public TransportLine(TransportLineData transportLineData)
	{
		m_VehicleRequest = Entity.Null;
		m_VehicleInterval = transportLineData.m_DefaultVehicleInterval;
		m_UnbunchingFactor = transportLineData.m_DefaultUnbunchingFactor;
		m_Flags = (TransportLineFlags)0;
		m_TicketPrice = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity vehicleRequest = m_VehicleRequest;
		writer.Write(vehicleRequest);
		float vehicleInterval = m_VehicleInterval;
		writer.Write(vehicleInterval);
		float unbunchingFactor = m_UnbunchingFactor;
		writer.Write(unbunchingFactor);
		TransportLineFlags flags = m_Flags;
		writer.Write((ushort)flags);
		ushort ticketPrice = m_TicketPrice;
		writer.Write(ticketPrice);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity vehicleRequest = ref m_VehicleRequest;
		reader.Read(out vehicleRequest);
		if (reader.context.version < Version.routeVehicleInterval)
		{
			reader.Read(out float _);
		}
		ref float vehicleInterval = ref m_VehicleInterval;
		reader.Read(out vehicleInterval);
		ref float unbunchingFactor = ref m_UnbunchingFactor;
		reader.Read(out unbunchingFactor);
		if (reader.context.version >= Version.transportLineFlags)
		{
			reader.Read(out ushort value2);
			m_Flags = (TransportLineFlags)value2;
		}
		if (reader.context.version >= Version.transportLinePolicies)
		{
			ref ushort ticketPrice = ref m_TicketPrice;
			reader.Read(out ticketPrice);
		}
	}
}
