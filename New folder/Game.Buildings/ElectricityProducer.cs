using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct ElectricityProducer : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Capacity;

	public int m_LastProduction;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int capacity = m_Capacity;
		writer.Write(capacity);
		int lastProduction = m_LastProduction;
		writer.Write(lastProduction);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int capacity = ref m_Capacity;
		reader.Read(out capacity);
		if (reader.context.version >= Version.powerPlantConsumption && reader.context.version < Version.serviceConsumption)
		{
			reader.Read(out int _);
		}
		if (reader.context.version >= Version.powerPlantLastFlow)
		{
			ref int lastProduction = ref m_LastProduction;
			reader.Read(out lastProduction);
		}
	}
}
