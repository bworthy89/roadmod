using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class GarbageInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		ProcessingRate,
		GarbageCapacity,
		StoredGarbage,
		ResultCount
	}

	[BurstCompile]
	private struct UpdateGarbageJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_BuildingEfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentLookup<Storage> m_Storages;

		[ReadOnly]
		public ComponentLookup<Geometry> m_Geometries;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> m_GarbageFacilities;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> m_StorageAreaDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public BufferLookup<Resources> m_Resources;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_BuildingEfficiencyType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				if (efficiency == 0f)
				{
					continue;
				}
				GarbageFacilityData data = default(GarbageFacilityData);
				if (m_GarbageFacilities.HasComponent(prefabRef.m_Prefab))
				{
					data = m_GarbageFacilities[prefabRef.m_Prefab];
				}
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_Prefabs, ref m_GarbageFacilities);
				}
				if (m_Resources.TryGetBuffer(entity, out var bufferData))
				{
					num4 += (float)EconomyUtils.GetResources(Resource.Garbage, bufferData);
				}
				if (m_SubAreas.TryGetBuffer(entity, out var bufferData2))
				{
					for (int j = 0; j < bufferData2.Length; j++)
					{
						Entity area = bufferData2[j].m_Area;
						Entity prefab = m_Prefabs[area].m_Prefab;
						if (m_Storages.TryGetComponent(area, out var componentData) && m_StorageAreaDatas.TryGetComponent(prefab, out var componentData2))
						{
							Geometry geometry = m_Geometries[area];
							data.m_GarbageCapacity += AreaUtils.CalculateStorageCapacity(geometry, componentData2);
							num4 += (float)componentData.m_Amount;
						}
					}
				}
				num += efficiency * (float)data.m_ProcessingSpeed;
				num2 += (float)(data.m_LongTermStorage ? data.m_GarbageCapacity : 0);
				num3 += (data.m_LongTermStorage ? num4 : 0f);
			}
			m_Results[0] += num;
			m_Results[1] += num2;
			m_Results[2] += num3;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Storage> __Game_Areas_Storage_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> __Game_Prefabs_GarbageFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> __Game_Prefabs_StorageAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Areas_Storage_RO_ComponentLookup = state.GetComponentLookup<Storage>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup = state.GetComponentLookup<GarbageFacilityData>(isReadOnly: true);
			__Game_Prefabs_StorageAreaData_RO_ComponentLookup = state.GetComponentLookup<StorageAreaData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
		}
	}

	private const string kGroup = "garbageInfo";

	private GarbageAccumulationSystem m_GarbageAccumulationSystem;

	private GetterValueBinding<int> m_Capacity;

	private GetterValueBinding<int> m_StoredGarbage;

	private GetterValueBinding<float> m_ProcessingRate;

	private GetterValueBinding<float> m_GarbageRate;

	private GetterValueBinding<IndicatorValue> m_ProcessingAvailability;

	private GetterValueBinding<IndicatorValue> m_LandfillAvailability;

	private EntityQuery m_GarbageFacilityQuery;

	private EntityQuery m_GarbageFacilityModifiedQuery;

	private NativeArray<float> m_Results;

	private TypeHandle __TypeHandle;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_Capacity.active && !m_StoredGarbage.active && !m_ProcessingRate.active && !m_GarbageRate.active && !m_ProcessingAvailability.active)
			{
				return m_LandfillAvailability.active;
			}
			return true;
		}
	}

	protected override bool Modified => !m_GarbageFacilityModifiedQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GarbageAccumulationSystem = base.World.GetOrCreateSystemManaged<GarbageAccumulationSystem>();
		m_GarbageFacilityQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_GarbageFacilityModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<ServiceDispatch>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		AddBinding(m_Capacity = new GetterValueBinding<int>("garbageInfo", "capacity", GetGarbageCapacity));
		AddBinding(m_StoredGarbage = new GetterValueBinding<int>("garbageInfo", "storedGarbage", GetStoredGarbage));
		AddBinding(m_ProcessingRate = new GetterValueBinding<float>("garbageInfo", "processingRate", GetProcessingRate));
		AddBinding(m_GarbageRate = new GetterValueBinding<float>("garbageInfo", "productionRate", () => m_GarbageAccumulationSystem.garbageAccumulation));
		AddBinding(m_ProcessingAvailability = new GetterValueBinding<IndicatorValue>("garbageInfo", "processingAvailability", GetProcessingAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_LandfillAvailability = new GetterValueBinding<IndicatorValue>("garbageInfo", "landfillAvailability", GetLandfillAvailability, new ValueWriter<IndicatorValue>()));
		m_Results = new NativeArray<float>(3, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		ResetResults();
		JobChunkExtensions.Schedule(new UpdateGarbageJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BuildingEfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Storages = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Storage_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Geometries = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageFacilities = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageAreaDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_GarbageFacilityQuery, base.Dependency).Complete();
		m_Capacity.Update();
		m_StoredGarbage.Update();
		m_ProcessingRate.Update();
		m_GarbageRate.Update();
		m_ProcessingAvailability.Update();
		m_LandfillAvailability.Update();
	}

	private void ResetResults()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0f;
		}
	}

	private int GetGarbageCapacity()
	{
		return (int)m_Results[1];
	}

	private int GetStoredGarbage()
	{
		return (int)m_Results[2];
	}

	private float GetProcessingRate()
	{
		return m_Results[0];
	}

	private IndicatorValue GetLandfillAvailability()
	{
		return IndicatorValue.Calculate(m_Results[1], m_Results[2], 0f);
	}

	private IndicatorValue GetProcessingAvailability()
	{
		return IndicatorValue.Calculate(m_Results[0], math.max(m_GarbageAccumulationSystem.garbageAccumulation, 0L));
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
	public GarbageInfoviewUISystem()
	{
	}
}
