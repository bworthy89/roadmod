using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class SelectVehiclesSection : InfoSectionBase
{
	private enum Result
	{
		HasDepots,
		EnergyTypes,
		Count
	}

	[BurstCompile]
	private struct TransportDepots : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradesType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_TransportDepotDataFromEntity;

		public TransportType m_TransportType;

		public NativeArray<int> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradesType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = m_PrefabRefFromEntity[entity].m_Prefab;
				m_TransportDepotDataFromEntity.TryGetComponent(prefab, out var componentData);
				if (CollectionUtils.TryGet(bufferAccessor, i, out var value))
				{
					UpgradeUtils.CombineStats(ref componentData, value, ref m_PrefabRefFromEntity, ref m_TransportDepotDataFromEntity);
				}
				if (componentData.m_TransportType == m_TransportType)
				{
					m_Results[0] = 1;
					m_Results[1] |= (int)componentData.m_EnergyTypes;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct TransportVehiclesListJob : IJob
	{
		public Resource m_Resources;

		public EnergyTypes m_EnergyTypes;

		public SizeClass m_SizeClass;

		public PublicTransportPurpose m_PublicTransportPurpose;

		public TransportType m_TransportType;

		public NativeList<Entity> m_PrimaryList;

		public NativeList<Entity> m_SecondaryList;

		[ReadOnly]
		public TransportVehicleSelectData m_VehicleSelectData;

		public void Execute()
		{
			m_VehicleSelectData.ListVehicles(m_TransportType, m_EnergyTypes, m_SizeClass, m_PublicTransportPurpose, m_Resources, m_PrimaryList, m_SecondaryList, ignoreTheme: true);
		}
	}

	[BurstCompile]
	private struct WorkVehiclesListJob : IJob
	{
		public RoadTypes m_RoadTypes;

		public SizeClass m_SizeClass;

		public VehicleWorkType m_WorkType;

		public MapFeature m_MapFeature;

		public Resource m_Resources;

		public NativeList<Entity> m_Prefabs;

		[ReadOnly]
		public WorkVehicleSelectData m_VehicleSelectData;

		public void Execute()
		{
			m_VehicleSelectData.ListVehicles(m_RoadTypes, m_SizeClass, m_WorkType, m_MapFeature, m_Resources, m_Prefabs);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> __Game_Prefabs_TransportDepotData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TransportDepotData_RO_ComponentLookup = state.GetComponentLookup<TransportDepotData>(isReadOnly: true);
		}
	}

	private PrefabUISystem m_PrefabUISystem;

	private ImageSystem m_ImageSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private TransportVehicleSelectData m_TransportVehicleSelectData;

	private WorkVehicleSelectData m_WorkVehicleSelectData;

	private EntityQuery m_TransportVehiclePrefabQuery;

	private EntityQuery m_WorkVehiclePrefabQuery;

	private EntityQuery m_DepotQuery;

	private NativeArray<int> m_Results;

	private TypeHandle __TypeHandle;

	protected override string group => "SelectVehiclesSection";

	private string routePrefab { get; set; }

	private NativeList<Entity> selectedPrimaryVehicles { get; set; }

	private NativeList<Entity> selectedSecondaryVehicles { get; set; }

	private NativeList<Entity> availablePrimaryVehicles { get; set; }

	private NativeList<Entity> availableSecondaryVehicles { get; set; }

	protected override bool displayForUpgrades => true;

	protected override void Reset()
	{
		selectedPrimaryVehicles.Clear();
		selectedSecondaryVehicles.Clear();
		availablePrimaryVehicles.Clear();
		availableSecondaryVehicles.Clear();
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_TransportVehicleSelectData = new TransportVehicleSelectData(this);
		m_WorkVehicleSelectData = new WorkVehicleSelectData(this);
		m_DepotQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.TransportDepot>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_TransportVehiclePrefabQuery = GetEntityQuery(TransportVehicleSelectData.GetEntityQueryDesc());
		m_WorkVehiclePrefabQuery = GetEntityQuery(WorkVehicleSelectData.GetEntityQueryDesc());
		selectedPrimaryVehicles = new NativeList<Entity>(20, Allocator.Persistent);
		selectedSecondaryVehicles = new NativeList<Entity>(20, Allocator.Persistent);
		availablePrimaryVehicles = new NativeList<Entity>(20, Allocator.Persistent);
		availableSecondaryVehicles = new NativeList<Entity>(20, Allocator.Persistent);
		m_Results = new NativeArray<int>(2, Allocator.Persistent);
		AddBinding(new TriggerBinding<Entity, Entity>(group, "selectVehicles", SelectVehicleModel));
		AddBinding(new TriggerBinding<Entity, Entity>(group, "deselectVehicles", DeselectVehicleModel));
	}

	private void SelectVehicleModel(Entity primary, Entity secondary)
	{
		DynamicBuffer<VehicleModel> buffer = base.EntityManager.GetBuffer<VehicleModel>(selectedEntity);
		bool flag = primary != Entity.Null;
		bool flag2 = secondary != Entity.Null;
		bool flag3 = false;
		bool flag4 = false;
		for (int i = 0; i < buffer.Length; i++)
		{
			VehicleModel value = buffer[i];
			if (flag && !flag3 && value.m_PrimaryPrefab == Entity.Null)
			{
				value.m_PrimaryPrefab = primary;
				flag3 = true;
			}
			if (flag2 && !flag4 && value.m_SecondaryPrefab == Entity.Null)
			{
				value.m_SecondaryPrefab = secondary;
				flag4 = true;
			}
			buffer[i] = value;
			if (flag3 && flag4)
			{
				break;
			}
		}
		if (flag && !flag3 && flag2 && !flag4)
		{
			buffer.Add(new VehicleModel
			{
				m_PrimaryPrefab = primary,
				m_SecondaryPrefab = secondary
			});
		}
		else if (flag && !flag3)
		{
			buffer.Add(new VehicleModel
			{
				m_PrimaryPrefab = primary,
				m_SecondaryPrefab = Entity.Null
			});
		}
		else if (flag2 && !flag4)
		{
			buffer.Add(new VehicleModel
			{
				m_PrimaryPrefab = Entity.Null,
				m_SecondaryPrefab = secondary
			});
		}
		m_InfoUISystem.RequestUpdate();
	}

	private void DeselectVehicleModel(Entity primary, Entity secondary)
	{
		DynamicBuffer<VehicleModel> buffer = base.EntityManager.GetBuffer<VehicleModel>(selectedEntity);
		bool flag = primary != Entity.Null;
		bool flag2 = secondary != Entity.Null;
		bool flag3 = false;
		bool flag4 = false;
		for (int i = 0; i < buffer.Length; i++)
		{
			VehicleModel value = buffer[i];
			if (flag && !flag3 && value.m_PrimaryPrefab == primary)
			{
				value.m_PrimaryPrefab = Entity.Null;
				flag3 = true;
			}
			if (flag2 && !flag4 && value.m_SecondaryPrefab == secondary)
			{
				value.m_SecondaryPrefab = Entity.Null;
				flag4 = true;
			}
			if (value.m_PrimaryPrefab == Entity.Null && value.m_SecondaryPrefab == Entity.Null)
			{
				buffer.RemoveAtSwapBack(i);
			}
			else
			{
				buffer[i] = value;
			}
			if (flag3 && flag4)
			{
				break;
			}
		}
		m_InfoUISystem.RequestUpdate();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		selectedPrimaryVehicles.Dispose();
		selectedSecondaryVehicles.Dispose();
		availablePrimaryVehicles.Dispose();
		availableSecondaryVehicles.Dispose();
		m_Results.Dispose();
		base.OnDestroy();
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<VehicleModel>(selectedEntity))
		{
			if (!base.EntityManager.HasComponent<TransportLineData>(selectedPrefab))
			{
				return base.EntityManager.HasComponent<WorkRouteData>(selectedPrefab);
			}
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (base.visible = Visible())
		{
			routePrefab = m_PrefabSystem.GetPrefabName(selectedPrefab);
			if (base.EntityManager.HasComponent<WorkRoute>(selectedEntity))
			{
				WorkRouteData componentData = base.EntityManager.GetComponentData<WorkRouteData>(selectedPrefab);
				m_WorkVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_WorkVehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
				JobHandle jobHandle2 = IJobExtensions.Schedule(new WorkVehiclesListJob
				{
					m_RoadTypes = componentData.m_RoadType,
					m_Resources = Resource.NoResource,
					m_Prefabs = availablePrimaryVehicles,
					m_SizeClass = componentData.m_SizeClass,
					m_WorkType = VehicleWorkType.Harvest,
					m_MapFeature = componentData.m_MapFeature,
					m_VehicleSelectData = m_WorkVehicleSelectData
				}, JobHandle.CombineDependencies(base.Dependency, jobHandle));
				m_WorkVehicleSelectData.PostUpdate(jobHandle2);
				jobHandle2.Complete();
			}
			else
			{
				TransportLineData componentData2 = base.EntityManager.GetComponentData<TransportLineData>(selectedPrefab);
				JobChunkExtensions.Schedule(new TransportDepots
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_InstalledUpgradesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TransportDepotDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TransportType = componentData2.m_TransportType,
					m_Results = m_Results
				}, m_DepotQuery, base.Dependency).Complete();
				m_TransportVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_TransportVehiclePrefabQuery, Allocator.TempJob, out var jobHandle3);
				bool flag2 = m_InfoUISystem.tags.Contains(SelectedInfoTags.CargoRoute);
				JobHandle jobHandle4 = IJobExtensions.Schedule(new TransportVehiclesListJob
				{
					m_Resources = (Resource)(flag2 ? (-1) : 0),
					m_EnergyTypes = (EnergyTypes)m_Results[1],
					m_SizeClass = componentData2.m_SizeClass,
					m_PublicTransportPurpose = ((!flag2) ? PublicTransportPurpose.TransportLine : ((PublicTransportPurpose)0)),
					m_TransportType = componentData2.m_TransportType,
					m_PrimaryList = availablePrimaryVehicles,
					m_SecondaryList = availableSecondaryVehicles,
					m_VehicleSelectData = m_TransportVehicleSelectData
				}, JobHandle.CombineDependencies(base.Dependency, jobHandle3));
				m_TransportVehicleSelectData.PostUpdate(jobHandle4);
				jobHandle4.Complete();
			}
			base.visible = availablePrimaryVehicles.Length > 1 || availableSecondaryVehicles.Length > 1;
		}
	}

	protected override void OnProcess()
	{
		foreach (VehicleModel item in base.EntityManager.GetBuffer<VehicleModel>(selectedEntity, isReadOnly: true))
		{
			VehicleModel current = item;
			if (current.m_PrimaryPrefab != Entity.Null)
			{
				selectedPrimaryVehicles.Add(in current.m_PrimaryPrefab);
			}
			if (current.m_SecondaryPrefab != Entity.Null)
			{
				selectedSecondaryVehicles.Add(in current.m_SecondaryPrefab);
			}
		}
		base.tooltipTags.Add("TransportLine");
		base.tooltipTags.Add("CargoRoute");
		base.tooltipTags.Add("WorkRoute");
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("routePrefab");
		writer.Write(routePrefab);
		writer.PropertyName("selectedPrimaryVehicles");
		writer.ArrayBegin(selectedPrimaryVehicles.Length);
		for (int i = 0; i < selectedPrimaryVehicles.Length; i++)
		{
			WriteVehicle(writer, selectedPrimaryVehicles[i]);
		}
		writer.ArrayEnd();
		writer.PropertyName("selectedSecondaryVehicles");
		if (base.EntityManager.TryGetComponent<TransportLineData>(selectedPrefab, out var component))
		{
			TransportType transportType = component.m_TransportType;
			if (transportType == TransportType.Train || transportType == TransportType.Tram || transportType == TransportType.Subway)
			{
				writer.ArrayBegin(selectedSecondaryVehicles.Length);
				for (int j = 0; j < selectedSecondaryVehicles.Length; j++)
				{
					WriteVehicle(writer, selectedSecondaryVehicles[j]);
				}
				writer.ArrayEnd();
				goto IL_00f4;
			}
		}
		writer.WriteNull();
		goto IL_00f4;
		IL_00f4:
		writer.PropertyName("availablePrimaryVehicles");
		writer.ArrayBegin(availablePrimaryVehicles.Length);
		for (int k = 0; k < availablePrimaryVehicles.Length; k++)
		{
			WriteVehicle(writer, availablePrimaryVehicles[k]);
		}
		writer.ArrayEnd();
		writer.PropertyName("availableSecondaryVehicles");
		if (base.EntityManager.TryGetComponent<TransportLineData>(selectedPrefab, out component))
		{
			TransportType transportType = component.m_TransportType;
			if (transportType == TransportType.Train || transportType == TransportType.Tram || transportType == TransportType.Subway)
			{
				writer.ArrayBegin(availableSecondaryVehicles.Length);
				for (int l = 0; l < availableSecondaryVehicles.Length; l++)
				{
					WriteVehicle(writer, availableSecondaryVehicles[l]);
				}
				writer.ArrayEnd();
				return;
			}
		}
		writer.WriteNull();
	}

	private void WriteVehicle(IJsonWriter writer, Entity entity)
	{
		writer.TypeBegin(GetType().FullName + "+VehiclePrefab");
		writer.PropertyName("entity");
		writer.Write(entity);
		writer.PropertyName("id");
		writer.Write(m_PrefabSystem.GetPrefabName(entity));
		writer.PropertyName("locked");
		writer.Write(base.EntityManager.HasEnabledComponent<Locked>(entity));
		writer.PropertyName("multiunit");
		writer.Write(base.EntityManager.HasComponent<MultipleUnitTrainData>(entity));
		writer.PropertyName("requirements");
		m_PrefabUISystem.BindPrefabRequirements(writer, entity);
		writer.PropertyName("thumbnail");
		writer.Write(m_ImageSystem.GetThumbnail(entity) ?? m_ImageSystem.placeholderIcon);
		writer.PropertyName("objectRequirementIcons");
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer))
		{
			writer.ArrayBegin(buffer.Length);
			foreach (ObjectRequirementElement item in buffer)
			{
				writer.Write(m_ImageSystem.GetThumbnail(item.m_Requirement));
			}
			writer.ArrayEnd();
		}
		else
		{
			writer.WriteNull();
		}
		writer.TypeEnd();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public SelectVehiclesSection()
	{
	}
}
