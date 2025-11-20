using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Objects;

public struct Moving : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_Velocity;

	public float3 m_AngularVelocity;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 velocity = m_Velocity;
		writer.Write(velocity);
		float3 angularVelocity = m_AngularVelocity;
		writer.Write(angularVelocity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 velocity = ref m_Velocity;
		reader.Read(out velocity);
		ref float3 angularVelocity = ref m_AngularVelocity;
		reader.Read(out angularVelocity);
		if (!math.all(math.isfinite(m_Velocity)) || !math.all(math.isfinite(m_AngularVelocity)))
		{
			m_Velocity = default(float3);
			m_AngularVelocity = default(float3);
		}
	}
}
