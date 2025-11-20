using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct TrafficLight : IComponentData, IQueryTypeParameter, ISerializable
{
	public TrafficLightState m_State;

	public ushort m_GroupMask0;

	public ushort m_GroupMask1;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		TrafficLightState state = m_State;
		writer.Write((ushort)state);
		ushort groupMask = m_GroupMask0;
		writer.Write(groupMask);
		ushort groupMask2 = m_GroupMask1;
		writer.Write(groupMask2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out ushort value);
		if (reader.context.version >= Version.trafficLightGroups)
		{
			ref ushort groupMask = ref m_GroupMask0;
			reader.Read(out groupMask);
			ref ushort groupMask2 = ref m_GroupMask1;
			reader.Read(out groupMask2);
		}
		m_State = (TrafficLightState)value;
	}
}
