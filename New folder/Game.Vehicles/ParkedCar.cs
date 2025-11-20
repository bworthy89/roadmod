using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct ParkedCar : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Lane;

	public float m_CurvePosition;

	public ParkedCar(Entity lane, float curvePosition)
	{
		m_Lane = lane;
		m_CurvePosition = curvePosition;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		float curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref float curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
	}
}
