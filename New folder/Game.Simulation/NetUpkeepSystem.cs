#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class NetUpkeepSystem : GameSystemBase
{
	[BurstCompile]
	private struct NetUpkeepJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentLookup<PlaceableNetComposition> m_PlaceableNetCompositionData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Composition> nativeArray = archetypeChunk.GetNativeArray(ref m_CompositionType);
				NativeArray<Curve> nativeArray2 = archetypeChunk.GetNativeArray(ref m_CurveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Composition composition = nativeArray[j];
					Curve curve = nativeArray2[j];
					if (m_PlaceableNetCompositionData.HasComponent(composition.m_Edge))
					{
						PlaceableNetComposition placeableNetData = m_PlaceableNetCompositionData[composition.m_Edge];
						num += NetUtils.GetUpkeepCost(curve, placeableNetData);
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PlaceableNetComposition> __Game_Prefabs_PlaceableNetComposition_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetComposition_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetComposition>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private CitySystem m_CitySystem;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_UpkeepQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_UpkeepQuery = GetEntityQuery(ComponentType.ReadOnly<Composition>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Owner>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Native>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_UpkeepQuery);
		Assert.AreEqual(kUpdatesPerDay, 32);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateFrame sharedComponentFilter = new UpdateFrame
		{
			m_Index = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16)
		};
		m_UpkeepQuery.ResetFilter();
		m_UpkeepQuery.SetSharedComponentFilter(sharedComponentFilter);
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_UpkeepQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		NetUpkeepJob jobData = new NetUpkeepJob
		{
			m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PlaceableNetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetComposition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Chunks = chunks
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		chunks.Dispose(base.Dependency);
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
	public NetUpkeepSystem()
	{
	}
}
