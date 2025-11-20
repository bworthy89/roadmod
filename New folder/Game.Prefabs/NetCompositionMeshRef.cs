using Unity.Entities;

namespace Game.Prefabs;

public struct NetCompositionMeshRef : IComponentData, IQueryTypeParameter
{
	public Entity m_Mesh;

	public bool m_Rotate;
}
