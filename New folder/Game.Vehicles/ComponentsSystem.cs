using System.Runtime.CompilerServices;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Vehicles;

[CompilerGenerated]
public class ComponentsSystem : GameSystemBase
{
	[BurstCompile]
	private struct VehicleComponentsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<PassengerTransport> m_PassengerTransportData;

		[ReadOnly]
		public ComponentLookup<EvacuatingTransport> m_EvacuatingTransportData;

		[ReadOnly]
		public ComponentLookup<PrisonerTransport> m_PrisonerTransportData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity e = nativeArray[i];
				Temp temp = nativeArray2[i];
				if (m_PassengerTransportData.HasComponent(temp.m_Original))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(PassengerTransport));
				}
				if (m_EvacuatingTransportData.HasComponent(temp.m_Original))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(EvacuatingTransport));
				}
				if (m_PrisonerTransportData.HasComponent(temp.m_Original))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(PrisonerTransport));
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PassengerTransport> __Game_Vehicles_PassengerTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EvacuatingTransport> __Game_Vehicles_EvacuatingTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrisonerTransport> __Game_Vehicles_PrisonerTransport_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Vehicles_PassengerTransport_RO_ComponentLookup = state.GetComponentLookup<PassengerTransport>(isReadOnly: true);
			__Game_Vehicles_EvacuatingTransport_RO_ComponentLookup = state.GetComponentLookup<EvacuatingTransport>(isReadOnly: true);
			__Game_Vehicles_PrisonerTransport_RO_ComponentLookup = state.GetComponentLookup<PrisonerTransport>(isReadOnly: true);
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_VehicleQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadOnly<Temp>());
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new VehicleComponentsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PassengerTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PassengerTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EvacuatingTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_EvacuatingTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrisonerTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PrisonerTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_VehicleQuery, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public ComponentsSystem()
	{
	}
}
