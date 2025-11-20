using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ProductionUISystem : UISystemBase
{
	public struct FinalConsumptionQuery : IJsonReadable, IJsonWritable
	{
		public Entity resource;

		public CityProductionStatisticSystem.CityResourceUsage.Consumer consumer;

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("resource");
			reader.Read(out resource);
			reader.ReadProperty("consumer");
			reader.Read(out int value);
			consumer = (CityProductionStatisticSystem.CityResourceUsage.Consumer)value;
			reader.ReadMapEnd();
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(FinalConsumptionQuery).FullName);
			writer.PropertyName("resource");
			writer.Write(resource);
			writer.PropertyName("consumer");
			writer.Write((int)consumer);
			writer.TypeEnd();
		}
	}

	public struct FinalConsumption : IJsonWritable
	{
		public CityProductionStatisticSystem.CityResourceUsage.Consumer consumer;

		public int consumption;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(FinalConsumption).FullName);
			writer.PropertyName("consumer");
			writer.Write((int)consumer);
			writer.PropertyName("consumption");
			writer.Write(consumption);
			writer.TypeEnd();
		}
	}

	public struct ResourceValue : IJsonWritable
	{
		public Entity entity;

		public Resource resource;

		public int value;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(ResourceValue).FullName);
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("resource");
			writer.Write(resource.ToString());
			writer.PropertyName("value");
			writer.Write(value);
			writer.TypeEnd();
		}
	}

	public struct ProductionChainData : IJsonWritable
	{
		public ResourceValue consume1;

		public ResourceValue consume2;

		public ResourceValue produce;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(ProductionChainData).FullName);
			writer.PropertyName("consume1");
			writer.Write(consume1);
			writer.PropertyName("consume2");
			writer.Write(consume2);
			writer.PropertyName("produce");
			writer.Write(produce);
			writer.TypeEnd();
		}
	}

	public struct ProductionChainValue : IJsonWritable
	{
		public int consume1;

		public int consume2;

		public int produce;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(ProductionChainValue).FullName);
			writer.PropertyName("consume1");
			writer.Write(consume1);
			writer.PropertyName("consume2");
			writer.Write(consume2);
			writer.PropertyName("produce");
			writer.Write((ulong)produce);
			writer.TypeEnd();
		}

		public static implicit operator ProductionChainValue(CityProductionStatisticSystem.ProductionChainValue productionChain)
		{
			return new ProductionChainValue
			{
				consume1 = productionChain.m_Consume1,
				consume2 = productionChain.m_Consume2,
				produce = productionChain.m_Produce
			};
		}

		public static implicit operator CityProductionStatisticSystem.ProductionChainValue(ProductionChainValue productionChain)
		{
			return new CityProductionStatisticSystem.ProductionChainValue
			{
				m_Consume1 = productionChain.consume1,
				m_Consume2 = productionChain.consume2,
				m_Produce = productionChain.produce
			};
		}
	}

	private const string kGroup = "production";

	private UIUpdateState m_UpdateState;

	private PrefabSystem m_PrefabSystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	private CountCityStoredResourceSystem m_CountCityStoredResourceSystem;

	private CityProductionCapacityCalculationSystem m_CityProductionCapacityCalculationSystem;

	private EntityQuery m_ResourceCategoryQuery;

	private EntityQuery m_IndustrialCompanyQuery;

	private EntityQuery m_CommercialCompanyQuery;

	private EntityQuery m_ServiceUpkeepQuery;

	private NativeParallelMultiHashMap<Entity, (Entity, Entity)> m_ProductionChain;

	private GetterValueBinding<int> m_MaxProgressBinding;

	private RawValueBinding m_ResourceCategoriesBinding;

	private RawValueBinding m_ProductionChainDataBinding;

	private RawMapBinding<Entity> m_ResourceDetailsBinding;

	private RawMapBinding<Entity> m_ResourceBinding;

	private RawMapBinding<Entity> m_ServiceBinding;

	private RawMapBinding<Entity> m_ServiceUpkeepConsumptionBinding;

	private RawMapBinding<Entity> m_ImportExportBinding;

	private RawMapBinding<Entity> m_ConsumptionProductionBinding;

	private RawMapBinding<Entity> m_StoredResourceBinding;

	private RawMapBinding<FinalConsumptionQuery> m_FinalConsumptionBinding;

	private RawMapBinding<Entity> m_MaxProductionBinding;

	private ResourcePrefabs m_ResourcePrefabs;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateState = UIUpdateState.Create(base.World, 256);
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_CountCityStoredResourceSystem = base.World.GetOrCreateSystemManaged<CountCityStoredResourceSystem>();
		m_CityProductionCapacityCalculationSystem = base.World.GetOrCreateSystemManaged<CityProductionCapacityCalculationSystem>();
		m_ResourceCategoryQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<UIGroupElement>(), ComponentType.ReadOnly<UIResourceCategoryData>(), ComponentType.ReadOnly<UIObjectData>());
		m_IndustrialCompanyQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.Exclude<ServiceCompanyData>(), ComponentType.Exclude<StorageCompanyData>());
		m_CommercialCompanyQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<ServiceCompanyData>(), ComponentType.Exclude<StorageCompanyData>());
		m_ServiceUpkeepQuery = GetEntityQuery(ComponentType.ReadWrite<ServiceUpkeepData>(), ComponentType.ReadOnly<ServiceObjectData>());
		AddBinding(m_MaxProgressBinding = new GetterValueBinding<int>("production", "maxProgress", GetMaxProgress));
		AddBinding(m_ResourceCategoriesBinding = new RawValueBinding("production", "resourceCategories", WriteResourceCategories));
		AddBinding(m_ResourceBinding = new RawMapBinding<Entity>("production", "resources", WriteResource));
		AddBinding(m_ResourceDetailsBinding = new RawMapBinding<Entity>("production", "resourceDetails", WriteResourceDetails));
		AddBinding(m_ServiceBinding = new RawMapBinding<Entity>("production", "services", WriteService));
		AddBinding(m_ProductionChainDataBinding = new RawValueBinding("production", "productionChainData", WriteProductionChainData));
		AddBinding(m_ServiceUpkeepConsumptionBinding = new RawMapBinding<Entity>("production", "serviceUpkeepConsumption", WriteServiceUpkeepConsumption));
		AddBinding(m_ConsumptionProductionBinding = new RawMapBinding<Entity>("production", "consumptionProduction", WriteConsumptionProduction));
		AddBinding(m_ImportExportBinding = new RawMapBinding<Entity>("production", "importExport", WriteImportExport));
		AddBinding(m_StoredResourceBinding = new RawMapBinding<Entity>("production", "storedResource", WriteStoredResource));
		AddBinding(m_MaxProductionBinding = new RawMapBinding<Entity>("production", "maxProduction", WriteMaxProduction));
		AddBinding(m_FinalConsumptionBinding = new RawMapBinding<FinalConsumptionQuery>("production", "finalConsumption", WriteFinalConsumption, new ValueReader<FinalConsumptionQuery>(), new ValueWriter<FinalConsumptionQuery>()));
		m_ProductionChain = new NativeParallelMultiHashMap<Entity, (Entity, Entity)>(50, Allocator.Persistent);
		m_ResourcePrefabs = base.World.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (GameManager.instance.gameMode == GameMode.Game)
		{
			BuildProductionChain(m_ProductionChain);
			m_MaxProgressBinding.Update();
			m_ResourceCategoriesBinding.Update();
			m_ResourceBinding.Update();
			m_ResourceDetailsBinding.Update();
			m_ServiceBinding.Update();
			m_ProductionChainDataBinding.Update();
			m_ServiceUpkeepConsumptionBinding.Update();
			m_ConsumptionProductionBinding.Update();
			m_ImportExportBinding.Update();
			m_MaxProductionBinding.Update();
			m_FinalConsumptionBinding.Update();
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ProductionChain.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (GameManager.instance.gameMode == GameMode.Game && m_UpdateState.Advance())
		{
			m_MaxProgressBinding.Update();
			m_ProductionChainDataBinding.Update();
			m_ServiceUpkeepConsumptionBinding.Update();
			m_ConsumptionProductionBinding.Update();
			m_ImportExportBinding.Update();
			m_MaxProductionBinding.Update();
			m_FinalConsumptionBinding.Update();
		}
	}

	private int GetMaxProgress()
	{
		int num = 0;
		ResourceIterator iterator = ResourceIterator.GetIterator();
		NativeArray<int2> consumptionProductions = m_CityProductionStatisticSystem.GetConsumptionProductions();
		while (iterator.Next())
		{
			Entity entity = m_ResourcePrefabs[iterator.resource];
			ResourcePrefab prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(entity);
			if (prefab.m_IsLeisure || prefab.m_IsMaterial || prefab.m_IsProduceable)
			{
				int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
				int2 @int = consumptionProductions[resourceIndex];
				int productionCapacity = m_CityProductionCapacityCalculationSystem.GetProductionCapacity(iterator.resource);
				num = math.max(num, math.max(math.max(@int.x, @int.y), productionCapacity));
			}
		}
		return num;
	}

	private void WriteResourceCategories(IJsonWriter writer)
	{
		NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(m_ResourceCategoryQuery, Allocator.TempJob);
		try
		{
			writer.ArrayBegin(sortedObjects.Length);
			for (int i = 0; i < sortedObjects.Length; i++)
			{
				Entity entity = sortedObjects[i].entity;
				UIResourceCategoryPrefab prefab = m_PrefabSystem.GetPrefab<UIResourceCategoryPrefab>(sortedObjects[i].prefabData);
				NativeList<UIObjectInfo> objects = UIObjectInfo.GetObjects(base.EntityManager, base.EntityManager.GetBuffer<UIGroupElement>(entity, isReadOnly: true), Allocator.TempJob);
				objects.Sort();
				try
				{
					writer.TypeBegin("production.ResourceCategory");
					writer.PropertyName("entity");
					writer.Write(entity);
					writer.PropertyName("name");
					writer.Write(prefab.name);
					writer.PropertyName("resources");
					writer.ArrayBegin(objects.Length);
					for (int j = 0; j < objects.Length; j++)
					{
						WriteResource(writer, objects[j].entity);
					}
					writer.ArrayEnd();
					writer.TypeEnd();
				}
				finally
				{
					objects.Dispose();
				}
			}
			writer.ArrayEnd();
		}
		finally
		{
			sortedObjects.Dispose();
		}
	}

	public void WriteResource(IJsonWriter writer, Entity entity)
	{
		ResourcePrefab prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(entity);
		UIProductionLinks component = prefab.GetComponent<UIProductionLinks>();
		Resource resource = EconomyUtils.GetResource(prefab.m_Resource);
		try
		{
			writer.TypeBegin("production.Resource");
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("name");
			writer.Write(resource.ToString());
			writer.PropertyName("icon");
			writer.Write(ImageSystem.GetIcon(prefab));
			writer.PropertyName("weight");
			writer.Write(prefab.m_Weight);
			writer.PropertyName("unit");
			writer.Write((prefab.m_Weight > 0f) ? "weight" : "integerRounded");
			writer.PropertyName("tradable");
			writer.Write(prefab.m_IsTradable);
			writer.PropertyName("producer");
			WriteProductionLink(writer, component.m_Producer);
			writer.PropertyName("consumers");
			if (component.m_FinalConsumers != null)
			{
				writer.ArrayBegin(component.m_FinalConsumers.Length);
				for (int i = 0; i < component.m_FinalConsumers.Length; i++)
				{
					WriteProductionLink(writer, component.m_FinalConsumers[i]);
				}
				writer.ArrayEnd();
			}
			else
			{
				writer.WriteEmptyArray();
			}
			writer.TypeEnd();
		}
		catch (Exception message)
		{
			writer.WriteNull();
			UnityEngine.Debug.LogError(message);
		}
	}

	private void WriteProductionLink(IJsonWriter writer, UIProductionLinkPrefab prefab)
	{
		writer.TypeBegin("ProductionLink");
		writer.PropertyName("name");
		writer.Write(prefab.m_Type.ToString());
		writer.PropertyName("icon");
		writer.Write(prefab.m_Icon);
		writer.TypeEnd();
	}

	private void WriteResourceDetails(IJsonWriter writer, Entity entity)
	{
		NativeList<Entity> outputs = new NativeList<Entity>(Allocator.TempJob);
		NativeList<Entity> outputs2 = new NativeList<Entity>(Allocator.TempJob);
		NativeKeyValueArrays<Entity, (Entity, Entity)> keyValueArrays = m_ProductionChain.GetKeyValueArrays(Allocator.TempJob);
		NativeArray<Entity> serviceUpkeeps = m_ServiceUpkeepQuery.ToEntityArray(Allocator.TempJob);
		try
		{
			writer.TypeBegin("production.ResourceDetails");
			writer.PropertyName("inputs");
			writer.ArrayBegin(m_ProductionChain.CountValuesForKey(entity));
			foreach (var item in m_ProductionChain.GetValuesForKey(entity))
			{
				int num = 0;
				if (item.Item1 != Entity.Null)
				{
					num++;
				}
				if (item.Item2 != Entity.Null)
				{
					num++;
				}
				writer.ArrayBegin(num);
				if (item.Item1 != Entity.Null)
				{
					writer.Write(item.Item1);
				}
				if (item.Item2 != Entity.Null)
				{
					writer.Write(item.Item2);
				}
				writer.ArrayEnd();
			}
			writer.ArrayEnd();
			FindOutputs(entity, outputs, keyValueArrays);
			writer.PropertyName("outputs");
			writer.ArrayBegin(outputs.Length);
			for (int i = 0; i < outputs.Length; i++)
			{
				writer.Write(outputs[i]);
			}
			writer.ArrayEnd();
			FindServiceOutputs(entity, outputs2, serviceUpkeeps);
			writer.PropertyName("serviceOutputs");
			writer.ArrayBegin(outputs2.Length);
			for (int j = 0; j < outputs2.Length; j++)
			{
				writer.Write(outputs2[j]);
			}
			writer.ArrayEnd();
			writer.TypeEnd();
		}
		finally
		{
			keyValueArrays.Dispose();
			outputs.Dispose();
			outputs2.Dispose();
			serviceUpkeeps.Dispose();
		}
	}

	private void FindServiceOutputs(Entity entity, NativeList<Entity> outputs, NativeArray<Entity> serviceUpkeeps)
	{
		for (int i = 0; i < serviceUpkeeps.Length; i++)
		{
			Entity entity2 = serviceUpkeeps[i];
			DynamicBuffer<ServiceUpkeepData> buffer = base.EntityManager.GetBuffer<ServiceUpkeepData>(entity2, isReadOnly: true);
			for (int j = 0; j < buffer.Length; j++)
			{
				Resource resource = buffer[j].m_Upkeep.m_Resource;
				if (resource == Resource.NoResource)
				{
					continue;
				}
				Entity entity3 = m_ResourcePrefabs[resource];
				if (entity == entity3)
				{
					Entity value = base.EntityManager.GetComponentData<ServiceObjectData>(entity2).m_Service;
					if (!outputs.Contains(value))
					{
						outputs.Add(in value);
					}
				}
			}
		}
	}

	private void FindOutputs(Entity entity, NativeList<Entity> outputs, NativeKeyValueArrays<Entity, (Entity, Entity)> keyValueArrays)
	{
		for (int i = 0; i < keyValueArrays.Length; i++)
		{
			if (keyValueArrays.Values[i].Item1 == entity || keyValueArrays.Values[i].Item2 == entity)
			{
				outputs.Add(keyValueArrays.Keys[i]);
			}
		}
	}

	private void WriteService(IJsonWriter writer, Entity entity)
	{
		if (base.EntityManager.TryGetComponent<PrefabData>(entity, out var component))
		{
			ServicePrefab prefab = m_PrefabSystem.GetPrefab<ServicePrefab>(component);
			writer.TypeBegin("production.Service");
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("name");
			writer.Write(prefab.name);
			writer.PropertyName("icon");
			writer.Write(ImageSystem.GetIcon(prefab));
			writer.TypeEnd();
		}
		else
		{
			writer.WriteNull();
		}
	}

	private void BuildProductionChain(NativeParallelMultiHashMap<Entity, (Entity, Entity)> multiHashMap)
	{
		NativeArray<IndustrialProcessData> datas = m_IndustrialCompanyQuery.ToComponentDataArray<IndustrialProcessData>(Allocator.TempJob);
		NativeArray<IndustrialProcessData> datas2 = m_CommercialCompanyQuery.ToComponentDataArray<IndustrialProcessData>(Allocator.TempJob);
		ProcessProductionChainDatas(datas, m_ResourcePrefabs, multiHashMap);
		ProcessProductionChainDatas(datas2, m_ResourcePrefabs, multiHashMap);
		datas.Dispose();
		datas2.Dispose();
	}

	private void ProcessProductionChainDatas(NativeArray<IndustrialProcessData> datas, ResourcePrefabs resourcePrefabs, NativeParallelMultiHashMap<Entity, (Entity, Entity)> multiHashMap)
	{
		for (int i = 0; i < datas.Length; i++)
		{
			IndustrialProcessData industrialProcessData = datas[i];
			Entity entity = resourcePrefabs[industrialProcessData.m_Output.m_Resource];
			if (entity != Entity.Null)
			{
				(Entity, Entity) value = (Entity.Null, Entity.Null);
				if (industrialProcessData.m_Input1.m_Resource != Resource.NoResource)
				{
					value.Item1 = resourcePrefabs[industrialProcessData.m_Input1.m_Resource];
				}
				if (industrialProcessData.m_Input2.m_Resource != Resource.NoResource)
				{
					value.Item2 = resourcePrefabs[industrialProcessData.m_Input2.m_Resource];
				}
				TryAddUniqueValue(multiHashMap, entity, value);
			}
		}
	}

	private static void TryAddUniqueValue(NativeParallelMultiHashMap<Entity, (Entity, Entity)> multiHashMap, Entity key, (Entity, Entity) value)
	{
		foreach (var item in multiHashMap.GetValuesForKey(key))
		{
			if (item.Item1 == value.Item1 && item.Item2 == value.Item2)
			{
				return;
			}
		}
		multiHashMap.Add(key, value);
	}

	private void WriteServiceUpkeepConsumption(IJsonWriter writer, Entity entity)
	{
		Resource resource = EconomyUtils.GetResource(m_PrefabSystem.GetPrefab<ResourcePrefab>(entity).m_Resource);
		writer.Write(m_CityProductionStatisticSystem.GetCityResourceUsages(CityProductionStatisticSystem.CityResourceUsage.Consumer.ServiceUpkeep, resource));
	}

	private void WriteConsumptionProduction(IJsonWriter writer, Entity entity)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(EconomyUtils.GetResource(m_PrefabSystem.GetPrefab<ResourcePrefab>(entity).m_Resource));
		writer.Write(m_CityProductionStatisticSystem.GetConsumptionProductions()[resourceIndex]);
	}

	private void WriteImportExport(IJsonWriter writer, Entity entity)
	{
		Resource resource = EconomyUtils.GetResource(m_PrefabSystem.GetPrefab<ResourcePrefab>(entity).m_Resource);
		writer.Write(m_CityProductionStatisticSystem.GetCityResourceUsages(CityProductionStatisticSystem.CityResourceUsage.Consumer.ImportExport, resource));
	}

	private void WriteStoredResource(IJsonWriter writer, Entity entity)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(EconomyUtils.GetResource(m_PrefabSystem.GetPrefab<ResourcePrefab>(entity).m_Resource));
		writer.Write(m_CountCityStoredResourceSystem.GetCityStoredResources()[resourceIndex]);
	}

	private void WriteMaxProduction(IJsonWriter writer, Entity entity)
	{
		Resource resource = EconomyUtils.GetResource(m_PrefabSystem.GetPrefab<ResourcePrefab>(entity).m_Resource);
		int productionCapacity = m_CityProductionCapacityCalculationSystem.GetProductionCapacity(resource);
		writer.Write(productionCapacity);
	}

	private void WriteFinalConsumption(IJsonWriter writer, FinalConsumptionQuery query)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(EconomyUtils.GetResource(m_PrefabSystem.GetPrefab<ResourcePrefab>(query.resource).m_Resource));
		NativeArray<CityProductionStatisticSystem.CityResourceUsage> cityResourceUsages = m_CityProductionStatisticSystem.GetCityResourceUsages();
		FinalConsumption value = new FinalConsumption
		{
			consumer = query.consumer,
			consumption = cityResourceUsages[resourceIndex][query.consumer]
		};
		writer.Write(value);
	}

	private void IterateResources(Action<Entity> action)
	{
		ResourceIterator iterator = ResourceIterator.GetIterator();
		while (iterator.Next())
		{
			Entity entity = m_ResourcePrefabs[iterator.resource];
			ResourcePrefab prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(entity);
			if (prefab.m_IsLeisure || prefab.m_IsMaterial || prefab.m_IsProduceable)
			{
				action(entity);
			}
		}
	}

	private void WriteProductionChainData(IJsonWriter writer)
	{
		NativeParallelHashMap<CityProductionStatisticSystem.ProductionChain, CityProductionStatisticSystem.ProductionChainValue> productionChainsHashMap = m_CityProductionStatisticSystem.GetProductionChains();
		int chainCount = 0;
		IterateResources(delegate(Entity e)
		{
			foreach (var item in m_ProductionChain.GetValuesForKey(e))
			{
				_ = item;
				chainCount++;
			}
		});
		writer.ArrayBegin(chainCount);
		IterateResources(delegate(Entity entity)
		{
			foreach (var item2 in m_ProductionChain.GetValuesForKey(entity))
			{
				Resource resource = Resource.NoResource;
				Resource resource2 = Resource.NoResource;
				Resource resource3 = Resource.NoResource;
				if (item2.Item1 != Entity.Null)
				{
					resource = EconomyUtils.GetResource(m_PrefabSystem.GetPrefab<ResourcePrefab>(item2.Item1).m_Resource);
				}
				if (item2.Item2 != Entity.Null)
				{
					resource2 = EconomyUtils.GetResource(m_PrefabSystem.GetPrefab<ResourcePrefab>(item2.Item2).m_Resource);
				}
				if (entity != Entity.Null)
				{
					resource3 = EconomyUtils.GetResource(m_PrefabSystem.GetPrefab<ResourcePrefab>(entity).m_Resource);
				}
				CityProductionStatisticSystem.ProductionChain key = new CityProductionStatisticSystem.ProductionChain
				{
					m_Consume1 = resource,
					m_Consume2 = resource2,
					m_Produce = resource3
				};
				ProductionChainValue productionChainValue = (productionChainsHashMap.ContainsKey(key) ? productionChainsHashMap[key] : default(CityProductionStatisticSystem.ProductionChainValue));
				writer.Write(new ProductionChainData
				{
					consume1 = new ResourceValue
					{
						entity = item2.Item1,
						resource = resource,
						value = productionChainValue.consume1
					},
					consume2 = new ResourceValue
					{
						entity = item2.Item2,
						resource = resource2,
						value = productionChainValue.consume2
					},
					produce = new ResourceValue
					{
						entity = entity,
						resource = resource3,
						value = productionChainValue.produce
					}
				});
			}
		});
		writer.ArrayEnd();
	}

	[Preserve]
	public ProductionUISystem()
	{
	}
}
