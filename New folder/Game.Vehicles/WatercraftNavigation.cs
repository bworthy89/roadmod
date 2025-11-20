using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct WatercraftNavigation : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_TargetPosition;

	public float3 m_TargetDirection;

	public float m_MaxSpeed;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 targetPosition = m_TargetPosition;
		writer.Write(targetPosition);
		float3 targetDirection = m_TargetDirection;
		writer.Write(targetDirection);
		float maxSpeed = m_MaxSpeed;
		writer.Write(maxSpeed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 targetPosition = ref m_TargetPosition;
		reader.Read(out targetPosition);
		ref float3 targetDirection = ref m_TargetDirection;
		reader.Read(out targetDirection);
		ref float maxSpeed = ref m_MaxSpeed;
		reader.Read(out maxSpeed);
	}
}
