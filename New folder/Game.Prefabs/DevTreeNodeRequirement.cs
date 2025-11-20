using Unity.Entities;

namespace Game.Prefabs;

public struct DevTreeNodeRequirement : IBufferElementData
{
	public Entity m_Node;
}
