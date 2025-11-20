using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct CarTrailerData : IComponentData, IQueryTypeParameter, ISerializable
{
	public CarTrailerType m_TrailerType;

	public TrailerMovementType m_MovementType;

	public float3 m_AttachPosition;

	public Entity m_FixedTractor;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte value = (byte)m_MovementType;
		writer.Write(value);
		float3 attachPosition = m_AttachPosition;
		writer.Write(attachPosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		ref float3 attachPosition = ref m_AttachPosition;
		reader.Read(out attachPosition);
		m_MovementType = (TrailerMovementType)value;
	}
}
