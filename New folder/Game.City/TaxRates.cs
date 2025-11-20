using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct TaxRates : IBufferElementData, ISerializable
{
	public int m_TaxRate;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_TaxRate);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_TaxRate);
	}
}
