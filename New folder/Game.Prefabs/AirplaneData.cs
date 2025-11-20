using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct AirplaneData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float2 m_FlyingSpeed;

	public float m_FlyingAcceleration;

	public float m_FlyingBraking;

	public float m_FlyingTurning;

	public float m_FlyingAngularAcceleration;

	public float m_ClimbAngle;

	public float m_SlowPitchAngle;

	public float m_TurningRollFactor;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float2 flyingSpeed = m_FlyingSpeed;
		writer.Write(flyingSpeed);
		float flyingAcceleration = m_FlyingAcceleration;
		writer.Write(flyingAcceleration);
		float flyingBraking = m_FlyingBraking;
		writer.Write(flyingBraking);
		float flyingTurning = m_FlyingTurning;
		writer.Write(flyingTurning);
		float flyingAngularAcceleration = m_FlyingAngularAcceleration;
		writer.Write(flyingAngularAcceleration);
		float climbAngle = m_ClimbAngle;
		writer.Write(climbAngle);
		float slowPitchAngle = m_SlowPitchAngle;
		writer.Write(slowPitchAngle);
		float turningRollFactor = m_TurningRollFactor;
		writer.Write(turningRollFactor);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float2 flyingSpeed = ref m_FlyingSpeed;
		reader.Read(out flyingSpeed);
		ref float flyingAcceleration = ref m_FlyingAcceleration;
		reader.Read(out flyingAcceleration);
		ref float flyingBraking = ref m_FlyingBraking;
		reader.Read(out flyingBraking);
		ref float flyingTurning = ref m_FlyingTurning;
		reader.Read(out flyingTurning);
		ref float flyingAngularAcceleration = ref m_FlyingAngularAcceleration;
		reader.Read(out flyingAngularAcceleration);
		ref float climbAngle = ref m_ClimbAngle;
		reader.Read(out climbAngle);
		ref float slowPitchAngle = ref m_SlowPitchAngle;
		reader.Read(out slowPitchAngle);
		ref float turningRollFactor = ref m_TurningRollFactor;
		reader.Read(out turningRollFactor);
	}
}
