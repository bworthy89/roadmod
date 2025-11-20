using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct MoveableBridgeData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_LiftOffsets;

	public float m_MovingTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 liftOffsets = m_LiftOffsets;
		writer.Write(liftOffsets);
		float movingTime = m_MovingTime;
		writer.Write(movingTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 liftOffsets = ref m_LiftOffsets;
		reader.Read(out liftOffsets);
		ref float movingTime = ref m_MovingTime;
		reader.Read(out movingTime);
	}
}
