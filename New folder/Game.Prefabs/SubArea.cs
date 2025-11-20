using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(1)]
public struct SubArea : IBufferElementData
{
	public Entity m_Prefab;

	public int2 m_NodeRange;
}
