using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct SubObject : IBufferElementData
{
	public Entity m_Prefab;

	public SubObjectFlags m_Flags;

	public float3 m_Position;

	public quaternion m_Rotation;

	public int m_ParentIndex;

	public int m_GroupIndex;

	public int m_Probability;
}
