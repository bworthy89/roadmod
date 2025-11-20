using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct FireEngineData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_ExtinguishingRate;

	public float m_ExtinguishingSpread;

	public float m_ExtinguishingCapacity;

	public float m_DestroyedClearDuration;

	public FireEngineData(float extinguishingRate, float extinguishingSpread, float extinguishingCapacity, float destroyedClearDuration)
	{
		m_ExtinguishingRate = extinguishingRate;
		m_ExtinguishingSpread = extinguishingSpread;
		m_ExtinguishingCapacity = extinguishingCapacity;
		m_DestroyedClearDuration = destroyedClearDuration;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float extinguishingRate = m_ExtinguishingRate;
		writer.Write(extinguishingRate);
		float extinguishingSpread = m_ExtinguishingSpread;
		writer.Write(extinguishingSpread);
		float extinguishingCapacity = m_ExtinguishingCapacity;
		writer.Write(extinguishingCapacity);
		float destroyedClearDuration = m_DestroyedClearDuration;
		writer.Write(destroyedClearDuration);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float extinguishingRate = ref m_ExtinguishingRate;
		reader.Read(out extinguishingRate);
		ref float extinguishingSpread = ref m_ExtinguishingSpread;
		reader.Read(out extinguishingSpread);
		ref float extinguishingCapacity = ref m_ExtinguishingCapacity;
		reader.Read(out extinguishingCapacity);
		ref float destroyedClearDuration = ref m_DestroyedClearDuration;
		reader.Read(out destroyedClearDuration);
	}
}
