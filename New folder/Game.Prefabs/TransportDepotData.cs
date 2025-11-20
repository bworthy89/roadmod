using Colossal.Serialization.Entities;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

public struct TransportDepotData : IComponentData, IQueryTypeParameter, ICombineData<TransportDepotData>, ISerializable
{
	public TransportType m_TransportType;

	public EnergyTypes m_EnergyTypes;

	public SizeClass m_SizeClass;

	public bool m_DispatchCenter;

	public int m_VehicleCapacity;

	public float m_ProductionDuration;

	public float m_MaintenanceDuration;

	public void Combine(TransportDepotData otherData)
	{
		m_EnergyTypes |= otherData.m_EnergyTypes;
		m_DispatchCenter |= otherData.m_DispatchCenter;
		m_VehicleCapacity += otherData.m_VehicleCapacity;
		m_ProductionDuration += otherData.m_ProductionDuration;
		m_MaintenanceDuration += otherData.m_MaintenanceDuration;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		bool dispatchCenter = m_DispatchCenter;
		writer.Write(dispatchCenter);
		int vehicleCapacity = m_VehicleCapacity;
		writer.Write(vehicleCapacity);
		float productionDuration = m_ProductionDuration;
		writer.Write(productionDuration);
		float maintenanceDuration = m_MaintenanceDuration;
		writer.Write(maintenanceDuration);
		TransportType transportType = m_TransportType;
		writer.Write((int)transportType);
		EnergyTypes energyTypes = m_EnergyTypes;
		writer.Write((byte)energyTypes);
		SizeClass sizeClass = m_SizeClass;
		writer.Write((byte)sizeClass);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref bool dispatchCenter = ref m_DispatchCenter;
		reader.Read(out dispatchCenter);
		ref int vehicleCapacity = ref m_VehicleCapacity;
		reader.Read(out vehicleCapacity);
		ref float productionDuration = ref m_ProductionDuration;
		reader.Read(out productionDuration);
		ref float maintenanceDuration = ref m_MaintenanceDuration;
		reader.Read(out maintenanceDuration);
		reader.Read(out int value);
		reader.Read(out byte value2);
		m_TransportType = (TransportType)value;
		m_EnergyTypes = (EnergyTypes)value2;
		if (reader.context.format.Has(FormatTags.BpPrefabData))
		{
			reader.Read(out byte value3);
			m_SizeClass = (SizeClass)value3;
		}
		else
		{
			m_SizeClass = SizeClass.Large;
		}
	}
}
