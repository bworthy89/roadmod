using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetCompositionCrosswalk : IBufferElementData
{
	public Entity m_Lane;

	public float3 m_Start;

	public float3 m_End;

	public LaneFlags m_Flags;
}
