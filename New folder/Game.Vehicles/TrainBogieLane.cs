using Colossal.Serialization.Entities;
using Game.Pathfind;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct TrainBogieLane : ISerializable
{
	public Entity m_Lane;

	public float4 m_CurvePosition;

	public TrainLaneFlags m_LaneFlags;

	public TrainBogieLane(TrainBogieCache cache)
	{
		m_Lane = cache.m_Lane;
		m_CurvePosition = cache.m_CurvePosition.xxxy;
		m_LaneFlags = cache.m_LaneFlags;
	}

	public TrainBogieLane(Entity lane, float4 curvePosition, TrainLaneFlags laneFlags)
	{
		m_Lane = lane;
		m_CurvePosition = curvePosition;
		m_LaneFlags = laneFlags;
	}

	public TrainBogieLane(TrainNavigationLane navLane)
	{
		m_Lane = navLane.m_Lane;
		m_CurvePosition = navLane.m_CurvePosition.xxxy;
		m_LaneFlags = navLane.m_Flags;
	}

	public TrainBogieLane(PathElement pathElement)
	{
		m_Lane = pathElement.m_Target;
		m_CurvePosition = pathElement.m_TargetDelta.xxxx;
		m_LaneFlags = (TrainLaneFlags)0u;
	}

	public TrainBogieLane(Entity lane, float curvePosition)
	{
		m_Lane = lane;
		m_CurvePosition = curvePosition;
		m_LaneFlags = (TrainLaneFlags)0u;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		float4 curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
		TrainLaneFlags laneFlags = m_LaneFlags;
		writer.Write((uint)laneFlags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		if (reader.context.version >= Version.tramNavigationImprovement)
		{
			ref float4 curvePosition = ref m_CurvePosition;
			reader.Read(out curvePosition);
		}
		else
		{
			reader.Read(out float3 value);
			m_CurvePosition = value.xyyz;
		}
		reader.Read(out uint value2);
		m_LaneFlags = (TrainLaneFlags)value2;
	}
}
