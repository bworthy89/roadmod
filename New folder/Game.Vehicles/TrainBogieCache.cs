using Colossal.Serialization.Entities;
using Game.Pathfind;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct TrainBogieCache : ISerializable
{
	public Entity m_Lane;

	public float2 m_CurvePosition;

	public TrainLaneFlags m_LaneFlags;

	public TrainBogieCache(TrainBogieLane lane)
	{
		m_Lane = lane.m_Lane;
		m_CurvePosition = lane.m_CurvePosition.xw;
		m_LaneFlags = lane.m_LaneFlags;
	}

	public TrainBogieCache(PathElement pathElement)
	{
		m_Lane = pathElement.m_Target;
		m_CurvePosition = pathElement.m_TargetDelta.xx;
		m_LaneFlags = (TrainLaneFlags)0u;
	}

	public TrainBogieCache(Entity lane, float curvePosition)
	{
		m_Lane = lane;
		m_CurvePosition = curvePosition;
		m_LaneFlags = (TrainLaneFlags)0u;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		float2 curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
		TrainLaneFlags laneFlags = m_LaneFlags;
		writer.Write((uint)laneFlags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref float2 curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
		reader.Read(out uint value);
		m_LaneFlags = (TrainLaneFlags)value;
	}
}
