using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct StreetLight : IComponentData, IQueryTypeParameter, ISerializable
{
	public StreetLightState m_State;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_State);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_State = (StreetLightState)value;
	}
}
