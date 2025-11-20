using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct WaterwayData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_SpeedLimit;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_SpeedLimit);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_SpeedLimit);
	}
}
