using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class FireAndRescueInfoviewUISystem : InfoviewUISystemBase
{
	[BurstCompile]
	private struct FireHazardJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> m_DamagedType;

		[ReadOnly]
		public ComponentTypeHandle<UnderConstruction> m_UnderConstructionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public EventHelpers.FireHazardData m_FireHazardData;

		public NativeArray<float> m_Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Building> nativeArray3 = chunk.GetNativeArray(ref m_BuildingType);
			if (nativeArray3.Length == 0)
			{
				return;
			}
			NativeArray<CurrentDistrict> nativeArray4 = chunk.GetNativeArray(ref m_CurrentDistrictType);
			NativeArray<Damaged> nativeArray5 = chunk.GetNativeArray(ref m_DamagedType);
			NativeArray<UnderConstruction> nativeArray6 = chunk.GetNativeArray(ref m_UnderConstructionType);
			float num = 0f;
			float num2 = 0f;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				PrefabRef prefabRef = nativeArray2[i];
				Building building = nativeArray3[i];
				CurrentDistrict currentDistrict = nativeArray4[i];
				CollectionUtils.TryGet(nativeArray5, i, out var value);
				if (!CollectionUtils.TryGet(nativeArray6, i, out var value2))
				{
					value2 = new UnderConstruction
					{
						m_Progress = byte.MaxValue
					};
				}
				if (m_FireHazardData.GetFireHazard(prefabRef, building, currentDistrict, value, value2, out var _, out var riskFactor))
				{
					num += riskFactor;
					num2 += 1f;
				}
			}
			m_Result[0] += num;
			m_Result[1] += num2;
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
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> __Game_Objects_Damaged_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Damaged>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentTypeHandle = state.GetComponentTypeHandle<UnderConstruction>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		}
	}

	private const string kGroup = "fireAndRescueInfo";

	private LocalEffectSystem m_LocalEffectSystem;

	private ClimateSystem m_ClimateSystem;

	private FireHazardSystem m_FireHazardSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_FlammableQuery;

	private EntityQuery m_FireStationsModifiedQuery;

	private EntityQuery m_FireConfigQuery;

	private NativeArray<float> m_Results;

	private EventHelpers.FireHazardData m_FireHazardData;

	private ValueBinding<IndicatorValue> m_AverageFireHazard;

	private TypeHandle __TypeHandle;

	protected override bool Active
	{
		get
		{
			if (!base.Active)
			{
				return m_AverageFireHazard.active;
			}
			return true;
		}
	}

	protected override bool Modified => !m_FireStationsModifiedQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LocalEffectSystem = base.World.GetOrCreateSystemManaged<LocalEffectSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_FireHazardSystem = base.World.GetOrCreateSystemManaged<FireHazardSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_FlammableQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Tree>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Buildings.FireStation>(),
				ComponentType.ReadOnly<OnFire>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_FireStationsModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.FireStation>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			}
		});
		m_FireConfigQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		AddBinding(m_AverageFireHazard = new ValueBinding<IndicatorValue>("fireAndRescueInfo", "averageFireHazard", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		m_Results = new NativeArray<float>(2, Allocator.Persistent);
		m_FireHazardData = new EventHelpers.FireHazardData(this);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_Results.Dispose();
	}

	protected override void PerformUpdate()
	{
		JobHandle dependencies;
		LocalEffectSystem.ReadData readData = m_LocalEffectSystem.GetReadData(out dependencies);
		FireConfigurationPrefab prefab = m_PrefabSystem.GetPrefab<FireConfigurationPrefab>(m_FireConfigQuery.GetSingletonEntity());
		if (m_Results.IsCreated)
		{
			base.Dependency.Complete();
			float num = m_Results[0];
			float num2 = m_Results[1];
			float current = ((num2 > 0f) ? (num / num2) : 0f);
			m_AverageFireHazard.Update(new IndicatorValue(0f, 100f, current));
			m_Results[0] = 0f;
			m_Results[1] = 0f;
		}
		m_FireHazardData.Update(this, readData, prefab, m_ClimateSystem.temperature, m_FireHazardSystem.noRainDays);
		JobHandle jobHandle = JobChunkExtensions.Schedule(new FireHazardJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DamagedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnderConstructionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FireHazardData = m_FireHazardData,
			m_Result = m_Results
		}, m_FlammableQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_LocalEffectSystem.AddLocalEffectReader(jobHandle);
		base.Dependency = jobHandle;
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
	public FireAndRescueInfoviewUISystem()
	{
	}
}
