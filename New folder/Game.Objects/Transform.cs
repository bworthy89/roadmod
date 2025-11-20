using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Objects;

public struct Transform : IComponentData, IQueryTypeParameter, IEquatable<Transform>, ISerializable
{
	public float3 m_Position;

	public quaternion m_Rotation;

	public Transform(float3 position, quaternion rotation)
	{
		m_Position = position;
		m_Rotation = rotation;
	}

	public bool Equals(Transform other)
	{
		if (m_Position.Equals(other.m_Position))
		{
			return m_Rotation.Equals(other.m_Rotation);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (17 * 31 + m_Position.GetHashCode()) * 31 + m_Rotation.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 position = m_Position;
		writer.Write(position);
		quaternion rotation = m_Rotation;
		writer.Write(rotation);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 position = ref m_Position;
		reader.Read(out position);
		ref quaternion rotation = ref m_Rotation;
		reader.Read(out rotation);
		if (!math.all(m_Position >= -100000f) || !math.all(m_Position <= 100000f) || !math.all(math.isfinite(m_Rotation.value)) || math.all(m_Rotation.value == 0f))
		{
			m_Position = default(float3);
			m_Rotation = quaternion.identity;
		}
	}
}
