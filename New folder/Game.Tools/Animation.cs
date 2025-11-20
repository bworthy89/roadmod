using Game.Objects;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct Animation : IComponentData, IQueryTypeParameter
{
	public float3 m_TargetPosition;

	public float3 m_Position;

	public quaternion m_Rotation;

	public float3 m_SwayPivot;

	public float3 m_SwayPosition;

	public float3 m_SwayVelocity;

	public float m_PushFactor;

	public Transform ToTransform()
	{
		return new Transform(m_Position, m_Rotation);
	}
}
