using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Objects;

[InternalBufferCapacity(4)]
public struct TransformFrame : IBufferElementData, ISerializable
{
	public float3 m_Position;

	public float3 m_Velocity;

	public quaternion m_Rotation;

	public TransformFlags m_Flags;

	public ushort m_StateTimer;

	public TransformState m_State;

	public byte m_Activity;

	public TransformFrame(Transform transform)
	{
		m_Position = transform.m_Position;
		m_Velocity = default(float3);
		m_Rotation = transform.m_Rotation;
		m_Flags = (TransformFlags)0u;
		m_StateTimer = 0;
		m_State = TransformState.Default;
		m_Activity = 0;
	}

	public TransformFrame(Transform transform, Moving moving)
	{
		m_Position = transform.m_Position;
		m_Velocity = moving.m_Velocity;
		m_Rotation = transform.m_Rotation;
		m_Flags = (TransformFlags)0u;
		m_StateTimer = 0;
		m_State = TransformState.Default;
		m_Activity = 0;
	}

	public TransformFrame(float3 position, quaternion rotation, float3 velocity)
	{
		m_Position = position;
		m_Velocity = velocity;
		m_Rotation = rotation;
		m_Flags = (TransformFlags)0u;
		m_StateTimer = 0;
		m_State = TransformState.Default;
		m_Activity = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		ushort stateTimer = m_StateTimer;
		writer.Write(stateTimer);
		TransformState state = m_State;
		writer.Write((byte)state);
		byte activity = m_Activity;
		writer.Write(activity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref ushort stateTimer = ref m_StateTimer;
		reader.Read(out stateTimer);
		reader.Read(out byte value);
		ref byte activity = ref m_Activity;
		reader.Read(out activity);
		m_State = (TransformState)value;
	}
}
