using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct AdditionalBuildingTerraformElement : IBufferElementData, ISerializable
{
	public Bounds2 m_Area;

	public float m_HeightOffset;

	public bool m_Circular;

	public bool m_DontRaise;

	public bool m_DontLower;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float2 min = m_Area.min;
		writer.Write(min);
		float2 max = m_Area.max;
		writer.Write(max);
		float heightOffset = m_HeightOffset;
		writer.Write(heightOffset);
		bool circular = m_Circular;
		writer.Write(circular);
		bool dontRaise = m_DontRaise;
		writer.Write(dontRaise);
		bool dontLower = m_DontLower;
		writer.Write(dontLower);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float2 min = ref m_Area.min;
		reader.Read(out min);
		ref float2 max = ref m_Area.max;
		reader.Read(out max);
		ref float heightOffset = ref m_HeightOffset;
		reader.Read(out heightOffset);
		ref bool circular = ref m_Circular;
		reader.Read(out circular);
		ref bool dontRaise = ref m_DontRaise;
		reader.Read(out dontRaise);
		if (reader.context.version >= Version.pillarTerrainModification)
		{
			ref bool dontLower = ref m_DontLower;
			reader.Read(out dontLower);
		}
	}
}
