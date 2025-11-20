using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct ParkData : IComponentData, IQueryTypeParameter, ICombineData<ParkData>, ISerializable
{
	public short m_MaintenancePool;

	public bool m_AllowHomeless;

	public void Combine(ParkData otherData)
	{
		m_MaintenancePool += otherData.m_MaintenancePool;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		short maintenancePool = m_MaintenancePool;
		writer.Write(maintenancePool);
		bool allowHomeless = m_AllowHomeless;
		writer.Write(allowHomeless);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref short maintenancePool = ref m_MaintenancePool;
		reader.Read(out maintenancePool);
		if (reader.context.version >= Version.homelessPark)
		{
			ref bool allowHomeless = ref m_AllowHomeless;
			reader.Read(out allowHomeless);
		}
	}
}
