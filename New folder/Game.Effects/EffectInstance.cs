using Unity.Entities;
using Unity.Mathematics;

namespace Game.Effects;

public struct EffectInstance : IComponentData, IQueryTypeParameter
{
	public float3 m_Position;

	public quaternion m_Rotation;

	public float m_Intensity;
}
