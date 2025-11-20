using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct AuxiliaryNetLane : IBufferElementData
{
	public Entity m_Prefab;

	public float3 m_Position;

	public float3 m_Spacing;

	public CompositionFlags m_CompositionAll;

	public CompositionFlags m_CompositionAny;

	public CompositionFlags m_CompositionNone;

	public LaneFlags m_Flags;
}
