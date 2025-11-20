using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Objects;
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
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TransportStationAISystem : GameSystemBase
{
	[BurstCompile]
	private struct TransportStationTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public ComponentTypeHandle<Game.Buildings.TransportStation> m_TransportStationType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<TransportStationData> m_PrefabTransportStationData;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> m_ConnectedRoutes;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjectBuffers;

		[ReadOnly]
		public ComponentLookup<Owner> m_Owners;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.TransportStation> nativeArray3 = chunk.GetNativeArray(ref m_TransportStationType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				Game.Buildings.TransportStation transportStation = nativeArray3[i];
				TransportStationData data = default(TransportStationData);
				if (m_PrefabTransportStationData.HasComponent(prefabRef.m_Prefab))
				{
					data = m_PrefabTransportStationData[prefabRef.m_Prefab];
				}
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_PrefabRefData, ref m_PrefabTransportStationData);
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				Tick(entity, prefabRef, ref transportStation, data, efficiency);
				nativeArray3[i] = transportStation;
			}
		}

		private void Tick(Entity entity, PrefabRef prefabRef, ref Game.Buildings.TransportStation transportStation, TransportStationData prefabTransportStationData, float efficiency)
		{
			transportStation.m_ComfortFactor = math.saturate(prefabTransportStationData.m_ComfortFactor * efficiency);
			transportStation.m_LoadingFactor = math.max(0f, (1f + prefabTransportStationData.m_LoadingFactor) * efficiency);
			if (efficiency > 0f)
			{
				transportStation.m_CarRefuelTypes = prefabTransportStationData.m_CarRefuelTypes;
				transportStation.m_TrainRefuelTypes = prefabTransportStationData.m_TrainRefuelTypes;
				transportStation.m_WatercraftRefuelTypes = prefabTransportStationData.m_WatercraftRefuelTypes;
				transportStation.m_AircraftRefuelTypes = prefabTransportStationData.m_AircraftRefuelTypes;
				transportStation.m_Flags |= TransportStationFlags.TransportStopsActive;
			}
			else
			{
				transportStation.m_CarRefuelTypes = EnergyTypes.None;
				transportStation.m_TrainRefuelTypes = EnergyTypes.None;
				transportStation.m_WatercraftRefuelTypes = EnergyTypes.None;
				transportStation.m_AircraftRefuelTypes = EnergyTypes.None;
				transportStation.m_Flags &= ~TransportStationFlags.TransportStopsActive;
			}
			if ((transportStation.m_Flags & TransportStationFlags.TransportStopsActive) != 0 && (transportStation.m_CarRefuelTypes != EnergyTypes.None || transportStation.m_TrainRefuelTypes != EnergyTypes.None || transportStation.m_WatercraftRefuelTypes != EnergyTypes.None || transportStation.m_AircraftRefuelTypes != EnergyTypes.None))
			{
				NativeList<Entity> linesResult = new NativeList<Entity>(Allocator.Temp);
				BuildingUtils.GetNumberOfConnectedLines(entity, ref linesResult, ref m_ConnectedRoutes, ref m_SubObjectBuffers, ref m_Owners);
				if (linesResult.Length == 0)
				{
					transportStation.m_Flags &= ~TransportStationFlags.TransportStopsActive;
				}
				linesResult.Dispose();
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.TransportStation> __Game_Buildings_TransportStation_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStationData> __Game_Prefabs_TransportStationData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> __Game_Routes_ConnectedRoute_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_TransportStation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TransportStation>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TransportStationData_RO_ComponentLookup = state.GetComponentLookup<TransportStationData>(isReadOnly: true);
			__Game_Routes_ConnectedRoute_RO_BufferLookup = state.GetBufferLookup<ConnectedRoute>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
		}
	}

	private EntityQuery m_BuildingQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 0;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.TransportStation>(), ComponentType.Exclude<Game.Buildings.ServiceUpgrade>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_BuildingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new TransportStationTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TransportStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TransportStation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedRoutes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjectBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef)
		}, m_BuildingQuery, base.Dependency);
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
	public TransportStationAISystem()
	{
	}
}
