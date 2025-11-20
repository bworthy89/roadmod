using Colossal.Serialization.Entities;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

public struct TransportStationData : IComponentData, IQueryTypeParameter, ICombineData<TransportStationData>, ISerializable
{
	public float m_ComfortFactor;

	public float m_LoadingFactor;

	public EnergyTypes m_CarRefuelTypes;

	public EnergyTypes m_TrainRefuelTypes;

	public EnergyTypes m_WatercraftRefuelTypes;

	public EnergyTypes m_AircraftRefuelTypes;

	public void Combine(TransportStationData otherData)
	{
		m_ComfortFactor += otherData.m_ComfortFactor;
		m_LoadingFactor += otherData.m_LoadingFactor;
		m_CarRefuelTypes |= otherData.m_CarRefuelTypes;
		m_TrainRefuelTypes |= otherData.m_TrainRefuelTypes;
		m_WatercraftRefuelTypes |= otherData.m_WatercraftRefuelTypes;
		m_AircraftRefuelTypes |= otherData.m_AircraftRefuelTypes;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float comfortFactor = m_ComfortFactor;
		writer.Write(comfortFactor);
		float loadingFactor = m_LoadingFactor;
		writer.Write(loadingFactor);
		EnergyTypes carRefuelTypes = m_CarRefuelTypes;
		writer.Write((byte)carRefuelTypes);
		EnergyTypes trainRefuelTypes = m_TrainRefuelTypes;
		writer.Write((byte)trainRefuelTypes);
		EnergyTypes watercraftRefuelTypes = m_WatercraftRefuelTypes;
		writer.Write((byte)watercraftRefuelTypes);
		EnergyTypes aircraftRefuelTypes = m_AircraftRefuelTypes;
		writer.Write((byte)aircraftRefuelTypes);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float comfortFactor = ref m_ComfortFactor;
		reader.Read(out comfortFactor);
		ref float loadingFactor = ref m_LoadingFactor;
		reader.Read(out loadingFactor);
		reader.Read(out byte value);
		reader.Read(out byte value2);
		reader.Read(out byte value3);
		reader.Read(out byte value4);
		m_CarRefuelTypes = (EnergyTypes)value;
		m_TrainRefuelTypes = (EnergyTypes)value2;
		m_WatercraftRefuelTypes = (EnergyTypes)value3;
		m_AircraftRefuelTypes = (EnergyTypes)value4;
	}
}
