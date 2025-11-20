using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct ResourceProductionData : IBufferElementData, ISerializable
{
	public Resource m_Type;

	public int m_ProductionRate;

	public int m_StorageCapacity;

	public ResourceProductionData(Resource type, int productionRate, int storageCapacity)
	{
		m_Type = type;
		m_ProductionRate = productionRate;
		m_StorageCapacity = storageCapacity;
	}

	public static void Combine(NativeList<ResourceProductionData> resources, DynamicBuffer<ResourceProductionData> others)
	{
		for (int i = 0; i < others.Length; i++)
		{
			ResourceProductionData value = others[i];
			int num = 0;
			while (true)
			{
				if (num < resources.Length)
				{
					ResourceProductionData value2 = resources[num];
					if (value2.m_Type == value.m_Type)
					{
						value2.m_ProductionRate += value.m_ProductionRate;
						value2.m_StorageCapacity += value.m_StorageCapacity;
						resources[num] = value2;
						break;
					}
					num++;
					continue;
				}
				resources.Add(in value);
				break;
			}
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int productionRate = m_ProductionRate;
		writer.Write(productionRate);
		int storageCapacity = m_StorageCapacity;
		writer.Write(storageCapacity);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Type);
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int productionRate = ref m_ProductionRate;
		reader.Read(out productionRate);
		ref int storageCapacity = ref m_StorageCapacity;
		reader.Read(out storageCapacity);
		reader.Read(out sbyte value);
		m_Type = EconomyUtils.GetResource(value);
	}
}
