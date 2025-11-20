using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct CarTrailerLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Lane;

	public Entity m_NextLane;

	public float2 m_CurvePosition;

	public float2 m_NextPosition;

	public float m_Duration;

	public float m_Distance;

	public CarTrailerLane(ParkedCar parkedCar)
	{
		m_Lane = parkedCar.m_Lane;
		m_NextLane = Entity.Null;
		m_CurvePosition = parkedCar.m_CurvePosition;
		m_NextPosition = 0f;
		m_Duration = 0f;
		m_Distance = 0f;
	}

	public CarTrailerLane(CarCurrentLane currentLane)
	{
		m_Lane = currentLane.m_Lane;
		m_NextLane = Entity.Null;
		m_CurvePosition = currentLane.m_CurvePosition.xy;
		m_NextPosition = 0f;
		m_Duration = 0f;
		m_Distance = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		Entity nextLane = m_NextLane;
		writer.Write(nextLane);
		float2 curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
		float2 nextPosition = m_NextPosition;
		writer.Write(nextPosition);
		float duration = m_Duration;
		writer.Write(duration);
		float distance = m_Distance;
		writer.Write(distance);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref Entity nextLane = ref m_NextLane;
		reader.Read(out nextLane);
		ref float2 curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
		ref float2 nextPosition = ref m_NextPosition;
		reader.Read(out nextPosition);
		ref float duration = ref m_Duration;
		reader.Read(out duration);
		ref float distance = ref m_Distance;
		reader.Read(out distance);
	}
}
