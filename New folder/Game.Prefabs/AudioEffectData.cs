using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct AudioEffectData : IComponentData, IQueryTypeParameter
{
	public int m_AudioClipId;

	public float m_MaxDistance;

	public float3 m_SourceSize;

	public float2 m_FadeTimes;
}
