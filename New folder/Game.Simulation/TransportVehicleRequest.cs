using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct TransportVehicleRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Route;

	public float m_Priority;

	public TransportVehicleRequest(Entity route, float priority)
	{
		m_Route = route;
		m_Priority = priority;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity route = m_Route;
		writer.Write(route);
		float priority = m_Priority;
		writer.Write(priority);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity route = ref m_Route;
		reader.Read(out route);
		ref float priority = ref m_Priority;
		reader.Read(out priority);
	}
}
