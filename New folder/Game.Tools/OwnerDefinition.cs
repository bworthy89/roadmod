using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct OwnerDefinition : IComponentData, IQueryTypeParameter, IEquatable<OwnerDefinition>
{
	public Entity m_Prefab;

	public float3 m_Position;

	public quaternion m_Rotation;

	public bool Equals(OwnerDefinition other)
	{
		if (m_Prefab.Equals(other.m_Prefab) && m_Position.Equals(other.m_Position))
		{
			return m_Rotation.Equals(other.m_Rotation);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((17 * 31 + m_Prefab.GetHashCode()) * 31 + m_Position.GetHashCode()) * 31 + m_Rotation.GetHashCode();
	}
}
