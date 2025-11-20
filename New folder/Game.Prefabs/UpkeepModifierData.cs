using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public struct UpkeepModifierData : IBufferElementData, ICombineBuffer<UpkeepModifierData>, ISerializable
{
	public Resource m_Resource;

	public float m_Multiplier;

	public float Transform(float upkeep)
	{
		upkeep *= m_Multiplier;
		return upkeep;
	}

	public void Combine(NativeList<UpkeepModifierData> result)
	{
		for (int i = 0; i < result.Length; i++)
		{
			ref UpkeepModifierData reference = ref result.ElementAt(i);
			if (reference.m_Resource == m_Resource)
			{
				reference.m_Multiplier *= m_Multiplier;
				return;
			}
		}
		result.Add(in this);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float multiplier = m_Multiplier;
		writer.Write(multiplier);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float multiplier = ref m_Multiplier;
		reader.Read(out multiplier);
		if (reader.context.version < Version.upkeepModifierRelative)
		{
			reader.Read(out float _);
		}
		reader.Read(out sbyte value2);
		m_Resource = EconomyUtils.GetResource(value2);
	}
}
