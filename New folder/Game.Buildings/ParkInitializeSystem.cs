using System.Runtime.CompilerServices;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class ParkInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeParksJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Park> m_ParkType;

		public ComponentTypeHandle<ModifiedServiceCoverage> m_ModifiedServiceCoverageType;

		[ReadOnly]
		public ComponentLookup<ParkData> m_ParkData;

		[ReadOnly]
		public ComponentLookup<CoverageData> m_CoverageData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public Entity m_City;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			DynamicBuffer<CityModifier> cityModifiers = default(DynamicBuffer<CityModifier>);
			if (m_City != Entity.Null)
			{
				cityModifiers = m_CityModifiers[m_City];
			}
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Park> nativeArray2 = chunk.GetNativeArray(ref m_ParkType);
			NativeArray<ModifiedServiceCoverage> nativeArray3 = chunk.GetNativeArray(ref m_ModifiedServiceCoverageType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity prefab = nativeArray[i].m_Prefab;
				Park park = nativeArray2[i];
				if (m_ParkData.HasComponent(prefab))
				{
					ParkData prefabParkData = m_ParkData[prefab];
					park.m_Maintenance = prefabParkData.m_MaintenancePool;
					nativeArray2[i] = park;
					if (m_CoverageData.HasComponent(prefab))
					{
						CoverageData prefabCoverageData = m_CoverageData[prefab];
						nativeArray3[i] = ParkAISystem.GetModifiedServiceCoverage(park, prefabParkData, prefabCoverageData, cityModifiers);
					}
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Park> __Game_Buildings_Park_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ModifiedServiceCoverage> __Game_Buildings_ModifiedServiceCoverage_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ParkData> __Game_Prefabs_ParkData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CoverageData> __Game_Prefabs_CoverageData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Park_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Park>();
			__Game_Buildings_ModifiedServiceCoverage_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ModifiedServiceCoverage>();
			__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(isReadOnly: true);
			__Game_Prefabs_CoverageData_RO_ComponentLookup = state.GetComponentLookup<CoverageData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private CitySystem m_CitySystem;

	private EntityQuery m_ParkQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ParkQuery = GetEntityQuery(ComponentType.ReadWrite<Park>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>());
		RequireForUpdate(m_ParkQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new InitializeParksJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Park_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ModifiedServiceCoverageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ModifiedServiceCoverage_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CoverageData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City
		}, m_ParkQuery, base.Dependency);
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
	public ParkInitializeSystem()
	{
	}
}
