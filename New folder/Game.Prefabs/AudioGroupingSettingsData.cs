using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct AudioGroupingSettingsData : IBufferElementData
{
	public GroupAmbienceType m_Type;

	public float2 m_Height;

	public float m_FadeSpeed;

	public float m_Scale;

	public Entity m_GroupSoundNear;

	public Entity m_GroupSoundFar;

	public float2 m_NearHeight;

	public float m_NearWeight;
}
