using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct UIGroupElement : IBufferElementData
{
	public Entity m_Prefab;

	public UIGroupElement(Entity prefab)
	{
		m_Prefab = prefab;
	}
}
