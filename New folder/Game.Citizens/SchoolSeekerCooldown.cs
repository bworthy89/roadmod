using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct SchoolSeekerCooldown : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_SimulationFrame;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_SimulationFrame);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_SimulationFrame);
	}
}
