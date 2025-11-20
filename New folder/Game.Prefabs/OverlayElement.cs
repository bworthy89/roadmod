using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct OverlayElement : IBufferElementData
{
	public Entity m_Overlay;

	public int m_SortOrder;

	public float4 m_SourceRegion;

	public float4 m_TargetRegion;
}
