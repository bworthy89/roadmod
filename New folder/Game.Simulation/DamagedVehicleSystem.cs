#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class DamagedVehicleSystem : GameSystemBase
{
	[BurstCompile]
	private struct DamagedVehicleJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public ComponentTypeHandle<MaintenanceConsumer> m_MaintenanceConsumerType;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> m_DamagedType;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> m_MaintenanceRequestData;

		[ReadOnly]
		public EntityArchetype m_MaintenanceRequestArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Destroyed> nativeArray2 = chunk.GetNativeArray(ref m_DestroyedType);
			NativeArray<MaintenanceConsumer> nativeArray3 = chunk.GetNativeArray(ref m_MaintenanceConsumerType);
			NativeArray<Damaged> nativeArray4 = chunk.GetNativeArray(ref m_DamagedType);
			if (nativeArray2.Length != 0)
			{
				if (nativeArray3.Length != 0)
				{
					for (int i = 0; i < nativeArray2.Length; i++)
					{
						Destroyed destroyed = nativeArray2[i];
						Entity entity = nativeArray[i];
						if (destroyed.m_Cleared < 1f)
						{
							MaintenanceConsumer maintenanceConsumer = nativeArray3[i];
							RequestMaintenanceIfNeeded(unfilteredChunkIndex, entity, maintenanceConsumer);
						}
						else
						{
							m_CommandBuffer.RemoveComponent<MaintenanceConsumer>(unfilteredChunkIndex, entity);
						}
					}
					return;
				}
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (nativeArray2[j].m_Cleared < 1f)
					{
						Entity entity2 = nativeArray[j];
						MaintenanceConsumer maintenanceConsumer2 = default(MaintenanceConsumer);
						RequestMaintenanceIfNeeded(unfilteredChunkIndex, entity2, maintenanceConsumer2);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, maintenanceConsumer2);
					}
				}
				return;
			}
			if (nativeArray3.Length != 0)
			{
				for (int k = 0; k < nativeArray4.Length; k++)
				{
					Damaged damaged = nativeArray4[k];
					Entity entity3 = nativeArray[k];
					if (math.any(damaged.m_Damage > 0f))
					{
						MaintenanceConsumer maintenanceConsumer3 = nativeArray3[k];
						RequestMaintenanceIfNeeded(unfilteredChunkIndex, entity3, maintenanceConsumer3);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<MaintenanceConsumer>(unfilteredChunkIndex, entity3);
					}
				}
				return;
			}
			for (int l = 0; l < nativeArray4.Length; l++)
			{
				if (math.any(nativeArray4[l].m_Damage > 0f))
				{
					Entity entity4 = nativeArray[l];
					MaintenanceConsumer maintenanceConsumer4 = default(MaintenanceConsumer);
					RequestMaintenanceIfNeeded(unfilteredChunkIndex, entity4, maintenanceConsumer4);
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity4, maintenanceConsumer4);
				}
			}
		}

		private void RequestMaintenanceIfNeeded(int jobIndex, Entity entity, MaintenanceConsumer maintenanceConsumer)
		{
			if (!m_MaintenanceRequestData.HasComponent(maintenanceConsumer.m_Request))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_MaintenanceRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new MaintenanceRequest(entity, 100));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
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
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MaintenanceConsumer> __Game_Simulation_MaintenanceConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> __Game_Objects_Damaged_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> __Game_Simulation_MaintenanceRequest_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Simulation_MaintenanceConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MaintenanceConsumer>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Damaged>(isReadOnly: true);
			__Game_Simulation_MaintenanceRequest_RO_ComponentLookup = state.GetComponentLookup<MaintenanceRequest>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 512u;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_DamagedQuery;

	private EntityArchetype m_MaintenanceRequestArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_DamagedQuery = GetEntityQuery(ComponentType.ReadOnly<Damaged>(), ComponentType.ReadOnly<Stopped>(), ComponentType.ReadOnly<Car>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_MaintenanceRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<MaintenanceRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_DamagedQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new DamagedVehicleJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MaintenanceConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_MaintenanceConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DamagedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MaintenanceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MaintenanceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceRequestArchetype = m_MaintenanceRequestArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_DamagedQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public DamagedVehicleSystem()
	{
	}
}
