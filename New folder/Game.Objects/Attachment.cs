using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Attachment : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Entity m_Attached;

	public Attachment(Entity attached)
	{
		m_Attached = attached;
	}
}
