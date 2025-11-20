using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct Effect : IBufferElementData
{
	public Entity m_Effect;

	public float3 m_Position;

	public float3 m_Scale;

	public quaternion m_Rotation;

	public int2 m_BoneIndex;

	public float m_Intensity;

	public int m_ParentMesh;

	public int m_AnimationIndex;

	public bool m_Procedural;
}
