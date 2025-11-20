using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(1)]
public struct PlaceableInfoviewItem : IBufferElementData
{
	public Entity m_Item;

	public int m_Priority;
}
