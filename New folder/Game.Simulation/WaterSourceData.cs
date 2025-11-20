using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct WaterSourceData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_ConstantDepth;

	public float m_Radius;

	public float m_Height;

	public float m_Multiplier;

	public float m_Polluted;

	public int m_id;

	public float m_modifier;

	public int SourceNameId { get; set; }

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int constantDepth = m_ConstantDepth;
		writer.Write(constantDepth);
		float height = m_Height;
		writer.Write(height);
		float radius = m_Radius;
		writer.Write(radius);
		float multiplier = m_Multiplier;
		writer.Write(multiplier);
		float polluted = m_Polluted;
		writer.Write(polluted);
		int id = m_id;
		writer.Write(id);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (!reader.context.format.Has(FormatTags.NewWaterSources))
		{
			ref int constantDepth = ref m_ConstantDepth;
			reader.Read(out constantDepth);
			ref float height = ref m_Height;
			reader.Read(out height);
			ref float radius = ref m_Radius;
			reader.Read(out radius);
			ref float multiplier = ref m_Multiplier;
			reader.Read(out multiplier);
			if (reader.context.version >= Version.waterPollution)
			{
				ref float polluted = ref m_Polluted;
				reader.Read(out polluted);
			}
			m_id = -1;
		}
		else
		{
			ref int constantDepth2 = ref m_ConstantDepth;
			reader.Read(out constantDepth2);
			ref float height2 = ref m_Height;
			reader.Read(out height2);
			ref float radius2 = ref m_Radius;
			reader.Read(out radius2);
			ref float multiplier2 = ref m_Multiplier;
			reader.Read(out multiplier2);
			ref float polluted2 = ref m_Polluted;
			reader.Read(out polluted2);
			ref int id = ref m_id;
			reader.Read(out id);
		}
		m_modifier = 1f;
	}
}
