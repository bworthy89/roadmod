using Colossal.Serialization.Entities;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Areas;

public struct Extractor : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_ResourceAmount;

	public float m_MaxConcentration;

	public float m_ExtractedAmount;

	public float m_WorkAmount;

	public float m_HarvestedAmount;

	public float m_TotalExtracted;

	public VehicleWorkType m_WorkType;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float resourceAmount = m_ResourceAmount;
		writer.Write(resourceAmount);
		float maxConcentration = m_MaxConcentration;
		writer.Write(maxConcentration);
		float extractedAmount = m_ExtractedAmount;
		writer.Write(extractedAmount);
		float workAmount = m_WorkAmount;
		writer.Write(workAmount);
		float harvestedAmount = m_HarvestedAmount;
		writer.Write(harvestedAmount);
		VehicleWorkType workType = m_WorkType;
		writer.Write((int)workType);
		float totalExtracted = m_TotalExtracted;
		writer.Write(totalExtracted);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float resourceAmount = ref m_ResourceAmount;
		reader.Read(out resourceAmount);
		if (reader.context.version >= Version.resourceConcentration)
		{
			ref float maxConcentration = ref m_MaxConcentration;
			reader.Read(out maxConcentration);
		}
		if (reader.context.version >= Version.extractedResources)
		{
			ref float extractedAmount = ref m_ExtractedAmount;
			reader.Read(out extractedAmount);
		}
		if (reader.context.version >= Version.harvestedResources)
		{
			ref float workAmount = ref m_WorkAmount;
			reader.Read(out workAmount);
			ref float harvestedAmount = ref m_HarvestedAmount;
			reader.Read(out harvestedAmount);
			reader.Read(out uint value);
			m_WorkType = (VehicleWorkType)value;
		}
		if (reader.context.version >= Version.totalExtractedResources)
		{
			ref float totalExtracted = ref m_TotalExtracted;
			reader.Read(out totalExtracted);
		}
	}
}
