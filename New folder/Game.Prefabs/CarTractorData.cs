using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct CarTractorData : IComponentData, IQueryTypeParameter, ISerializable
{
	public CarTrailerType m_TrailerType;

	public float3 m_AttachPosition;

	public Entity m_FixedTrailer;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_AttachPosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_AttachPosition);
	}
}
