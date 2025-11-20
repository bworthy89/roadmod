using Colossal.Serialization.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Game.Simulation;

public struct AvailabilityInfoCell : IAvailabilityInfoCell, IStrideSerializable, ISerializable
{
	public float4 m_AvailabilityInfo;

	public void AddAttractiveness(float amount)
	{
		m_AvailabilityInfo.x += amount;
	}

	public void AddConsumers(float amount)
	{
		m_AvailabilityInfo.y += amount;
	}

	public void AddWorkplaces(float amount)
	{
		m_AvailabilityInfo.z += amount;
	}

	public void AddServices(float amount)
	{
		m_AvailabilityInfo.w += amount;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_AvailabilityInfo);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_AvailabilityInfo);
	}

	public int GetStride(Context context)
	{
		return UnsafeUtility.SizeOf<float4>();
	}
}
