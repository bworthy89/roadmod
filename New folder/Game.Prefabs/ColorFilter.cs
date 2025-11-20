using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(2)]
public struct ColorFilter : IBufferElementData
{
	public ColorGroupID m_GroupID;

	public float3 m_OverrideAlpha;

	public Entity m_EntityFilter;

	public ColorFilterFlags m_Flags;

	public AgeMask m_AgeFilter;

	public GenderMask m_GenderFilter;

	public sbyte m_OverrideProbability;
}
