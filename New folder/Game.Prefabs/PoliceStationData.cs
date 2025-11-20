using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PoliceStationData : IComponentData, IQueryTypeParameter, ICombineData<PoliceStationData>, ISerializable
{
	public int m_PatrolCarCapacity;

	public int m_PoliceHelicopterCapacity;

	public int m_JailCapacity;

	public PolicePurpose m_PurposeMask;

	public void Combine(PoliceStationData otherData)
	{
		m_PatrolCarCapacity += otherData.m_PatrolCarCapacity;
		m_PoliceHelicopterCapacity += otherData.m_PoliceHelicopterCapacity;
		m_JailCapacity += otherData.m_JailCapacity;
		m_PurposeMask |= otherData.m_PurposeMask;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int patrolCarCapacity = m_PatrolCarCapacity;
		writer.Write(patrolCarCapacity);
		int policeHelicopterCapacity = m_PoliceHelicopterCapacity;
		writer.Write(policeHelicopterCapacity);
		int jailCapacity = m_JailCapacity;
		writer.Write(jailCapacity);
		byte value = (byte)m_PurposeMask;
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int patrolCarCapacity = ref m_PatrolCarCapacity;
		reader.Read(out patrolCarCapacity);
		ref int policeHelicopterCapacity = ref m_PoliceHelicopterCapacity;
		reader.Read(out policeHelicopterCapacity);
		ref int jailCapacity = ref m_JailCapacity;
		reader.Read(out jailCapacity);
		reader.Read(out byte value);
		m_PurposeMask = (PolicePurpose)value;
	}
}
