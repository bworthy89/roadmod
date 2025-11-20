using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct EarlyDisasterWarningDuration : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_EndFrame;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_EndFrame);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_EndFrame);
	}
}
