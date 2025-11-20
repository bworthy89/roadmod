using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(1)]
public struct SubMesh : IBufferElementData, IEmptySerializable
{
	public Entity m_SubMesh;

	public float3 m_Position;

	public quaternion m_Rotation;

	public SubMeshFlags m_Flags;

	public ushort m_RandomSeed;

	public SubMesh(Entity mesh, SubMeshFlags flags, ushort randomSeed)
	{
		m_SubMesh = mesh;
		m_Position = default(float3);
		m_Rotation = quaternion.identity;
		m_Flags = flags;
		m_RandomSeed = randomSeed;
	}

	public SubMesh(Entity mesh, float3 position, quaternion rotation, SubMeshFlags flags, ushort randomSeed)
	{
		m_SubMesh = mesh;
		m_Position = position;
		m_Rotation = rotation;
		m_Flags = flags;
		m_RandomSeed = randomSeed;
	}
}
