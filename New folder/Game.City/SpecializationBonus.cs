using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct SpecializationBonus : IBufferElementData, IDefaultSerializable, ISerializable
{
	public int m_Value;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Value);
	}

	public float GetBonus(float maxBonus, int coefficient)
	{
		return maxBonus * (float)m_Value / (float)(m_Value + coefficient);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Value);
	}

	public void SetDefaults(Context context)
	{
		m_Value = 0;
	}
}
