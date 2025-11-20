using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct WaterTower : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_StoredWater;

	public int m_Polluted;

	public int m_LastStoredWater;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int storedWater = m_StoredWater;
		writer.Write(storedWater);
		int polluted = m_Polluted;
		writer.Write(polluted);
		int lastStoredWater = m_LastStoredWater;
		writer.Write(lastStoredWater);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int storedWater = ref m_StoredWater;
		reader.Read(out storedWater);
		ref int polluted = ref m_Polluted;
		reader.Read(out polluted);
		if (reader.context.version >= Version.waterSelectedInfoFix)
		{
			ref int lastStoredWater = ref m_LastStoredWater;
			reader.Read(out lastStoredWater);
		}
	}
}
