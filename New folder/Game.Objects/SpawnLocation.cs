using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct SpawnLocation : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_AccessRestriction;

	public Entity m_ConnectedLane1;

	public Entity m_ConnectedLane2;

	public float m_CurvePosition1;

	public float m_CurvePosition2;

	public int m_GroupIndex;

	public SpawnLocationFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity accessRestriction = m_AccessRestriction;
		writer.Write(accessRestriction);
		Entity connectedLane = m_ConnectedLane1;
		writer.Write(connectedLane);
		Entity connectedLane2 = m_ConnectedLane2;
		writer.Write(connectedLane2);
		float curvePosition = m_CurvePosition1;
		writer.Write(curvePosition);
		float curvePosition2 = m_CurvePosition2;
		writer.Write(curvePosition2);
		int groupIndex = m_GroupIndex;
		writer.Write(groupIndex);
		SpawnLocationFlags flags = m_Flags;
		writer.Write((uint)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.pathfindAccessRestriction)
		{
			ref Entity accessRestriction = ref m_AccessRestriction;
			reader.Read(out accessRestriction);
		}
		if (reader.context.version >= Version.spawnLocationRefactor)
		{
			ref Entity connectedLane = ref m_ConnectedLane1;
			reader.Read(out connectedLane);
			ref Entity connectedLane2 = ref m_ConnectedLane2;
			reader.Read(out connectedLane2);
			ref float curvePosition = ref m_CurvePosition1;
			reader.Read(out curvePosition);
			ref float curvePosition2 = ref m_CurvePosition2;
			reader.Read(out curvePosition2);
		}
		else
		{
			reader.Read(out Entity _);
			reader.Read(out float _);
		}
		if (reader.context.version >= Version.spawnLocationGroup)
		{
			ref int groupIndex = ref m_GroupIndex;
			reader.Read(out groupIndex);
		}
		if (reader.context.version >= Version.pathfindRestrictions)
		{
			reader.Read(out uint value3);
			m_Flags = (SpawnLocationFlags)value3;
		}
	}
}
