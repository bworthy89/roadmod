using System;
using Unity.Entities;

namespace Game.Routes;

public struct RouteSearchItem : IEquatable<RouteSearchItem>
{
	public Entity m_Entity;

	public int m_Element;

	public RouteSearchItem(Entity entity, int element)
	{
		m_Entity = entity;
		m_Element = element;
	}

	public bool Equals(RouteSearchItem other)
	{
		return m_Entity.Equals(other.m_Entity) & m_Element.Equals(other.m_Element);
	}

	public override int GetHashCode()
	{
		return (17 * 31 + m_Entity.GetHashCode()) * 31 + m_Element.GetHashCode();
	}
}
