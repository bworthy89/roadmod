using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct RouteInfo : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Duration;

	public float m_Distance;

	public RouteInfoFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float duration = m_Duration;
		writer.Write(duration);
		float distance = m_Distance;
		writer.Write(distance);
		RouteInfoFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float duration = ref m_Duration;
		reader.Read(out duration);
		ref float distance = ref m_Distance;
		reader.Read(out distance);
		if (reader.context.version >= Version.transportLinePolicies)
		{
			reader.Read(out byte value);
			m_Flags = (RouteInfoFlags)value;
		}
	}
}
