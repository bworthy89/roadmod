using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct BoardingVehicle : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Vehicle;

	public Entity m_Testing;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity vehicle = m_Vehicle;
		writer.Write(vehicle);
		Entity testing = m_Testing;
		writer.Write(testing);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity vehicle = ref m_Vehicle;
		reader.Read(out vehicle);
		if (reader.context.version >= Version.boardingTest)
		{
			ref Entity testing = ref m_Testing;
			reader.Read(out testing);
		}
	}
}
