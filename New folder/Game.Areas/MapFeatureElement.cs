using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

[InternalBufferCapacity(9)]
public struct MapFeatureElement : IBufferElementData, ISerializable
{
	public float m_Amount;

	public float m_RenewalRate;

	public MapFeatureElement(float amount, float regenerationRate)
	{
		m_Amount = amount;
		m_RenewalRate = regenerationRate;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float amount = m_Amount;
		writer.Write(amount);
		float renewalRate = m_RenewalRate;
		writer.Write(renewalRate);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float amount = ref m_Amount;
		reader.Read(out amount);
		if (reader.context.version >= Version.naturalResourceRenewalRate)
		{
			ref float renewalRate = ref m_RenewalRate;
			reader.Read(out renewalRate);
		}
	}
}
