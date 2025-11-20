using Colossal.Serialization.Entities;
using Game.Pathfind;
using Unity.Entities;

namespace Game.Vehicles;

public struct TrainCurrentLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public TrainBogieLane m_Front;

	public TrainBogieLane m_Rear;

	public TrainBogieCache m_FrontCache;

	public TrainBogieCache m_RearCache;

	public float m_Duration;

	public float m_Distance;

	public TrainCurrentLane(PathElement pathElement)
	{
		m_Front = new TrainBogieLane(pathElement);
		m_Rear = new TrainBogieLane(pathElement);
		m_FrontCache = new TrainBogieCache(pathElement);
		m_RearCache = new TrainBogieCache(pathElement);
		m_Duration = 0f;
		m_Distance = 0f;
	}

	public TrainCurrentLane(ParkedTrain parkedTrain)
	{
		m_Front = new TrainBogieLane(parkedTrain.m_FrontLane, parkedTrain.m_CurvePosition.x);
		m_Rear = new TrainBogieLane(parkedTrain.m_RearLane, parkedTrain.m_CurvePosition.y);
		m_FrontCache = new TrainBogieCache(parkedTrain.m_FrontLane, parkedTrain.m_CurvePosition.x);
		m_RearCache = new TrainBogieCache(parkedTrain.m_RearLane, parkedTrain.m_CurvePosition.y);
		m_Duration = 0f;
		m_Distance = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		TrainBogieLane front = m_Front;
		writer.Write(front);
		TrainBogieLane rear = m_Rear;
		writer.Write(rear);
		TrainBogieCache frontCache = m_FrontCache;
		writer.Write(frontCache);
		TrainBogieCache rearCache = m_RearCache;
		writer.Write(rearCache);
		float duration = m_Duration;
		writer.Write(duration);
		float distance = m_Distance;
		writer.Write(distance);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref TrainBogieLane front = ref m_Front;
		reader.Read(out front);
		ref TrainBogieLane rear = ref m_Rear;
		reader.Read(out rear);
		ref TrainBogieCache frontCache = ref m_FrontCache;
		reader.Read(out frontCache);
		ref TrainBogieCache rearCache = ref m_RearCache;
		reader.Read(out rearCache);
		if (reader.context.version >= Version.trafficFlowFixes)
		{
			ref float duration = ref m_Duration;
			reader.Read(out duration);
			ref float distance = ref m_Distance;
			reader.Read(out distance);
		}
	}
}
