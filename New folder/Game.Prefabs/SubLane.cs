using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct SubLane : IBufferElementData
{
	public Entity m_Prefab;

	public Bezier4x3 m_Curve;

	public int2 m_NodeIndex;

	public int2 m_ParentMesh;
}
