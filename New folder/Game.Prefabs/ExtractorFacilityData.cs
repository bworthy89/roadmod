using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct ExtractorFacilityData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Bounds1 m_RotationRange;

	public Bounds1 m_HeightOffset;

	public ExtractorRequirementFlags m_Requirements;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float min = m_RotationRange.min;
		writer.Write(min);
		float max = m_RotationRange.max;
		writer.Write(max);
		float min2 = m_HeightOffset.min;
		writer.Write(min2);
		float max2 = m_HeightOffset.max;
		writer.Write(max2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float min = ref m_RotationRange.min;
		reader.Read(out min);
		ref float max = ref m_RotationRange.max;
		reader.Read(out max);
		ref float min2 = ref m_HeightOffset.min;
		reader.Read(out min2);
		ref float max2 = ref m_HeightOffset.max;
		reader.Read(out max2);
	}
}
