using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Economy;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

public struct WorkVehicleData : IComponentData, IQueryTypeParameter, ISerializable
{
	public VehicleWorkType m_WorkType;

	public MapFeature m_MapFeature;

	public Resource m_Resources;

	public float m_MaxWorkAmount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte value = (byte)m_WorkType;
		writer.Write(value);
		sbyte value2 = (sbyte)m_MapFeature;
		writer.Write(value2);
		float maxWorkAmount = m_MaxWorkAmount;
		writer.Write(maxWorkAmount);
		Resource resources = m_Resources;
		writer.Write((ulong)resources);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		reader.Read(out sbyte value2);
		ref float maxWorkAmount = ref m_MaxWorkAmount;
		reader.Read(out maxWorkAmount);
		m_WorkType = (VehicleWorkType)value;
		m_MapFeature = (MapFeature)value2;
		if (reader.context.version >= Version.landfillVehicles)
		{
			reader.Read(out ulong value3);
			m_Resources = (Resource)value3;
		}
	}
}
