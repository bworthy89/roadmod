using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Triggers;

public struct ChirpLink : IBufferElementData, ISerializable
{
	public Entity m_Chirp;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Chirp);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Chirp);
	}
}
