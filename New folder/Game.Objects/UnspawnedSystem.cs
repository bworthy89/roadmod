using System.Runtime.CompilerServices;
using Game.Common;
using Game.Creatures;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class UnspawnedSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateUnspawnedJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutType;

		[ReadOnly]
		public BufferTypeHandle<Passenger> m_PassengerType;

		[ReadOnly]
		public ComponentLookup<Relative> m_RelativeData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Passenger> m_Passengers;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			BufferAccessor<LayoutElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_LayoutType);
			BufferAccessor<Passenger> bufferAccessor3 = chunk.GetBufferAccessor(ref m_PassengerType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				CheckSubObjects(unfilteredChunkIndex, bufferAccessor[i], isUnspawned);
			}
			for (int j = 0; j < bufferAccessor2.Length; j++)
			{
				CheckLayout(unfilteredChunkIndex, nativeArray[j], bufferAccessor2[j], isUnspawned);
			}
			for (int k = 0; k < bufferAccessor3.Length; k++)
			{
				CheckPassengers(unfilteredChunkIndex, bufferAccessor3[k], isUnspawned);
			}
		}

		private void CheckSubObjects(int jobIndex, DynamicBuffer<SubObject> subObjects, bool isUnspawned)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_RelativeData.HasComponent(subObject) && m_UnspawnedData.HasComponent(subObject) != isUnspawned)
				{
					UpdateUnspawned(jobIndex, subObject, isUnspawned);
					if (m_SubObjects.TryGetBuffer(subObject, out var bufferData))
					{
						CheckSubObjects(jobIndex, bufferData, isUnspawned);
					}
				}
			}
		}

		private void CheckLayout(int jobIndex, Entity entity, DynamicBuffer<LayoutElement> layout, bool isUnspawned)
		{
			for (int i = 0; i < layout.Length; i++)
			{
				Entity vehicle = layout[i].m_Vehicle;
				if (!(vehicle == entity) && m_UnspawnedData.HasComponent(vehicle) != isUnspawned)
				{
					UpdateUnspawned(jobIndex, vehicle, isUnspawned);
					if (m_SubObjects.TryGetBuffer(vehicle, out var bufferData))
					{
						CheckSubObjects(jobIndex, bufferData, isUnspawned);
					}
					if (m_Passengers.TryGetBuffer(vehicle, out var bufferData2))
					{
						CheckPassengers(jobIndex, bufferData2, isUnspawned);
					}
				}
			}
		}

		private void CheckPassengers(int jobIndex, DynamicBuffer<Passenger> passengers, bool isUnspawned)
		{
			for (int i = 0; i < passengers.Length; i++)
			{
				Entity passenger = passengers[i].m_Passenger;
				if (m_RelativeData.HasComponent(passenger) && m_UnspawnedData.HasComponent(passenger) != isUnspawned)
				{
					UpdateUnspawned(jobIndex, passenger, isUnspawned);
					if (m_SubObjects.TryGetBuffer(passenger, out var bufferData))
					{
						CheckSubObjects(jobIndex, bufferData, isUnspawned);
					}
				}
			}
		}

		private void UpdateUnspawned(int jobIndex, Entity entity, bool isUnspawned)
		{
			if (isUnspawned)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
			}
			else
			{
				m_CommandBuffer.RemoveComponent<Unspawned>(jobIndex, entity);
			}
			if (!m_UpdatedData.HasComponent(entity))
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
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
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Passenger> __Game_Vehicles_Passenger_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Relative> __Game_Objects_Relative_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Passenger> __Game_Vehicles_Passenger_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferTypeHandle = state.GetBufferTypeHandle<Passenger>(isReadOnly: true);
			__Game_Objects_Relative_RO_ComponentLookup = state.GetComponentLookup<Relative>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferLookup = state.GetBufferLookup<Passenger>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdateQuery;

	private ModificationEndBarrier m_ModificationEndBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationEndBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_UpdateQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadWrite<Vehicle>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadWrite<Creature>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>()
			}
		});
		RequireForUpdate(m_UpdateQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateUnspawnedJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LayoutType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PassengerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_RelativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_Passengers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationEndBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_UpdateQuery, base.Dependency);
		m_ModificationEndBarrier.AddJobHandleForProducer(jobHandle);
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
	public UnspawnedSystem()
	{
	}
}
