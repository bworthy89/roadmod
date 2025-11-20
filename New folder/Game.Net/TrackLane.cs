using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct TrackLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_AccessRestriction;

	public TrackLaneFlags m_Flags;

	public float m_SpeedLimit;

	public float m_Curviness;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity accessRestriction = m_AccessRestriction;
		writer.Write(accessRestriction);
		TrackLaneFlags flags = m_Flags;
		writer.Write((uint)flags);
		float speedLimit = m_SpeedLimit;
		writer.Write(speedLimit);
		float curviness = m_Curviness;
		writer.Write(curviness);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.pathfindRestrictions)
		{
			ref Entity accessRestriction = ref m_AccessRestriction;
			reader.Read(out accessRestriction);
		}
		reader.Read(out uint value);
		ref float speedLimit = ref m_SpeedLimit;
		reader.Read(out speedLimit);
		ref float curviness = ref m_Curviness;
		reader.Read(out curviness);
		m_Flags = (TrackLaneFlags)value;
	}
}
