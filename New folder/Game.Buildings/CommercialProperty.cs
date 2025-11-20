using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Buildings;

public struct CommercialProperty : IComponentData, IQueryTypeParameter, ISerializable
{
	public Resource m_Resources;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((uint)m_Resources);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		m_Resources = (Resource)value;
	}
}
