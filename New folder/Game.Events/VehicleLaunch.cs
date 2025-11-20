using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct VehicleLaunch : IComponentData, IQueryTypeParameter, ISerializable
{
	public VehicleLaunchFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((uint)m_Flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		m_Flags = (VehicleLaunchFlags)value;
	}
}
