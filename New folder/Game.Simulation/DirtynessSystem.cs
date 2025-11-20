using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class DirtynessSystem : GameSystemBase
{
	[BurstCompile]
	private struct DirtynessJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<BuildingCondition> m_BuildingConditionType;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> m_BuildingAbandonedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_BuildingEfficiencyType;

		public ComponentTypeHandle<Surface> m_ObjectSurfaceType;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityEffects;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public Entity m_City;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Surface> nativeArray = chunk.GetNativeArray(ref m_ObjectSurfaceType);
			NativeArray<BuildingCondition> nativeArray2 = chunk.GetNativeArray(ref m_BuildingConditionType);
			if (nativeArray2.Length != 0)
			{
				if (chunk.Has(ref m_BuildingAbandonedType))
				{
					for (int i = 0; i < nativeArray.Length; i++)
					{
						nativeArray.ElementAt(i).m_Dirtyness = byte.MaxValue;
					}
					return;
				}
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				DynamicBuffer<CityModifier> cityEffects = m_CityEffects[m_City];
				for (int j = 0; j < nativeArray.Length; j++)
				{
					BuildingCondition buildingCondition = nativeArray2[j];
					PrefabRef prefabRef = nativeArray3[j];
					ref Surface reference = ref nativeArray.ElementAt(j);
					if (buildingCondition.m_Condition < 0)
					{
						int x = 0;
						if (m_SpawnableBuildingData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
						{
							AreaType areaType = m_ZoneData[componentData.m_ZonePrefab].m_AreaType;
							BuildingPropertyData propertyData = m_BuildingPropertyData[prefabRef.m_Prefab];
							x = BuildingUtils.GetLevelingCost(areaType, propertyData, math.min(4, componentData.m_Level), cityEffects);
						}
						x = math.max(x, -buildingCondition.m_Condition);
						reference.m_Dirtyness = (byte)((buildingCondition.m_Condition * -255 + (x >> 1)) / x);
					}
					else
					{
						reference.m_Dirtyness = 0;
					}
				}
				return;
			}
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_BuildingEfficiencyType);
			if (bufferAccessor.Length != 0)
			{
				Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
				for (int k = 0; k < bufferAccessor.Length; k++)
				{
					DynamicBuffer<Efficiency> buffer = bufferAccessor[k];
					ref Surface reference2 = ref nativeArray.ElementAt(k);
					float num = math.clamp(math.saturate(1f - BuildingUtils.GetEfficiency(buffer)) - (float)(int)reference2.m_Dirtyness * 0.003921569f, -0.1f, 0.01f);
					reference2.m_Dirtyness = (byte)math.clamp(reference2.m_Dirtyness + MathUtils.RoundToIntRandom(ref random, num * 255f), 0, 255);
				}
			}
			else
			{
				for (int l = 0; l < nativeArray.Length; l++)
				{
					nativeArray.ElementAt(l).m_Dirtyness = 0;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<BuildingCondition> __Game_Buildings_BuildingCondition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> __Game_Buildings_Abandoned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		public ComponentTypeHandle<Surface> __Game_Objects_Surface_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_BuildingCondition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingCondition>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Abandoned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Objects_Surface_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Surface>();
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private CitySystem m_CitySystem;

	private EntityQuery m_SurfaceQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_SurfaceQuery = GetEntityQuery(ComponentType.ReadWrite<Surface>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Overridden>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_SurfaceQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new DirtynessJob
		{
			m_BuildingConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_BuildingCondition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingAbandonedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingEfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ObjectSurfaceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Surface_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingPropertyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_City = m_CitySystem.City
		}, m_SurfaceQuery, base.Dependency);
		base.Dependency = dependency;
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
	public DirtynessSystem()
	{
	}
}
