using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Creatures;

public struct AnimalCurrentLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Lane;

	public Entity m_NextLane;

	public Entity m_QueueEntity;

	public Sphere3 m_QueueArea;

	public float2 m_CurvePosition;

	public float2 m_NextPosition;

	public CreatureLaneFlags m_Flags;

	public CreatureLaneFlags m_NextFlags;

	public float m_LanePosition;

	public AnimalCurrentLane(Entity lane, float curvePosition, CreatureLaneFlags flags)
	{
		m_Lane = lane;
		m_NextLane = Entity.Null;
		m_QueueEntity = Entity.Null;
		m_QueueArea = default(Sphere3);
		m_CurvePosition = curvePosition;
		m_NextPosition = 0f;
		m_Flags = flags;
		m_NextFlags = (CreatureLaneFlags)0u;
		m_LanePosition = 0f;
	}

	public AnimalCurrentLane(CreatureLaneFlags flags)
	{
		m_Lane = Entity.Null;
		m_NextLane = Entity.Null;
		m_QueueEntity = Entity.Null;
		m_QueueArea = default(Sphere3);
		m_CurvePosition = 0f;
		m_NextPosition = 0f;
		m_Flags = flags;
		m_NextFlags = (CreatureLaneFlags)0u;
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
		Entity nextLane = m_NextLane;
		writer.Write(nextLane);
		float2 nextPosition = m_NextPosition;
		writer.Write(nextPosition);
		CreatureLaneFlags nextFlags = m_NextFlags;
		writer.Write((uint)nextFlags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref float2 curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
		reader.Read(out uint value);
		ref float lanePosition = ref m_LanePosition;
		reader.Read(out lanePosition);
		m_Flags = (CreatureLaneFlags)value;
		if (reader.context.version >= Version.animalNavigation)
		{
			ref Entity nextLane = ref m_NextLane;
			reader.Read(out nextLane);
			ref float2 nextPosition = ref m_NextPosition;
			reader.Read(out nextPosition);
			reader.Read(out uint value2);
			m_NextFlags = (CreatureLaneFlags)value2;
		}
	}
}
