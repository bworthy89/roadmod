using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct BuildingTerraformData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_FlatX0;

	public float3 m_FlatZ0;

	public float3 m_FlatX1;

	public float3 m_FlatZ1;

	public float4 m_Smooth;

	public float m_HeightOffset;

	public bool m_DontRaise;

	public bool m_DontLower;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 flatX = m_FlatX0;
		writer.Write(flatX);
		float3 flatZ = m_FlatZ0;
		writer.Write(flatZ);
		float3 flatX2 = m_FlatX1;
		writer.Write(flatX2);
		float3 flatZ2 = m_FlatZ1;
		writer.Write(flatZ2);
		float4 smooth = m_Smooth;
		writer.Write(smooth);
		float heightOffset = m_HeightOffset;
		writer.Write(heightOffset);
		bool dontRaise = m_DontRaise;
		writer.Write(dontRaise);
		bool dontLower = m_DontLower;
		writer.Write(dontLower);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 flatX = ref m_FlatX0;
		reader.Read(out flatX);
		ref float3 flatZ = ref m_FlatZ0;
		reader.Read(out flatZ);
		ref float3 flatX2 = ref m_FlatX1;
		reader.Read(out flatX2);
		ref float3 flatZ2 = ref m_FlatZ1;
		reader.Read(out flatZ2);
		ref float4 smooth = ref m_Smooth;
		reader.Read(out smooth);
		if (reader.context.version >= Version.pillarTerrainModification)
		{
			ref float heightOffset = ref m_HeightOffset;
			reader.Read(out heightOffset);
			ref bool dontRaise = ref m_DontRaise;
			reader.Read(out dontRaise);
			ref bool dontLower = ref m_DontLower;
			reader.Read(out dontLower);
		}
	}
}
