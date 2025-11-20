using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

public struct CurrentVehicle : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Vehicle;

	public CreatureVehicleFlags m_Flags;

	public CurrentVehicle(Entity vehicle, CreatureVehicleFlags flags)
	{
		m_Vehicle = vehicle;
		m_Flags = flags;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity vehicle = m_Vehicle;
		writer.Write(vehicle);
		CreatureVehicleFlags flags = m_Flags;
		writer.Write((uint)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity vehicle = ref m_Vehicle;
		reader.Read(out vehicle);
		reader.Read(out uint value);
		m_Flags = (CreatureVehicleFlags)value;
	}
}
