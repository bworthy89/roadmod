using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Routes;

public struct PathTargets : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_StartLane;

	public Entity m_EndLane;

	public float2 m_CurvePositions;

	public float3 m_ReadyStartPosition;

	public float3 m_ReadyEndPosition;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity startLane = m_StartLane;
		writer.Write(startLane);
		Entity endLane = m_EndLane;
		writer.Write(endLane);
		float2 curvePositions = m_CurvePositions;
		writer.Write(curvePositions);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity startLane = ref m_StartLane;
		reader.Read(out startLane);
		ref Entity endLane = ref m_EndLane;
		reader.Read(out endLane);
		ref float2 curvePositions = ref m_CurvePositions;
		reader.Read(out curvePositions);
	}
}
