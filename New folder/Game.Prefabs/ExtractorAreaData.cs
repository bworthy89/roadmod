using Colossal.Serialization.Entities;
using Game.Areas;
using Unity.Entities;

namespace Game.Prefabs;

public struct ExtractorAreaData : IComponentData, IQueryTypeParameter, ISerializable
{
	public MapFeature m_MapFeature;

	public float m_ObjectSpawnFactor;

	public float m_MaxObjectArea;

	public bool m_RequireNaturalResource;

	public float m_WorkAmountFactor;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)m_MapFeature;
		writer.Write(value);
		bool requireNaturalResource = m_RequireNaturalResource;
		writer.Write(requireNaturalResource);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		ref bool requireNaturalResource = ref m_RequireNaturalResource;
		reader.Read(out requireNaturalResource);
		m_MapFeature = (MapFeature)value;
	}
}
