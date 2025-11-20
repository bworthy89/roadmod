using Unity.Entities;

namespace Game.Prefabs;

public struct SharedMeshData : IComponentData, IQueryTypeParameter
{
	public Entity m_Mesh;
}
