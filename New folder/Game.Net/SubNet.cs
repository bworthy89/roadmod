using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct SubNet : IBufferElementData, IEquatable<SubNet>, IEmptySerializable
{
	public Entity m_SubNet;

	public SubNet(Entity subNet)
	{
		m_SubNet = subNet;
	}

	public bool Equals(SubNet other)
	{
		return m_SubNet.Equals(other.m_SubNet);
	}

	public override int GetHashCode()
	{
		return m_SubNet.GetHashCode();
	}
}
