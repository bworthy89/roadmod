using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct BuildOrder : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_Start;

	public uint m_End;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint start = m_Start;
		writer.Write(start);
		uint end = m_End;
		writer.Write(end);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint start = ref m_Start;
		reader.Read(out start);
		ref uint end = ref m_End;
		reader.Read(out end);
	}
}
