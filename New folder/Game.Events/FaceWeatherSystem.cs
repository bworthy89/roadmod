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
public class FaceWeatherSystem : GameSystemBase
{
	[BurstCompile]
	private struct FaceWeatherJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<FaceWeather> m_FaceWeatherType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		public ComponentLookup<FacingWeather> m_FacingWeatherData;

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
			NativeParallelHashMap<Entity, FacingWeather> nativeParallelHashMap = new NativeParallelHashMap<Entity, FacingWeather>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<FaceWeather> nativeArray = m_Chunks[j].GetNativeArray(ref m_FaceWeatherType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					FaceWeather faceWeather = nativeArray[k];
					if (!m_PrefabRefData.HasComponent(faceWeather.m_Target))
					{
						continue;
					}
					FacingWeather facingWeather = new FacingWeather(faceWeather.m_Event, faceWeather.m_Severity);
					if (nativeParallelHashMap.TryGetValue(faceWeather.m_Target, out var item))
					{
						if (facingWeather.m_Severity > item.m_Severity)
						{
							nativeParallelHashMap[faceWeather.m_Target] = facingWeather;
						}
					}
					else if (m_FacingWeatherData.HasComponent(faceWeather.m_Target))
					{
						item = m_FacingWeatherData[faceWeather.m_Target];
						if (facingWeather.m_Severity > item.m_Severity)
						{
							nativeParallelHashMap.TryAdd(faceWeather.m_Target, facingWeather);
						}
					}
					else
					{
						nativeParallelHashMap.TryAdd(faceWeather.m_Target, facingWeather);
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
				FacingWeather facingWeather2 = nativeParallelHashMap[entity];
				if (m_FacingWeatherData.HasComponent(entity))
				{
					if (m_FacingWeatherData[entity].m_Event != facingWeather2.m_Event)
					{
						if (m_TargetElements.HasBuffer(facingWeather2.m_Event))
						{
							CollectionUtils.TryAddUniqueValue(m_TargetElements[facingWeather2.m_Event], new TargetElement(entity));
						}
						AddJournalData(facingWeather2, entity);
					}
					m_FacingWeatherData[entity] = facingWeather2;
				}
				else
				{
					if (m_TargetElements.HasBuffer(facingWeather2.m_Event))
					{
						CollectionUtils.TryAddUniqueValue(m_TargetElements[facingWeather2.m_Event], new TargetElement(entity));
					}
					m_CommandBuffer.AddComponent(entity, facingWeather2);
					AddJournalData(facingWeather2, entity);
				}
			}
		}

		private void AddJournalData(FacingWeather facingWeather, Entity target)
		{
			if (m_BuildingData.HasComponent(target))
			{
				Entity e = m_CommandBuffer.CreateEntity(m_JournalDataArchetype);
				m_CommandBuffer.SetComponent(e, new AddEventJournalData(facingWeather.m_Event, EventDataTrackingType.Damages));
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<FaceWeather> __Game_Events_FaceWeather_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		public ComponentLookup<FacingWeather> __Game_Events_FacingWeather_RW_ComponentLookup;

		public BufferLookup<TargetElement> __Game_Events_TargetElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_FaceWeather_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FaceWeather>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Events_FacingWeather_RW_ComponentLookup = state.GetComponentLookup<FacingWeather>();
			__Game_Events_TargetElement_RW_BufferLookup = state.GetBufferLookup<TargetElement>();
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_FaceWeatherQuery;

	private EntityArchetype m_JournalDataArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_FaceWeatherQuery = GetEntityQuery(ComponentType.ReadOnly<FaceWeather>(), ComponentType.ReadOnly<Game.Common.Event>());
		m_JournalDataArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<AddEventJournalData>(), ComponentType.ReadWrite<Game.Common.Event>());
		RequireForUpdate(m_FaceWeatherQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_FaceWeatherQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new FaceWeatherJob
		{
			m_Chunks = chunks,
			m_FaceWeatherType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_FaceWeather_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FacingWeatherData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_FacingWeather_RW_ComponentLookup, ref base.CheckedStateRef),
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
	public FaceWeatherSystem()
	{
	}
}
