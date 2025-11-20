using Colossal.Serialization.Entities;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

public struct HelicopterData : IComponentData, IQueryTypeParameter, ISerializable
{
	public HelicopterType m_HelicopterType;

	public float m_FlyingMaxSpeed;

	public float m_FlyingAcceleration;

	public float m_FlyingAngularAcceleration;

	public float m_AccelerationSwayFactor;

	public float m_VelocitySwayFactor;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		HelicopterType helicopterType = m_HelicopterType;
		writer.Write((byte)helicopterType);
		float flyingMaxSpeed = m_FlyingMaxSpeed;
		writer.Write(flyingMaxSpeed);
		float flyingAcceleration = m_FlyingAcceleration;
		writer.Write(flyingAcceleration);
		float flyingAngularAcceleration = m_FlyingAngularAcceleration;
		writer.Write(flyingAngularAcceleration);
		float accelerationSwayFactor = m_AccelerationSwayFactor;
		writer.Write(accelerationSwayFactor);
		float velocitySwayFactor = m_VelocitySwayFactor;
		writer.Write(velocitySwayFactor);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		ref float flyingMaxSpeed = ref m_FlyingMaxSpeed;
		reader.Read(out flyingMaxSpeed);
		ref float flyingAcceleration = ref m_FlyingAcceleration;
		reader.Read(out flyingAcceleration);
		ref float flyingAngularAcceleration = ref m_FlyingAngularAcceleration;
		reader.Read(out flyingAngularAcceleration);
		ref float accelerationSwayFactor = ref m_AccelerationSwayFactor;
		reader.Read(out accelerationSwayFactor);
		ref float velocitySwayFactor = ref m_VelocitySwayFactor;
		reader.Read(out velocitySwayFactor);
		m_HelicopterType = (HelicopterType)value;
	}
}
