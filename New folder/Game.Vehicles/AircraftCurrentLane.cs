using Colossal.Serialization.Entities;
using Game.Pathfind;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct AircraftCurrentLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Lane;

	public float3 m_CurvePosition;

	public AircraftLaneFlags m_LaneFlags;

	public float m_Duration;

	public float m_Distance;

	public float m_LanePosition;

	public AircraftCurrentLane(ParkedCar parkedCar, AircraftLaneFlags flags)
	{
		m_Lane = parkedCar.m_Lane;
		m_CurvePosition = parkedCar.m_CurvePosition;
		m_LaneFlags = flags;
		m_Duration = 0f;
		m_Distance = 0f;
		m_LanePosition = 0f;
	}

	public AircraftCurrentLane(PathElement pathElement, AircraftLaneFlags laneFlags)
	{
		m_Lane = pathElement.m_Target;
		m_CurvePosition = pathElement.m_TargetDelta.xxx;
		m_LaneFlags = laneFlags;
		m_Duration = 0f;
		m_Distance = 0f;
		m_LanePosition = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		float3 curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
		AircraftLaneFlags laneFlags = m_LaneFlags;
		writer.Write((uint)laneFlags);
		float duration = m_Duration;
		writer.Write(duration);
		float distance = m_Distance;
		writer.Write(distance);
		float lanePosition = m_LanePosition;
		writer.Write(lanePosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref float3 curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
		reader.Read(out uint value);
		ref float duration = ref m_Duration;
		reader.Read(out duration);
		ref float distance = ref m_Distance;
		reader.Read(out distance);
		ref float lanePosition = ref m_LanePosition;
		reader.Read(out lanePosition);
		m_LaneFlags = (AircraftLaneFlags)value;
	}
}
