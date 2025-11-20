using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

[InternalBufferCapacity(0)]
public struct SubRoute : IBufferElementData, IEquatable<SubRoute>, IEmptySerializable
{
	public Entity m_Route;

	public SubRoute(Entity route)
	{
		m_Route = route;
	}

	public bool Equals(SubRoute other)
	{
		return m_Route.Equals(other.m_Route);
	}

	public override int GetHashCode()
	{
		return m_Route.GetHashCode();
	}
}
