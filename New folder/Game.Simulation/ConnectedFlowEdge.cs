using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct ConnectedFlowEdge : IBufferElementData, IEmptySerializable, IEquatable<ConnectedFlowEdge>
{
	public Entity m_Edge;

	public ConnectedFlowEdge(Entity edge)
	{
		m_Edge = edge;
	}

	public bool Equals(ConnectedFlowEdge other)
	{
		return m_Edge.Equals(other.m_Edge);
	}

	public override int GetHashCode()
	{
		return m_Edge.GetHashCode();
	}

	public static implicit operator Entity(ConnectedFlowEdge element)
	{
		return element.m_Edge;
	}
}
