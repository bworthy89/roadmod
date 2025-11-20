using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

[InternalBufferCapacity(0)]
public struct SubObject : IBufferElementData, IEquatable<SubObject>, IEmptySerializable
{
	public Entity m_SubObject;

	public SubObject(Entity subObject)
	{
		m_SubObject = subObject;
	}

	public bool Equals(SubObject other)
	{
		return m_SubObject.Equals(other.m_SubObject);
	}

	public override int GetHashCode()
	{
		return m_SubObject.GetHashCode();
	}
}
