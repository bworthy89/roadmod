using Unity.Entities;
using Unity.Mathematics;

namespace Game.Effects;

public struct EnabledEffectData
{
	public Entity m_Owner;

	public Entity m_Prefab;

	public int m_EffectIndex;

	public EnabledEffectFlags m_Flags;

	public float3 m_Position;

	public float3 m_Scale;

	public quaternion m_Rotation;

	public float m_Intensity;

	public float m_NextTime;
}
