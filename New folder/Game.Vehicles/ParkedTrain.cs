using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct ParkedTrain : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ParkingLocation;

	public Entity m_FrontLane;

	public Entity m_RearLane;

	public float2 m_CurvePosition;

	public ParkedTrain(Entity location)
	{
		m_ParkingLocation = location;
		m_FrontLane = Entity.Null;
		m_RearLane = Entity.Null;
		m_CurvePosition = 0f;
	}

	public ParkedTrain(Entity location, TrainCurrentLane currentLane)
	{
		m_ParkingLocation = location;
		m_FrontLane = currentLane.m_Front.m_Lane;
		m_RearLane = currentLane.m_Rear.m_Lane;
		m_CurvePosition = new float2(currentLane.m_Front.m_CurvePosition.y, currentLane.m_Rear.m_CurvePosition.y);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity parkingLocation = m_ParkingLocation;
		writer.Write(parkingLocation);
		Entity frontLane = m_FrontLane;
		writer.Write(frontLane);
		Entity rearLane = m_RearLane;
		writer.Write(rearLane);
		float2 curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity parkingLocation = ref m_ParkingLocation;
		reader.Read(out parkingLocation);
		ref Entity frontLane = ref m_FrontLane;
		reader.Read(out frontLane);
		ref Entity rearLane = ref m_RearLane;
		reader.Read(out rearLane);
		ref float2 curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
	}
}
