using Unity.Entities;

namespace Game.Tutorials;

public struct ObjectSelectionActivationData : IBufferElementData
{
	public Entity m_Prefab;

	public bool m_AllowTool;

	public ObjectSelectionActivationData(Entity prefab, bool allowTool)
	{
		m_Prefab = prefab;
		m_AllowTool = allowTool;
	}
}
