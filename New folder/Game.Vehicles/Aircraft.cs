using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct Aircraft : IComponentData, IQueryTypeParameter, ISerializable
{
	public AircraftFlags m_Flags;

	public Aircraft(AircraftFlags flags)
	{
		m_Flags = flags;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((uint)m_Flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		m_Flags = (AircraftFlags)value;
	}
}
