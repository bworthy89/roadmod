using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct PlaceholderObjectElement : IBufferElementData
{
	public Entity m_Object;

	public PlaceholderObjectElement(Entity obj)
	{
		m_Object = obj;
	}
}
