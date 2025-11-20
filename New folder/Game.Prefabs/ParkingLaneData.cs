using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct ParkingLaneData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float2 m_SlotSize;

	public float m_SlotAngle;

	public float m_SlotInterval;

	public float m_MaxCarLength;

	public RoadTypes m_RoadTypes;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float2 slotSize = m_SlotSize;
		writer.Write(slotSize);
		float slotAngle = m_SlotAngle;
		writer.Write(slotAngle);
		float slotInterval = m_SlotInterval;
		writer.Write(slotInterval);
		float maxCarLength = m_MaxCarLength;
		writer.Write(maxCarLength);
		RoadTypes roadTypes = m_RoadTypes;
		writer.Write((byte)roadTypes);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float2 slotSize = ref m_SlotSize;
		reader.Read(out slotSize);
		ref float slotAngle = ref m_SlotAngle;
		reader.Read(out slotAngle);
		ref float slotInterval = ref m_SlotInterval;
		reader.Read(out slotInterval);
		ref float maxCarLength = ref m_MaxCarLength;
		reader.Read(out maxCarLength);
		if (reader.context.version >= Version.roadPatchImprovements)
		{
			reader.Read(out byte value);
			m_RoadTypes = (RoadTypes)value;
		}
		else
		{
			m_RoadTypes = RoadTypes.Car;
		}
	}
}
