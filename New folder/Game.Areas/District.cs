using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

public struct District : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_OptionMask;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_OptionMask);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_OptionMask);
	}
}
