using Colossal.Serialization.Entities;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct AircraftData : IComponentData, IQueryTypeParameter, ISerializable
{
	public SizeClass m_SizeClass;

	public float m_GroundMaxSpeed;

	public float m_GroundAcceleration;

	public float m_GroundBraking;

	public float2 m_GroundTurning;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		SizeClass sizeClass = m_SizeClass;
		writer.Write((byte)sizeClass);
		float groundMaxSpeed = m_GroundMaxSpeed;
		writer.Write(groundMaxSpeed);
		float groundAcceleration = m_GroundAcceleration;
		writer.Write(groundAcceleration);
		float groundBraking = m_GroundBraking;
		writer.Write(groundBraking);
		float2 groundTurning = m_GroundTurning;
		writer.Write(groundTurning);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		ref float groundMaxSpeed = ref m_GroundMaxSpeed;
		reader.Read(out groundMaxSpeed);
		ref float groundAcceleration = ref m_GroundAcceleration;
		reader.Read(out groundAcceleration);
		ref float groundBraking = ref m_GroundBraking;
		reader.Read(out groundBraking);
		ref float2 groundTurning = ref m_GroundTurning;
		reader.Read(out groundTurning);
		m_SizeClass = (SizeClass)value;
	}
}
