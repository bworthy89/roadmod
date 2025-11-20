using Unity.Entities;

namespace Game.Tools;

[InternalBufferCapacity(0)]
public struct SelectionElement : IBufferElementData
{
	public Entity m_Entity;

	public SelectionElement(Entity entity)
	{
		m_Entity = entity;
	}
}
