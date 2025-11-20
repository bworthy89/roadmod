using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ApplyNotificationsSystem : GameSystemBase
{
	[BurstCompile]
	private struct ApplyTempIconsJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Icon> m_IconType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<IconElement> m_IconElementType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Icon> m_IconData;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ToolErrorData> m_ToolErrorData;

		[ReadOnly]
		public BufferLookup<IconElement> m_IconElements;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelMultiHashMap<Entity, Entity> nativeParallelMultiHashMap = new NativeParallelMultiHashMap<Entity, Entity>(num, Allocator.Temp);
			NativeHashMap<Entity, Entity> nativeHashMap = new NativeHashMap<Entity, Entity>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[j];
				if (archetypeChunk.Has(ref m_IconType))
				{
					NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
					NativeArray<Owner> nativeArray2 = archetypeChunk.GetNativeArray(ref m_OwnerType);
					NativeArray<PrefabRef> nativeArray3 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						Entity entity = nativeArray[k];
						Owner owner = nativeArray2[k];
						PrefabRef prefabRef = nativeArray3[k];
						bool flag = false;
						if (m_ToolErrorData.HasComponent(prefabRef.m_Prefab))
						{
							flag = (m_ToolErrorData[prefabRef.m_Prefab].m_Flags & ToolErrorFlags.TemporaryOnly) != 0;
						}
						if (m_TempData.HasComponent(owner.m_Owner))
						{
							Temp temp = m_TempData[owner.m_Owner];
							if (temp.m_Original != Entity.Null)
							{
								if (flag)
								{
									m_CommandBuffer.AddComponent(entity, default(Deleted));
								}
								else
								{
									nativeParallelMultiHashMap.Add(temp.m_Original, entity);
								}
								nativeHashMap.TryAdd(temp.m_Original, Entity.Null);
							}
							else if (flag)
							{
								m_CommandBuffer.AddComponent(entity, default(Deleted));
							}
							else
							{
								m_CommandBuffer.RemoveComponent<Temp>(entity);
								m_CommandBuffer.AddComponent(entity, in m_AppliedTypes);
							}
						}
						else
						{
							m_CommandBuffer.AddComponent(entity, default(Deleted));
						}
					}
					continue;
				}
				NativeArray<Entity> nativeArray4 = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Temp> nativeArray5 = archetypeChunk.GetNativeArray(ref m_TempType);
				bool flag2 = archetypeChunk.Has(ref m_IconElementType);
				for (int l = 0; l < nativeArray5.Length; l++)
				{
					Entity entity2 = nativeArray4[l];
					Temp temp2 = nativeArray5[l];
					if (temp2.m_Original != Entity.Null)
					{
						if (m_IconElements.HasBuffer(temp2.m_Original) && !nativeHashMap.TryAdd(temp2.m_Original, entity2))
						{
							nativeHashMap[temp2.m_Original] = entity2;
						}
						if (flag2)
						{
							m_CommandBuffer.RemoveComponent<IconElement>(entity2);
						}
					}
				}
			}
			NativeArray<Entity> keyArray = nativeHashMap.GetKeyArray(Allocator.Temp);
			NativeList<IconElement> list = new NativeList<IconElement>(32, Allocator.Temp);
			for (int m = 0; m < keyArray.Length; m++)
			{
				Entity entity3 = keyArray[m];
				DynamicBuffer<IconElement> dynamicBuffer = default(DynamicBuffer<IconElement>);
				DynamicBuffer<IconElement> dynamicBuffer2 = default(DynamicBuffer<IconElement>);
				if (m_IconElements.HasBuffer(entity3))
				{
					dynamicBuffer2 = m_IconElements[entity3];
					for (int n = 0; n < dynamicBuffer2.Length; n++)
					{
						list.Add(dynamicBuffer2[n]);
					}
				}
				if (nativeParallelMultiHashMap.TryGetFirstValue(entity3, out var item, out var it))
				{
					if (!dynamicBuffer.IsCreated)
					{
						dynamicBuffer = ((!dynamicBuffer2.IsCreated) ? m_CommandBuffer.AddBuffer<IconElement>(entity3) : m_CommandBuffer.SetBuffer<IconElement>(entity3));
					}
					do
					{
						PrefabRef prefabRef2 = m_PrefabRefData[item];
						m_TargetData.TryGetComponent(item, out var componentData);
						int num2;
						Entity icon;
						if (dynamicBuffer2.IsCreated)
						{
							for (num2 = 0; num2 < list.Length; num2++)
							{
								icon = list[num2].m_Icon;
								if (m_PrefabRefData[icon].m_Prefab != prefabRef2.m_Prefab)
								{
									continue;
								}
								m_TargetData.TryGetComponent(icon, out var componentData2);
								if (!(componentData2.m_Target == componentData.m_Target))
								{
									continue;
								}
								goto IL_041d;
							}
						}
						dynamicBuffer.Add(new IconElement(item));
						m_CommandBuffer.SetComponent(item, new Owner(entity3));
						m_CommandBuffer.RemoveComponent<Temp>(item);
						m_CommandBuffer.AddComponent(item, in m_AppliedTypes);
						continue;
						IL_041d:
						Icon component = m_IconData[item];
						component.m_ClusterIndex = m_IconData[icon].m_ClusterIndex;
						m_CommandBuffer.SetComponent(icon, component);
						m_CommandBuffer.AddComponent(icon, default(Updated));
						m_CommandBuffer.AddComponent(item, default(Deleted));
						dynamicBuffer.Add(new IconElement(icon));
						CollectionUtils.Remove(list, num2);
					}
					while (nativeParallelMultiHashMap.TryGetNextValue(out item, ref it));
				}
				if (!dynamicBuffer2.IsCreated)
				{
					continue;
				}
				Entity tempContainer = nativeHashMap[entity3];
				for (int num3 = 0; num3 < list.Length; num3++)
				{
					Entity icon2 = list[num3].m_Icon;
					if (ValidateOldIcon(icon2, entity3, tempContainer))
					{
						if (!dynamicBuffer.IsCreated)
						{
							dynamicBuffer = m_CommandBuffer.SetBuffer<IconElement>(entity3);
						}
						dynamicBuffer.Add(new IconElement(icon2));
					}
					else
					{
						m_CommandBuffer.AddComponent(icon2, default(Deleted));
					}
				}
				if (!dynamicBuffer.IsCreated)
				{
					m_CommandBuffer.RemoveComponent<IconElement>(entity3);
				}
				list.Clear();
			}
		}

		private bool ValidateOldIcon(Entity icon, Entity container, Entity tempContainer)
		{
			PrefabRef prefabRef = m_PrefabRefData[icon];
			if (m_ToolErrorData.HasComponent(prefabRef.m_Prefab))
			{
				return false;
			}
			if (tempContainer != Entity.Null && (int)m_IconData[icon].m_Priority >= 250 && m_DestroyedData.HasComponent(container) && !m_DestroyedData.HasComponent(tempContainer))
			{
				return false;
			}
			return true;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Icon> __Game_Notifications_Icon_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<IconElement> __Game_Notifications_IconElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Icon> __Game_Notifications_Icon_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ToolErrorData> __Game_Prefabs_ToolErrorData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<IconElement> __Game_Notifications_IconElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Icon>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Notifications_IconElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<IconElement>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentLookup = state.GetComponentLookup<Icon>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ToolErrorData_RO_ComponentLookup = state.GetComponentLookup<ToolErrorData>(isReadOnly: true);
			__Game_Notifications_IconElement_RO_BufferLookup = state.GetBufferLookup<IconElement>(isReadOnly: true);
		}
	}

	private ToolOutputBarrier m_ToolOutputBarrier;

	private EntityQuery m_TempQuery;

	private ComponentTypeSet m_AppliedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_TempQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_TempQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new ApplyTempIconsJob
		{
			m_Chunks = chunks,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IconType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IconElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ToolErrorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ToolErrorData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IconElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_AppliedTypes = m_AppliedTypes,
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		chunks.Dispose(jobHandle);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
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
	public ApplyNotificationsSystem()
	{
	}
}
