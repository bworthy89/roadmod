using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct LaneConnection : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_StartLane;

	public Entity m_EndLane;

	public float m_StartPosition;

	public float m_EndPosition;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity startLane = m_StartLane;
		writer.Write(startLane);
		Entity endLane = m_EndLane;
		writer.Write(endLane);
		float startPosition = m_StartPosition;
		writer.Write(startPosition);
		float endPosition = m_EndPosition;
		writer.Write(endPosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity startLane = ref m_StartLane;
		reader.Read(out startLane);
		ref Entity endLane = ref m_EndLane;
		reader.Read(out endLane);
		ref float startPosition = ref m_StartPosition;
		reader.Read(out startPosition);
		ref float endPosition = ref m_EndPosition;
		reader.Read(out endPosition);
	}
}
