using Colossal.Serialization.Entities;
using Game.Objects;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Creatures;

public struct AnimalNavigation : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_TargetPosition;

	public float3 m_TargetDirection;

	public float m_MaxSpeed;

	public TransformState m_TransformState;

	public byte m_LastActivity;

	public byte m_TargetActivity;

	public AnimalNavigation(float3 position)
	{
		m_TargetPosition = position;
		m_TargetDirection = default(float3);
		m_MaxSpeed = 0f;
		m_TransformState = TransformState.Default;
		m_LastActivity = 0;
		m_TargetActivity = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 targetPosition = m_TargetPosition;
		writer.Write(targetPosition);
		float3 targetDirection = m_TargetDirection;
		writer.Write(targetDirection);
		byte targetActivity = m_TargetActivity;
		writer.Write(targetActivity);
		TransformState transformState = m_TransformState;
		writer.Write((byte)transformState);
		byte lastActivity = m_LastActivity;
		writer.Write(lastActivity);
		float maxSpeed = m_MaxSpeed;
		writer.Write(maxSpeed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 targetPosition = ref m_TargetPosition;
		reader.Read(out targetPosition);
		if (reader.context.version >= Version.creatureTargetDirection)
		{
			ref float3 targetDirection = ref m_TargetDirection;
			reader.Read(out targetDirection);
		}
		if (reader.context.version >= Version.creatureTargetDirection2)
		{
			ref byte targetActivity = ref m_TargetActivity;
			reader.Read(out targetActivity);
		}
		if (reader.context.version >= Version.animationStateFix)
		{
			reader.Read(out byte value);
			ref byte lastActivity = ref m_LastActivity;
			reader.Read(out lastActivity);
			m_TransformState = (TransformState)value;
		}
		ref float maxSpeed = ref m_MaxSpeed;
		reader.Read(out maxSpeed);
	}
}
