using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Routes;

[CompilerGenerated]
public class BoardingVehicleSystem : GameSystemBase
{
	[BurstCompile]
	private struct BoardingVehicleJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<BoardingVehicle> m_BoardingVehicleType;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<Connected> m_ConnectedData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<BoardingVehicle> nativeArray2 = chunk.GetNativeArray(ref m_BoardingVehicleType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				BoardingVehicle value = nativeArray2[i];
				if (value.m_Vehicle != Entity.Null && !IsValidBoarding(nativeArray[i], value.m_Vehicle))
				{
					value.m_Vehicle = Entity.Null;
					nativeArray2[i] = value;
				}
			}
		}

		private bool IsValidBoarding(Entity stop, Entity vehicle)
		{
			if (m_TargetData.TryGetComponent(vehicle, out var componentData))
			{
				if (componentData.m_Target == stop)
				{
					return true;
				}
				if (m_ConnectedData.TryGetComponent(componentData.m_Target, out var componentData2))
				{
					return componentData2.m_Connected == stop;
				}
			}
			return false;
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

		public ComponentTypeHandle<BoardingVehicle> __Game_Routes_BoardingVehicle_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Routes_BoardingVehicle_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BoardingVehicle>();
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
		}
	}

	private EntityQuery m_WaypointQuery;

	private EntityQuery m_BoardingQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WaypointQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Waypoint>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_BoardingQuery = GetEntityQuery(ComponentType.ReadWrite<BoardingVehicle>());
		RequireForUpdate(m_BoardingQuery);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (GetLoaded() || !m_WaypointQuery.IsEmptyIgnoreFilter)
		{
			JobHandle dependency = JobChunkExtensions.ScheduleParallel(new BoardingVehicleJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_BoardingVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_BoardingVehicle_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef)
			}, m_BoardingQuery, base.Dependency);
			base.Dependency = dependency;
		}
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
	public BoardingVehicleSystem()
	{
	}
}
