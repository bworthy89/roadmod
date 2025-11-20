using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Notifications;

[InternalBufferCapacity(4)]
public struct IconElement : IBufferElementData, IEquatable<IconElement>, IEmptySerializable
{
	public Entity m_Icon;

	public IconElement(Entity icon)
	{
		m_Icon = icon;
	}

	public bool Equals(IconElement other)
	{
		return m_Icon.Equals(other.m_Icon);
	}

	public override int GetHashCode()
	{
		return m_Icon.GetHashCode();
	}
}
