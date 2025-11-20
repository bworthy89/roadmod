using System;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class CityProductionStatisticSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct ProcessResourcesAccumulatorJob : IJobParallelFor
	{
		public NativeArray<CityResourceUsage> m_CityResourceUsage;

		public NativeArray<int> m_ConsumptionByCitizensAccumulator;

		public NativeArray<int> m_ConsumptionByBuildingUpkeepAccumulator;

		public NativeArray<int> m_ImportExportAccumulator;

		public NativeArray<int> m_IndustrialConsumptionAccumulator;

		public NativeArray<int> m_BuildingLevelUpAccumulator;

		public void Execute(int index)
		{
			CityResourceUsage value = m_CityResourceUsage[index];
			value.m_Citizens = m_CityResourceUsage[index].GetNextStepValue(CityResourceUsage.Consumer.Citizens, m_ConsumptionByCitizensAccumulator[index]);
			value.m_ServiceUpkeep = m_CityResourceUsage[index].GetNextStepValue(CityResourceUsage.Consumer.ServiceUpkeep, m_ConsumptionByBuildingUpkeepAccumulator[index]);
			value.m_ImportExport = m_CityResourceUsage[index].GetNextStepValue(CityResourceUsage.Consumer.ImportExport, m_ImportExportAccumulator[index]);
			value.m_Industrial = m_CityResourceUsage[index].GetNextStepValue(CityResourceUsage.Consumer.Industrial, m_IndustrialConsumptionAccumulator[index]);
			value.m_LevelUp = m_CityResourceUsage[index].GetNextStepValue(CityResourceUsage.Consumer.LevelUp, m_BuildingLevelUpAccumulator[index]);
			m_CityResourceUsage[index] = value;
			m_ConsumptionByCitizensAccumulator[index] = 0;
			m_ConsumptionByBuildingUpkeepAccumulator[index] = 0;
			m_ImportExportAccumulator[index] = 0;
			m_IndustrialConsumptionAccumulator[index] = 0;
			m_BuildingLevelUpAccumulator[index] = 0;
		}
	}

	[BurstCompile]
	private struct ProcessProductionConsumptionEventJob : IJob
	{
		public NativeQueue<CompanyProcessingEvent> m_Queue;

		public NativeParallelHashMap<ProductionChain, ProductionChainValue> m_ConsumptionProductChains;

		public NativeParallelHashMap<ProductionChain, ProductionChainValue> m_ConsumptionProductChainsAccumulator;

		public NativeArray<int2> m_ConsumptionProduction;

		public void Execute()
		{
			m_ConsumptionProductChainsAccumulator.Clear();
			CompanyProcessingEvent item;
			while (m_Queue.TryDequeue(out item))
			{
				item.m_Consume1Amount = math.abs(item.m_Consume1Amount);
				item.m_Consume2Amount = math.abs(item.m_Consume2Amount);
				item.m_ProduceAmount = math.abs(item.m_ProduceAmount);
				ProductionChain key = new ProductionChain
				{
					m_Consume1 = item.m_Consume1,
					m_Consume2 = item.m_Consume2,
					m_Produce = item.m_Produce
				};
				if (!m_ConsumptionProductChainsAccumulator.TryGetValue(key, out var item2))
				{
					m_ConsumptionProductChainsAccumulator.Add(key, new ProductionChainValue(item.m_Consume1Amount, item.m_Consume2Amount, item.m_ProduceAmount));
					continue;
				}
				item2.m_Consume1 += item.m_Consume1Amount;
				item2.m_Consume2 += item.m_Consume2Amount;
				item2.m_Produce += item.m_ProduceAmount;
				m_ConsumptionProductChainsAccumulator[key] = item2;
			}
			foreach (KeyValue<ProductionChain, ProductionChainValue> item4 in m_ConsumptionProductChainsAccumulator)
			{
				if (!m_ConsumptionProductChains.ContainsKey(item4.Key))
				{
					m_ConsumptionProductChains.Add(item4.Key, item4.Value);
				}
			}
			NativeArray<ProductionChain> keyArray = m_ConsumptionProductChains.GetKeyArray(Allocator.Temp);
			m_ConsumptionProduction.Fill(0);
			for (int i = 0; i < keyArray.Length; i++)
			{
				ProductionChain key2 = keyArray[i];
				ProductionChainValue value = m_ConsumptionProductChains[key2];
				if (!m_ConsumptionProductChainsAccumulator.TryGetValue(key2, out var item3))
				{
					item3 = new ProductionChainValue(0, 0, 0);
				}
				value.m_Consume1 = ((value.m_Consume1 == 0) ? item3.m_Consume1 : ((int)((float)kUpdatesPerDay * math.lerp((float)value.m_Consume1 / (float)kUpdatesPerDay, item3.m_Consume1, 0.5f))));
				value.m_Consume2 = ((value.m_Consume2 == 0) ? item3.m_Consume2 : ((int)((float)kUpdatesPerDay * math.lerp((float)value.m_Consume2 / (float)kUpdatesPerDay, item3.m_Consume2, 0.5f))));
				value.m_Produce = ((value.m_Produce == 0) ? item3.m_Produce : ((int)((float)kUpdatesPerDay * math.lerp((float)value.m_Produce / (float)kUpdatesPerDay, item3.m_Produce, 0.5f))));
				m_ConsumptionProductChains[key2] = value;
				int resourceIndex = EconomyUtils.GetResourceIndex(key2.m_Consume1);
				int resourceIndex2 = EconomyUtils.GetResourceIndex(key2.m_Consume2);
				int resourceIndex3 = EconomyUtils.GetResourceIndex(key2.m_Produce);
				if (resourceIndex >= 0)
				{
					m_ConsumptionProduction[resourceIndex] += new int2(value.m_Consume1, 0);
				}
				if (resourceIndex2 >= 0)
				{
					m_ConsumptionProduction[resourceIndex2] += new int2(value.m_Consume2, 0);
				}
				if (resourceIndex3 >= 0)
				{
					m_ConsumptionProduction[resourceIndex3] += new int2(0, value.m_Produce);
				}
			}
			keyArray.Dispose();
		}
	}

	public struct CompanyProcessingEvent : ISerializable
	{
		public Resource m_Consume1;

		public Resource m_Consume2;

		public Resource m_Produce;

		public int m_Consume1Amount;

		public int m_Consume2Amount;

		public int m_ProduceAmount;

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Consume1);
			writer.Write(value);
			sbyte value2 = (sbyte)EconomyUtils.GetResourceIndex(m_Consume2);
			writer.Write(value2);
			sbyte value3 = (sbyte)EconomyUtils.GetResourceIndex(m_Produce);
			writer.Write(value3);
			int consume1Amount = m_Consume1Amount;
			writer.Write(consume1Amount);
			int consume2Amount = m_Consume2Amount;
			writer.Write(consume2Amount);
			int produceAmount = m_ProduceAmount;
			writer.Write(produceAmount);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			reader.Read(out sbyte value);
			reader.Read(out sbyte value2);
			reader.Read(out sbyte value3);
			m_Consume1 = EconomyUtils.GetResource(value);
			m_Consume2 = EconomyUtils.GetResource(value2);
			m_Produce = EconomyUtils.GetResource(value3);
			ref int consume1Amount = ref m_Consume1Amount;
			reader.Read(out consume1Amount);
			ref int consume2Amount = ref m_Consume2Amount;
			reader.Read(out consume2Amount);
			ref int produceAmount = ref m_ProduceAmount;
			reader.Read(out produceAmount);
		}
	}

	public struct ProductionChain : IEquatable<ProductionChain>, ISerializable
	{
		public Resource m_Consume1;

		public Resource m_Consume2;

		public Resource m_Produce;

		public bool Equals(ProductionChain other)
		{
			if (m_Consume1 == other.m_Consume1 && m_Consume2 == other.m_Consume2)
			{
				return m_Produce == other.m_Produce;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is ProductionChain other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (int)math.hash(new int3((int)m_Consume1, (int)m_Consume2, (int)m_Produce));
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Consume1);
			writer.Write(value);
			sbyte value2 = (sbyte)EconomyUtils.GetResourceIndex(m_Consume2);
			writer.Write(value2);
			sbyte value3 = (sbyte)EconomyUtils.GetResourceIndex(m_Produce);
			writer.Write(value3);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			reader.Read(out sbyte value);
			reader.Read(out sbyte value2);
			reader.Read(out sbyte value3);
			m_Consume1 = EconomyUtils.GetResource(value);
			m_Consume2 = EconomyUtils.GetResource(value2);
			m_Produce = EconomyUtils.GetResource(value3);
		}
	}

	public struct ProductionChainValue : ISerializable
	{
		public int m_Consume1;

		public int m_Consume2;

		public int m_Produce;

		public ProductionChainValue(int itemConsume1Amount, int itemConsume2Amount, int itemProduceAmount)
		{
			m_Consume1 = itemConsume1Amount;
			m_Consume2 = itemConsume2Amount;
			m_Produce = itemProduceAmount;
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			int consume = m_Consume1;
			writer.Write(consume);
			int consume2 = m_Consume2;
			writer.Write(consume2);
			int produce = m_Produce;
			writer.Write(produce);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref int consume = ref m_Consume1;
			reader.Read(out consume);
			ref int consume2 = ref m_Consume2;
			reader.Read(out consume2);
			ref int produce = ref m_Produce;
			reader.Read(out produce);
		}
	}

	public struct CityResourceUsage : ISerializable
	{
		public enum Consumer
		{
			ServiceUpkeep,
			Citizens,
			ImportExport,
			Retail,
			Commercial,
			Industrial,
			Office,
			Heating,
			LevelUp,
			Count
		}

		public int m_ServiceUpkeep;

		public int m_Citizens;

		public int m_ImportExport;

		public int m_Retail;

		public int m_Commercial;

		public int m_Industrial;

		public int m_Office;

		public int m_Heating;

		public int m_LevelUp;

		public int this[int index]
		{
			get
			{
				return this[(Consumer)index];
			}
			set
			{
				this[(Consumer)index] = value;
			}
		}

		public int this[Consumer index]
		{
			get
			{
				return index switch
				{
					Consumer.ServiceUpkeep => m_ServiceUpkeep, 
					Consumer.Citizens => m_Citizens, 
					Consumer.ImportExport => m_ImportExport, 
					Consumer.Retail => m_Retail, 
					Consumer.Commercial => m_Commercial, 
					Consumer.Industrial => m_Industrial, 
					Consumer.Office => m_Office, 
					Consumer.Heating => m_Heating, 
					Consumer.LevelUp => m_LevelUp, 
					_ => 0, 
				};
			}
			set
			{
				switch (index)
				{
				case Consumer.ServiceUpkeep:
					m_ServiceUpkeep = value;
					break;
				case Consumer.Citizens:
					m_Citizens = value;
					break;
				case Consumer.ImportExport:
					m_ImportExport = value;
					break;
				case Consumer.Retail:
					m_Retail = value;
					break;
				case Consumer.Commercial:
					m_Commercial = value;
					break;
				case Consumer.Industrial:
					m_Industrial = value;
					break;
				case Consumer.Office:
					m_Office = value;
					break;
				case Consumer.Heating:
					m_Heating = value;
					break;
				case Consumer.LevelUp:
					m_LevelUp = value;
					break;
				}
			}
		}

		public int GetNextStepValue(Consumer index, int value)
		{
			int num = this[index];
			if (num != 0)
			{
				return (int)((float)kUpdatesPerDay * math.lerp((float)num / (float)kUpdatesPerDay, value, 0.5f));
			}
			return value;
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			int serviceUpkeep = m_ServiceUpkeep;
			writer.Write(serviceUpkeep);
			int citizens = m_Citizens;
			writer.Write(citizens);
			int importExport = m_ImportExport;
			writer.Write(importExport);
			int retail = m_Retail;
			writer.Write(retail);
			int commercial = m_Commercial;
			writer.Write(commercial);
			int industrial = m_Industrial;
			writer.Write(industrial);
			int office = m_Office;
			writer.Write(office);
			int heating = m_Heating;
			writer.Write(heating);
			int levelUp = m_LevelUp;
			writer.Write(levelUp);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref int serviceUpkeep = ref m_ServiceUpkeep;
			reader.Read(out serviceUpkeep);
			ref int citizens = ref m_Citizens;
			reader.Read(out citizens);
			ref int importExport = ref m_ImportExport;
			reader.Read(out importExport);
			ref int retail = ref m_Retail;
			reader.Read(out retail);
			ref int commercial = ref m_Commercial;
			reader.Read(out commercial);
			ref int industrial = ref m_Industrial;
			reader.Read(out industrial);
			ref int office = ref m_Office;
			reader.Read(out office);
			ref int heating = ref m_Heating;
			reader.Read(out heating);
			if (reader.context.format.Has(FormatTags.LevelUpStatistics))
			{
				ref int levelUp = ref m_LevelUp;
				reader.Read(out levelUp);
			}
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private NativeArray<CityResourceUsage> m_CityResourceUsage;

	private NativeArray<CityResourceUsage> m_CityResourceUsageTemp;

	private NativeArray<int2> m_ConsumptionProduction;

	private NativeArray<int2> m_ConsumptionProductionTemp;

	private NativeArray<int> m_ConsumptionByCitizensAccumulator;

	private NativeArray<int> m_ConsumptionByServiceUpkeepAccumulator;

	private NativeArray<int> m_ImportExportsAccumulator;

	private NativeArray<int> m_IndustrialConsumptionAccumulator;

	private NativeArray<int> m_BuildingLevelUpAccumulator;

	private NativeParallelHashMap<ProductionChain, ProductionChainValue> m_ConsumptionProductionChains;

	private NativeParallelHashMap<ProductionChain, ProductionChainValue> m_ConsumptionProductionChainsTemp;

	private NativeParallelHashMap<ProductionChain, ProductionChainValue> m_ConsumptionProductionChainsAccumulator;

	private NativeQueue<CompanyProcessingEvent> m_ProcessingEventQueue;

	private JobHandle m_WriteChainQueueDeps;

	private NativeArray<JobHandle> m_WriteAccumulatorDeps;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public NativeParallelHashMap<ProductionChain, ProductionChainValue> GetProductionChains()
	{
		return m_ConsumptionProductionChainsTemp;
	}

	public NativeArray<int2> GetConsumptionProductions()
	{
		return m_ConsumptionProductionTemp;
	}

	public NativeArray<CityResourceUsage> GetCityResourceUsages()
	{
		return m_CityResourceUsageTemp;
	}

	public int GetCityResourceUsages(CityResourceUsage.Consumer consumer, Resource resource)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(resource);
		return m_CityResourceUsageTemp[resourceIndex][consumer];
	}

	public NativeQueue<CompanyProcessingEvent> GetConsumptionQueue(out JobHandle deps)
	{
		deps = m_WriteChainQueueDeps;
		return m_ProcessingEventQueue;
	}

	public void AddChainWriter(JobHandle deps)
	{
		m_WriteChainQueueDeps = JobHandle.CombineDependencies(m_WriteChainQueueDeps, deps);
	}

	public NativeArray<int> GetCityResourceUsageAccumulator(CityResourceUsage.Consumer consumer, out JobHandle deps)
	{
		deps = m_WriteAccumulatorDeps[(int)consumer];
		return consumer switch
		{
			CityResourceUsage.Consumer.Citizens => m_ConsumptionByCitizensAccumulator, 
			CityResourceUsage.Consumer.ServiceUpkeep => m_ConsumptionByServiceUpkeepAccumulator, 
			CityResourceUsage.Consumer.ImportExport => m_ImportExportsAccumulator, 
			CityResourceUsage.Consumer.Industrial => m_IndustrialConsumptionAccumulator, 
			CityResourceUsage.Consumer.LevelUp => m_BuildingLevelUpAccumulator, 
			_ => throw new NotImplementedException(), 
		};
	}

	public void AddCityUsageAccumulatorWriter(CityResourceUsage.Consumer consumer, JobHandle deps)
	{
		m_WriteAccumulatorDeps[(int)consumer] = JobHandle.CombineDependencies(m_WriteAccumulatorDeps[(int)consumer], deps);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		NativeArray<int2> consumptionProduction = m_ConsumptionProduction;
		writer.Write(consumptionProduction);
		NativeArray<CityResourceUsage> cityResourceUsage = m_CityResourceUsage;
		writer.Write(cityResourceUsage);
		NativeArray<int> consumptionByCitizensAccumulator = m_ConsumptionByCitizensAccumulator;
		writer.Write(consumptionByCitizensAccumulator);
		NativeArray<int> consumptionByServiceUpkeepAccumulator = m_ConsumptionByServiceUpkeepAccumulator;
		writer.Write(consumptionByServiceUpkeepAccumulator);
		NativeArray<int> importExportsAccumulator = m_ImportExportsAccumulator;
		writer.Write(importExportsAccumulator);
		NativeArray<ProductionChain> keyArray = m_ConsumptionProductionChains.GetKeyArray(Allocator.Temp);
		NativeArray<ProductionChainValue> valueArray = m_ConsumptionProductionChains.GetValueArray(Allocator.Temp);
		int length = keyArray.Length;
		writer.Write(length);
		NativeArray<ProductionChain> value = keyArray;
		writer.Write(value);
		NativeArray<ProductionChainValue> value2 = valueArray;
		writer.Write(value2);
		keyArray.Dispose();
		valueArray.Dispose();
		NativeArray<CompanyProcessingEvent> nativeArray = m_ProcessingEventQueue.ToArray(Allocator.Temp);
		int length2 = nativeArray.Length;
		writer.Write(length2);
		NativeArray<CompanyProcessingEvent> value3 = nativeArray;
		writer.Write(value3);
		nativeArray.Dispose();
		NativeArray<int> industrialConsumptionAccumulator = m_IndustrialConsumptionAccumulator;
		writer.Write(industrialConsumptionAccumulator);
		NativeArray<int> buildingLevelUpAccumulator = m_BuildingLevelUpAccumulator;
		writer.Write(buildingLevelUpAccumulator);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		NativeArray<int2> consumptionProduction = m_ConsumptionProduction;
		reader.Read(consumptionProduction);
		if (!reader.context.format.Has(FormatTags.RemoveConsumptionAccumulator))
		{
			NativeArray<int2> nativeArray = new NativeArray<int2>(m_ConsumptionProduction.Length, Allocator.Temp);
			NativeArray<int2> value = nativeArray;
			reader.Read(value);
			nativeArray.Dispose();
		}
		NativeArray<CityResourceUsage> cityResourceUsage = m_CityResourceUsage;
		reader.Read(cityResourceUsage);
		NativeArray<int> consumptionByCitizensAccumulator = m_ConsumptionByCitizensAccumulator;
		reader.Read(consumptionByCitizensAccumulator);
		NativeArray<int> consumptionByServiceUpkeepAccumulator = m_ConsumptionByServiceUpkeepAccumulator;
		reader.Read(consumptionByServiceUpkeepAccumulator);
		NativeArray<int> importExportsAccumulator = m_ImportExportsAccumulator;
		reader.Read(importExportsAccumulator);
		reader.Read(out int value2);
		NativeArray<ProductionChain> nativeArray2 = new NativeArray<ProductionChain>(value2, Allocator.Temp);
		NativeArray<ProductionChain> value3 = nativeArray2;
		reader.Read(value3);
		NativeArray<ProductionChainValue> nativeArray3 = new NativeArray<ProductionChainValue>(value2, Allocator.Temp);
		NativeArray<ProductionChainValue> value4 = nativeArray3;
		reader.Read(value4);
		m_ConsumptionProductionChains.Clear();
		m_ConsumptionProductionChainsAccumulator.Clear();
		for (int i = 0; i < nativeArray2.Length; i++)
		{
			m_ConsumptionProductionChains.Add(nativeArray2[i], nativeArray3[i]);
		}
		nativeArray2.Dispose();
		nativeArray3.Dispose();
		reader.Read(out int value5);
		NativeArray<CompanyProcessingEvent> nativeArray4 = new NativeArray<CompanyProcessingEvent>(value5, Allocator.Temp);
		NativeArray<CompanyProcessingEvent> value6 = nativeArray4;
		reader.Read(value6);
		for (int j = 0; j < nativeArray4.Length; j++)
		{
			m_ProcessingEventQueue.Enqueue(nativeArray4[j]);
		}
		nativeArray4.Dispose();
		m_ConsumptionProductionChainsTemp.Clear();
		foreach (KeyValue<ProductionChain, ProductionChainValue> consumptionProductionChain in m_ConsumptionProductionChains)
		{
			m_ConsumptionProductionChainsTemp.Add(consumptionProductionChain.Key, consumptionProductionChain.Value);
		}
		if (reader.context.format.Has(FormatTags.IndustrialConsumptionAccumulator))
		{
			NativeArray<int> industrialConsumptionAccumulator = m_IndustrialConsumptionAccumulator;
			reader.Read(industrialConsumptionAccumulator);
		}
		else
		{
			m_IndustrialConsumptionAccumulator.Fill(0);
		}
		if (reader.context.format.Has(FormatTags.LevelUpStatistics))
		{
			NativeArray<int> buildingLevelUpAccumulator = m_BuildingLevelUpAccumulator;
			reader.Read(buildingLevelUpAccumulator);
		}
		else
		{
			m_BuildingLevelUpAccumulator.Fill(0);
		}
		m_ConsumptionProductionTemp.CopyFrom(m_ConsumptionProduction);
		m_CityResourceUsageTemp.CopyFrom(m_CityResourceUsage);
	}

	public void SetDefaults(Context context)
	{
		m_ConsumptionProduction.Fill(0);
		m_ConsumptionProductionTemp.Fill(0);
		m_CityResourceUsage.Fill(default(CityResourceUsage));
		m_CityResourceUsageTemp.Fill(default(CityResourceUsage));
		m_ConsumptionByCitizensAccumulator.Fill(0);
		m_ConsumptionByServiceUpkeepAccumulator.Fill(0);
		m_ImportExportsAccumulator.Fill(0);
		m_IndustrialConsumptionAccumulator.Fill(0);
		m_BuildingLevelUpAccumulator.Fill(0);
		m_ConsumptionProductionChainsTemp.Clear();
		m_ConsumptionProductionChains.Clear();
		m_ConsumptionProductionChainsAccumulator.Clear();
		m_ProcessingEventQueue.Clear();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		int resourceCount = EconomyUtils.ResourceCount;
		m_ConsumptionProduction = new NativeArray<int2>(resourceCount, Allocator.Persistent);
		m_ConsumptionProductionTemp = new NativeArray<int2>(resourceCount, Allocator.Persistent);
		m_CityResourceUsage = new NativeArray<CityResourceUsage>(resourceCount, Allocator.Persistent);
		m_CityResourceUsageTemp = new NativeArray<CityResourceUsage>(resourceCount, Allocator.Persistent);
		m_ConsumptionByCitizensAccumulator = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ConsumptionByServiceUpkeepAccumulator = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ImportExportsAccumulator = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_IndustrialConsumptionAccumulator = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_BuildingLevelUpAccumulator = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ConsumptionProductionChains = new NativeParallelHashMap<ProductionChain, ProductionChainValue>(100, Allocator.Persistent);
		m_ConsumptionProductionChainsTemp = new NativeParallelHashMap<ProductionChain, ProductionChainValue>(100, Allocator.Persistent);
		m_ConsumptionProductionChainsAccumulator = new NativeParallelHashMap<ProductionChain, ProductionChainValue>(100, Allocator.Persistent);
		m_ProcessingEventQueue = new NativeQueue<CompanyProcessingEvent>(Allocator.Persistent);
		m_WriteAccumulatorDeps = new NativeArray<JobHandle>(9, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_ConsumptionProduction.Dispose();
		m_ConsumptionProductionTemp.Dispose();
		m_ConsumptionProductionChains.Dispose();
		m_ConsumptionProductionChainsTemp.Dispose();
		m_ConsumptionProductionChainsAccumulator.Dispose();
		m_ProcessingEventQueue.Dispose();
		m_CityResourceUsage.Dispose();
		m_CityResourceUsageTemp.Dispose();
		m_ConsumptionByCitizensAccumulator.Dispose();
		m_ConsumptionByServiceUpkeepAccumulator.Dispose();
		m_ImportExportsAccumulator.Dispose();
		m_IndustrialConsumptionAccumulator.Dispose();
		m_BuildingLevelUpAccumulator.Dispose();
		m_WriteAccumulatorDeps.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_ConsumptionProductionChainsTemp.Clear();
		foreach (KeyValue<ProductionChain, ProductionChainValue> consumptionProductionChain in m_ConsumptionProductionChains)
		{
			m_ConsumptionProductionChainsTemp.Add(consumptionProductionChain.Key, consumptionProductionChain.Value);
		}
		m_ConsumptionProductionTemp.CopyFrom(m_ConsumptionProduction);
		m_CityResourceUsageTemp.CopyFrom(m_CityResourceUsage);
		JobHandle job = new ProcessProductionConsumptionEventJob
		{
			m_Queue = m_ProcessingEventQueue,
			m_ConsumptionProductChains = m_ConsumptionProductionChains,
			m_ConsumptionProduction = m_ConsumptionProduction,
			m_ConsumptionProductChainsAccumulator = m_ConsumptionProductionChainsAccumulator
		}.Schedule(JobHandle.CombineDependencies(base.Dependency, m_WriteChainQueueDeps));
		JobHandle job2 = IJobParallelForExtensions.Schedule(new ProcessResourcesAccumulatorJob
		{
			m_CityResourceUsage = m_CityResourceUsage,
			m_ConsumptionByCitizensAccumulator = m_ConsumptionByCitizensAccumulator,
			m_ConsumptionByBuildingUpkeepAccumulator = m_ConsumptionByServiceUpkeepAccumulator,
			m_ImportExportAccumulator = m_ImportExportsAccumulator,
			m_IndustrialConsumptionAccumulator = m_IndustrialConsumptionAccumulator,
			m_BuildingLevelUpAccumulator = m_BuildingLevelUpAccumulator
		}, m_CityResourceUsage.Length, 10, JobHandle.CombineDependencies(base.Dependency, JobHandle.CombineDependencies(m_WriteAccumulatorDeps)));
		base.Dependency = JobHandle.CombineDependencies(job, job2);
		AddChainWriter(base.Dependency);
		for (int i = 0; i < 9; i++)
		{
			AddCityUsageAccumulatorWriter((CityResourceUsage.Consumer)i, base.Dependency);
		}
	}

	[Preserve]
	public CityProductionStatisticSystem()
	{
	}
}
