using Unity.Entities;

namespace Game.Prefabs;

public struct MeshSettingsData : IComponentData, IQueryTypeParameter
{
	public Entity m_MissingObjectMesh;

	public Entity m_DefaultBaseMesh;

	public Entity m_MissingNetSection;
}
