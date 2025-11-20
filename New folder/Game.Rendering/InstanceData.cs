using Unity.Entities;

namespace Game.Rendering;

public struct InstanceData
{
	public Entity m_Entity;

	public byte m_MeshGroup;

	public byte m_MeshIndex;

	public byte m_TileIndex;
}
