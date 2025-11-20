using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct EmergencyGeneratorData : IComponentData, IQueryTypeParameter, ICombineData<EmergencyGeneratorData>, ISerializable
{
	public int m_ElectricityProduction;

	public Bounds1 m_ActivationThreshold;

	public void Combine(EmergencyGeneratorData otherData)
	{
		m_ElectricityProduction += otherData.m_ElectricityProduction;
		m_ActivationThreshold = new Bounds1(math.max(otherData.m_ActivationThreshold.min, m_ActivationThreshold.min), math.max(otherData.m_ActivationThreshold.max, m_ActivationThreshold.max));
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int electricityProduction = m_ElectricityProduction;
		writer.Write(electricityProduction);
		float min = m_ActivationThreshold.min;
		writer.Write(min);
		float max = m_ActivationThreshold.max;
		writer.Write(max);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int electricityProduction = ref m_ElectricityProduction;
		reader.Read(out electricityProduction);
		ref float min = ref m_ActivationThreshold.min;
		reader.Read(out min);
		ref float max = ref m_ActivationThreshold.max;
		reader.Read(out max);
	}
}
