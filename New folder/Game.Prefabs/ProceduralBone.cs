using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct ProceduralBone : IBufferElementData
{
	public float3 m_Position;

	public float3 m_ObjectPosition;

	public quaternion m_Rotation;

	public quaternion m_ObjectRotation;

	public float3 m_Scale;

	public float4x4 m_BindPose;

	public BoneType m_Type;

	public int m_ParentIndex;

	public int m_BindIndex;

	public int m_HierarchyDepth;

	public int m_ConnectionID;

	public int m_SourceIndex;

	public float m_Speed;

	public float m_Acceleration;
}
