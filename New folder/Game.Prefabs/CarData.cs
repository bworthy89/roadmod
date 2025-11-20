using Colossal.Serialization.Entities;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct CarData : IComponentData, IQueryTypeParameter, ISerializable
{
	public SizeClass m_SizeClass;

	public EnergyTypes m_EnergyType;

	public float m_MaxSpeed;

	public float m_Acceleration;

	public float m_Braking;

	public float m_PivotOffset;

	public float2 m_Turning;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		SizeClass sizeClass = m_SizeClass;
		writer.Write((byte)sizeClass);
		EnergyTypes energyType = m_EnergyType;
		writer.Write((byte)energyType);
		float maxSpeed = m_MaxSpeed;
		writer.Write(maxSpeed);
		float acceleration = m_Acceleration;
		writer.Write(acceleration);
		float braking = m_Braking;
		writer.Write(braking);
		float pivotOffset = m_PivotOffset;
		writer.Write(pivotOffset);
		float2 turning = m_Turning;
		writer.Write(turning);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		reader.Read(out byte value2);
		ref float maxSpeed = ref m_MaxSpeed;
		reader.Read(out maxSpeed);
		ref float acceleration = ref m_Acceleration;
		reader.Read(out acceleration);
		ref float braking = ref m_Braking;
		reader.Read(out braking);
		ref float pivotOffset = ref m_PivotOffset;
		reader.Read(out pivotOffset);
		ref float2 turning = ref m_Turning;
		reader.Read(out turning);
		m_SizeClass = (SizeClass)value;
		m_EnergyType = (EnergyTypes)value2;
	}
}
