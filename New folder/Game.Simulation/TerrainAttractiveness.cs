using Colossal.Serialization.Entities;

namespace Game.Simulation;

public struct TerrainAttractiveness : IStrideSerializable, ISerializable
{
	public float m_ShoreBonus;

	public float m_ForestBonus;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float shoreBonus = m_ShoreBonus;
		writer.Write(shoreBonus);
		float forestBonus = m_ForestBonus;
		writer.Write(forestBonus);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float shoreBonus = ref m_ShoreBonus;
		reader.Read(out shoreBonus);
		ref float forestBonus = ref m_ForestBonus;
		reader.Read(out forestBonus);
	}

	public int GetStride(Context context)
	{
		return 8;
	}
}
