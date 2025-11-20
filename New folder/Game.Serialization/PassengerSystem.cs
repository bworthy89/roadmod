using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.Creatures;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class PassengerSystem : GameSystemBase
{
	[BurstCompile]
	private struct PassengerJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentTransport> m_CurrentTransportType;

		public BufferLookup<Passenger> m_Passengers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CurrentVehicle> nativeArray2 = chunk.GetNativeArray(ref m_CurrentVehicleType);
			NativeArray<CurrentTransport> nativeArray3 = chunk.GetNativeArray(ref m_CurrentTransportType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity passenger = nativeArray[i];
				CurrentVehicle currentVehicle = nativeArray2[i];
				if (m_Passengers.HasBuffer(currentVehicle.m_Vehicle))
				{
					m_Passengers[currentVehicle.m_Vehicle].Add(new Passenger(passenger));
				}
			}
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				Entity passenger2 = nativeArray[j];
				CurrentTransport currentTransport = nativeArray3[j];
				if (m_Passengers.HasBuffer(currentTransport.m_CurrentTransport))
				{
					m_Passengers[currentTransport.m_CurrentTransport].Add(new Passenger(passenger2));
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
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentTypeHandle;

		public BufferLookup<Passenger> __Game_Vehicles_Passenger_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentTransport>(isReadOnly: true);
			__Game_Vehicles_Passenger_RW_BufferLookup = state.GetBufferLookup<Passenger>();
		}
	}

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Query = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<CurrentVehicle>(),
				ComponentType.ReadOnly<CurrentTransport>()
			}
		});
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PassengerJob jobData = new PassengerJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentTransportType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Passengers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_Passenger_RW_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_Query, base.Dependency);
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
	public PassengerSystem()
	{
	}
}
