using System.Runtime.CompilerServices;
using Game.Common;
using Game.Simulation;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class DamageSystem : GameSystemBase
{
	[BurstCompile]
	private struct DamageObjectsJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Damage> m_DamageType;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		public ComponentLookup<Damaged> m_DamagedData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeParallelHashMap<Entity, Damaged> nativeParallelHashMap = new NativeParallelHashMap<Entity, Damaged>(10, Allocator.Temp);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				NativeArray<Damage> nativeArray = m_Chunks[i].GetNativeArray(ref m_DamageType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Damage damage = nativeArray[j];
					if (nativeParallelHashMap.TryGetValue(damage.m_Object, out var item))
					{
						item.m_Damage += damage.m_Delta;
						nativeParallelHashMap[damage.m_Object] = item;
					}
					else if (m_DamagedData.HasComponent(damage.m_Object))
					{
						item = m_DamagedData[damage.m_Object];
						item.m_Damage += damage.m_Delta;
						nativeParallelHashMap.TryAdd(damage.m_Object, item);
					}
					else
					{
						nativeParallelHashMap.TryAdd(damage.m_Object, new Damaged(damage.m_Delta));
					}
				}
			}
			if (nativeParallelHashMap.Count() == 0)
			{
				return;
			}
			NativeArray<Entity> keyArray = nativeParallelHashMap.GetKeyArray(Allocator.Temp);
			for (int k = 0; k < keyArray.Length; k++)
			{
				Entity entity = keyArray[k];
				Damaged damaged = nativeParallelHashMap[entity];
				if (m_DamagedData.HasComponent(entity))
				{
					if (math.any(damaged.m_Damage > 0f))
					{
						m_DamagedData[entity] = damaged;
						continue;
					}
					m_CommandBuffer.AddComponent(entity, default(BatchesUpdated));
					m_CommandBuffer.RemoveComponent<Damaged>(entity);
					if (m_VehicleData.HasComponent(entity))
					{
						m_CommandBuffer.RemoveComponent<MaintenanceConsumer>(entity);
					}
				}
				else if (math.any(damaged.m_Damage > 0f))
				{
					m_CommandBuffer.AddComponent(entity, damaged);
					m_CommandBuffer.AddComponent(entity, default(BatchesUpdated));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Damage> __Game_Objects_Damage_RO_ComponentTypeHandle;

		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RW_ComponentLookup;

		public ComponentLookup<Damaged> __Game_Objects_Damaged_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Objects_Damage_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Damage>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RW_ComponentLookup = state.GetComponentLookup<Vehicle>();
			__Game_Objects_Damaged_RW_ComponentLookup = state.GetComponentLookup<Damaged>();
		}
	}

	private ModificationBarrier2 m_ModificationBarrier;

	private EntityQuery m_EventQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2>();
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Damage>());
		RequireForUpdate(m_EventQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_EventQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new DamageObjectsJob
		{
			m_Chunks = chunks,
			m_DamageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damage_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RW_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		chunks.Dispose(jobHandle);
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
	public DamageSystem()
	{
	}
}
