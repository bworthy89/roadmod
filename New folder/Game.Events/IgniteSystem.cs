using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class IgniteSystem : GameSystemBase
{
	[BurstCompile]
	private struct IgniteFireJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Ignite> m_IgniteType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		public ComponentLookup<OnFire> m_OnFireData;

		public BufferLookup<TargetElement> m_TargetElements;

		public EntityArchetype m_JournalDataArchetype;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, OnFire> nativeParallelHashMap = new NativeParallelHashMap<Entity, OnFire>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<Ignite> nativeArray = m_Chunks[j].GetNativeArray(ref m_IgniteType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Ignite ignite = nativeArray[k];
					if (!m_PrefabRefData.HasComponent(ignite.m_Target))
					{
						continue;
					}
					OnFire onFire = new OnFire(ignite.m_Event, ignite.m_Intensity, ignite.m_RequestFrame);
					if (nativeParallelHashMap.TryGetValue(ignite.m_Target, out var item))
					{
						if (onFire.m_Intensity > item.m_Intensity)
						{
							nativeParallelHashMap[ignite.m_Target] = onFire;
						}
					}
					else if (m_OnFireData.HasComponent(ignite.m_Target))
					{
						item = m_OnFireData[ignite.m_Target];
						if (onFire.m_Intensity > item.m_Intensity)
						{
							nativeParallelHashMap.TryAdd(ignite.m_Target, onFire);
						}
					}
					else
					{
						nativeParallelHashMap.TryAdd(ignite.m_Target, onFire);
					}
				}
			}
			if (nativeParallelHashMap.Count() == 0)
			{
				return;
			}
			NativeArray<Entity> keyArray = nativeParallelHashMap.GetKeyArray(Allocator.Temp);
			for (int l = 0; l < keyArray.Length; l++)
			{
				Entity entity = keyArray[l];
				OnFire onFire2 = nativeParallelHashMap[entity];
				if (m_OnFireData.HasComponent(entity))
				{
					OnFire onFire3 = m_OnFireData[entity];
					if (onFire3.m_Event != onFire2.m_Event)
					{
						if (m_TargetElements.HasBuffer(onFire2.m_Event))
						{
							CollectionUtils.TryAddUniqueValue(m_TargetElements[onFire2.m_Event], new TargetElement(entity));
						}
						AddJournalData(entity, onFire2);
					}
					if (onFire3.m_RequestFrame < onFire2.m_RequestFrame)
					{
						onFire2.m_RequestFrame = onFire3.m_RequestFrame;
					}
					onFire2.m_RescueRequest = onFire3.m_RescueRequest;
					m_OnFireData[entity] = onFire2;
					continue;
				}
				if (m_TargetElements.HasBuffer(onFire2.m_Event))
				{
					CollectionUtils.TryAddUniqueValue(m_TargetElements[onFire2.m_Event], new TargetElement(entity));
				}
				m_CommandBuffer.AddComponent(entity, onFire2);
				m_CommandBuffer.AddComponent(entity, default(BatchesUpdated));
				AddJournalData(entity, onFire2);
				if (!m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData))
				{
					continue;
				}
				for (int m = 0; m < bufferData.Length; m++)
				{
					Entity upgrade = bufferData[m].m_Upgrade;
					if (!m_BuildingData.HasComponent(upgrade))
					{
						m_CommandBuffer.AddComponent<BatchesUpdated>(upgrade);
					}
				}
			}
		}

		private void AddJournalData(Entity target, OnFire onFire)
		{
			if (m_BuildingData.HasComponent(target))
			{
				Entity e = m_CommandBuffer.CreateEntity(m_JournalDataArchetype);
				m_CommandBuffer.SetComponent(e, new AddEventJournalData(onFire.m_Event, EventDataTrackingType.Damages));
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Ignite> __Game_Events_Ignite_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		public ComponentLookup<OnFire> __Game_Events_OnFire_RW_ComponentLookup;

		public BufferLookup<TargetElement> __Game_Events_TargetElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_Ignite_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Ignite>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Events_OnFire_RW_ComponentLookup = state.GetComponentLookup<OnFire>();
			__Game_Events_TargetElement_RW_BufferLookup = state.GetBufferLookup<TargetElement>();
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_IgniteQuery;

	private EntityArchetype m_JournalDataArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_IgniteQuery = GetEntityQuery(ComponentType.ReadOnly<Ignite>(), ComponentType.ReadOnly<Game.Common.Event>());
		m_JournalDataArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<AddEventJournalData>(), ComponentType.ReadWrite<Game.Common.Event>());
		RequireForUpdate(m_IgniteQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_IgniteQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new IgniteFireJob
		{
			m_Chunks = chunks,
			m_IgniteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Ignite_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_JournalDataArchetype = m_JournalDataArchetype,
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
	public IgniteSystem()
	{
	}
}
