using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Pathfind;
using Game.Routes;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Creatures;

public struct HumanCurrentLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Lane;

	public Entity m_QueueEntity;

	public Sphere3 m_QueueArea;

	public float2 m_CurvePosition;

	public CreatureLaneFlags m_Flags;

	public float m_LanePosition;

	public HumanCurrentLane(AccessLane accessLane, CreatureLaneFlags flags)
	{
		m_Lane = accessLane.m_Lane;
		m_QueueEntity = Entity.Null;
		m_QueueArea = default(Sphere3);
		m_CurvePosition = accessLane.m_CurvePos;
		m_Flags = flags;
		m_LanePosition = 0f;
	}

	public HumanCurrentLane(PathElement pathElement, CreatureLaneFlags flags)
	{
		m_Lane = pathElement.m_Target;
		m_QueueEntity = Entity.Null;
		m_QueueArea = default(Sphere3);
		m_CurvePosition = pathElement.m_TargetDelta.xx;
		m_Flags = flags;
		m_LanePosition = 0f;
	}

	public HumanCurrentLane(CreatureLaneFlags flags)
	{
		m_Lane = Entity.Null;
		m_QueueEntity = Entity.Null;
		m_QueueArea = default(Sphere3);
		m_CurvePosition = 0f;
		m_Flags = flags;
		m_LanePosition = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		float2 curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
		CreatureLaneFlags flags = m_Flags;
		writer.Write((uint)flags);
		float lanePosition = m_LanePosition;
		writer.Write(lanePosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref float2 curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
		reader.Read(out uint value);
		if (reader.context.version >= Version.lanePosition)
		{
			ref float lanePosition = ref m_LanePosition;
			reader.Read(out lanePosition);
		}
		m_Flags = (CreatureLaneFlags)value;
	}
}
