using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

[InternalBufferCapacity(8)]
public struct Efficiency : IBufferElementData, ISerializable, IComparable<Efficiency>
{
	public EfficiencyFactor m_Factor;

	public float m_Efficiency;

	public Efficiency(EfficiencyFactor factor, float efficiency)
	{
		m_Factor = factor;
		m_Efficiency = efficiency;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		EfficiencyFactor factor = m_Factor;
		writer.Write((byte)factor);
		float efficiency = m_Efficiency;
		writer.Write(efficiency);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_Factor = (EfficiencyFactor)value;
		ref float efficiency = ref m_Efficiency;
		reader.Read(out efficiency);
	}

	public int CompareTo(Efficiency other)
	{
		int num = other.m_Efficiency.CompareTo(m_Efficiency);
		if (num != 0)
		{
			return num;
		}
		return m_Factor.CompareTo(other.m_Factor);
	}
}
