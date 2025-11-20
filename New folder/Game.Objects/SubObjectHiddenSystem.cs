using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Tools;
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
public class SubObjectHiddenSystem : GameSystemBase
{
	[BurstCompile]
	private struct FillTempMapJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		public NativeParallelHashMap<Entity, Entity>.ParallelWriter m_TempMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity item = nativeArray[i];
				Temp temp = nativeArray3[i];
				if (temp.m_Original != Entity.Null)
				{
					m_TempMap.TryAdd(temp.m_Original, item);
				}
				if (nativeArray2.Length != 0)
				{
					Owner owner = nativeArray2[i];
					if (owner.m_Owner != Entity.Null && !m_TempData.HasComponent(owner.m_Owner))
					{
						m_TempMap.TryAdd(owner.m_Owner, item);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct HiddenSubObjectJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		[ReadOnly]
		public ComponentTypeHandle<Object> m_ObjectType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> m_VehicleType;

		[ReadOnly]
		public ComponentTypeHandle<Creature> m_CreatureType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public NativeParallelHashMap<Entity, Entity> m_TempMap;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			if (chunk.Has(ref m_ObjectType))
			{
				NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
				if (nativeArray2.Length != 0)
				{
					if (bufferAccessor.Length != 0)
					{
						if (chunk.Has(ref m_VehicleType) || chunk.Has(ref m_CreatureType) || chunk.Has(ref m_BuildingType))
						{
							for (int i = 0; i < bufferAccessor.Length; i++)
							{
								EnsureHidden(unfilteredChunkIndex, nativeArray[i], bufferAccessor[i]);
							}
							return;
						}
						StackList<Entity> stackList = stackalloc Entity[nativeArray2.Length];
						for (int j = 0; j < nativeArray2.Length; j++)
						{
							Entity entity = nativeArray[j];
							Owner owner = nativeArray2[j];
							if (!m_HiddenData.HasComponent(owner.m_Owner))
							{
								if (!m_TempMap.ContainsKey(entity) && !m_TempMap.ContainsKey(owner.m_Owner))
								{
									stackList.AddNoResize(entity);
									EnsureVisible(unfilteredChunkIndex, entity, bufferAccessor[j]);
								}
							}
							else
							{
								EnsureHidden(unfilteredChunkIndex, entity, bufferAccessor[j]);
							}
						}
						if (stackList.Length != 0)
						{
							m_CommandBuffer.AddComponent<BatchesUpdated>(unfilteredChunkIndex, stackList.AsArray());
							m_CommandBuffer.RemoveComponent<Hidden>(unfilteredChunkIndex, stackList.AsArray());
						}
						return;
					}
					StackList<Entity> stackList2 = stackalloc Entity[nativeArray2.Length];
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						Entity entity2 = nativeArray[k];
						Owner owner2 = nativeArray2[k];
						if (!m_HiddenData.HasComponent(owner2.m_Owner) && !m_TempMap.ContainsKey(entity2) && !m_TempMap.ContainsKey(owner2.m_Owner))
						{
							stackList2.AddNoResize(entity2);
						}
					}
					if (stackList2.Length != 0)
					{
						m_CommandBuffer.AddComponent<BatchesUpdated>(unfilteredChunkIndex, stackList2.AsArray());
						m_CommandBuffer.RemoveComponent<Hidden>(unfilteredChunkIndex, stackList2.AsArray());
					}
					return;
				}
			}
			for (int l = 0; l < bufferAccessor.Length; l++)
			{
				EnsureHidden(unfilteredChunkIndex, nativeArray[l], bufferAccessor[l]);
			}
		}

		private void EnsureHidden(int jobIndex, Entity parent, DynamicBuffer<SubObject> subObjects)
		{
			StackList<Entity> stackList = stackalloc Entity[subObjects.Length];
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (!m_HiddenData.HasComponent(subObject) && !m_BuildingData.HasComponent(subObject) && m_OwnerData.TryGetComponent(subObject, out var componentData) && !(componentData.m_Owner != parent))
				{
					stackList.AddNoResize(subObject);
					if (m_SubObjects.HasBuffer(subObject))
					{
						EnsureHidden(jobIndex, subObject, m_SubObjects[subObject]);
					}
				}
			}
			if (stackList.Length != 0)
			{
				m_CommandBuffer.AddComponent<Hidden>(jobIndex, stackList.AsArray());
				m_CommandBuffer.AddComponent<BatchesUpdated>(jobIndex, stackList.AsArray());
			}
		}

		private void EnsureVisible(int jobIndex, Entity parent, DynamicBuffer<SubObject> subObjects)
		{
			StackList<Entity> stackList = stackalloc Entity[subObjects.Length];
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_HiddenData.HasComponent(subObject) && !m_BuildingData.HasComponent(subObject) && !m_TempMap.ContainsKey(subObject) && m_OwnerData.TryGetComponent(subObject, out var componentData) && !(componentData.m_Owner != parent))
				{
					stackList.AddNoResize(subObject);
					if (m_SubObjects.TryGetBuffer(subObject, out var bufferData))
					{
						EnsureVisible(jobIndex, subObject, bufferData);
					}
				}
			}
			if (stackList.Length != 0)
			{
				m_CommandBuffer.AddComponent<BatchesUpdated>(jobIndex, stackList.AsArray());
				m_CommandBuffer.RemoveComponent<Hidden>(jobIndex, stackList.AsArray());
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
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Object> __Game_Objects_Object_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Creature> __Game_Creatures_Creature_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Objects_Object_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Object>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Vehicle>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Creature>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_HiddenQuery;

	private EntityQuery m_TempQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_HiddenQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<SubObject>()
			},
			Any = new ComponentType[0],
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Object>(),
				ComponentType.ReadOnly<Owner>()
			},
			Any = new ComponentType[0],
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Vehicle>(),
				ComponentType.ReadOnly<Creature>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_TempQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Object>()
			},
			Any = new ComponentType[0],
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		RequireForUpdate(m_HiddenQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeParallelHashMap<Entity, Entity> tempMap = new NativeParallelHashMap<Entity, Entity>(m_TempQuery.CalculateEntityCount() * 2, Allocator.TempJob);
		FillTempMapJob jobData = new FillTempMapJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempMap = tempMap.AsParallelWriter()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new HiddenSubObjectJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Object_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_TempMap = tempMap,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, dependsOn: JobChunkExtensions.ScheduleParallel(jobData, m_TempQuery, base.Dependency), query: m_HiddenQuery);
		tempMap.Dispose(jobHandle);
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
	public SubObjectHiddenSystem()
	{
	}
}
