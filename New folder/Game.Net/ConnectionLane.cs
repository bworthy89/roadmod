using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct ConnectionLane : IComponentData, IQueryTypeParameter, ISerializable, IEquatable<ConnectionLane>
{
	public Entity m_AccessRestriction;

	public ConnectionLaneFlags m_Flags;

	public TrackTypes m_TrackTypes;

	public RoadTypes m_RoadTypes;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity accessRestriction = m_AccessRestriction;
		writer.Write(accessRestriction);
		ConnectionLaneFlags flags = m_Flags;
		writer.Write((uint)flags);
		TrackTypes trackTypes = m_TrackTypes;
		writer.Write((byte)trackTypes);
		RoadTypes roadTypes = m_RoadTypes;
		writer.Write((byte)roadTypes);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.pathfindAccessRestriction)
		{
			ref Entity accessRestriction = ref m_AccessRestriction;
			reader.Read(out accessRestriction);
		}
		reader.Read(out uint value);
		reader.Read(out byte value2);
		if (reader.context.version >= Version.shipLanes)
		{
			reader.Read(out byte value3);
			m_RoadTypes = (RoadTypes)value3;
		}
		m_Flags = (ConnectionLaneFlags)value;
		m_TrackTypes = (TrackTypes)value2;
	}

	public bool Equals(ConnectionLane other)
	{
		if (m_Flags == other.m_Flags && m_TrackTypes == other.m_TrackTypes)
		{
			return m_RoadTypes == other.m_RoadTypes;
		}
		return false;
	}
}
