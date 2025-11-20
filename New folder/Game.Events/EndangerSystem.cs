using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class EndangerSystem : GameSystemBase
{
	[BurstCompile]
	private struct EndangerJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public ComponentTypeHandle<Endanger> m_EndangerType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.School> m_SchoolData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Hospital> m_HospitalData;

		public ComponentLookup<InDanger> m_InDangerData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, InDanger> nativeParallelHashMap = new NativeParallelHashMap<Entity, InDanger>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<Endanger> nativeArray = m_Chunks[j].GetNativeArray(ref m_EndangerType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Endanger endanger = nativeArray[k];
					if (!m_PrefabRefData.HasComponent(endanger.m_Target))
					{
						continue;
					}
					if ((endanger.m_Flags & DangerFlags.Evacuate) != 0 && (m_SchoolData.HasComponent(endanger.m_Target) || m_HospitalData.HasComponent(endanger.m_Target)))
					{
						endanger.m_Flags |= DangerFlags.UseTransport;
					}
					InDanger inDanger = new InDanger(endanger.m_Event, Entity.Null, endanger.m_Flags, endanger.m_EndFrame);
					if (nativeParallelHashMap.TryGetValue(endanger.m_Target, out var item))
					{
						if (EventUtils.IsWorse(inDanger.m_Flags, item.m_Flags) || item.m_EndFrame < m_SimulationFrame + 64)
						{
							nativeParallelHashMap[endanger.m_Target] = inDanger;
						}
					}
					else if (m_InDangerData.HasComponent(endanger.m_Target))
					{
						item = m_InDangerData[endanger.m_Target];
						if (EventUtils.IsWorse(inDanger.m_Flags, item.m_Flags) || item.m_EndFrame < m_SimulationFrame + 64)
						{
							nativeParallelHashMap.TryAdd(endanger.m_Target, inDanger);
						}
					}
					else
					{
						nativeParallelHashMap.TryAdd(endanger.m_Target, inDanger);
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
				InDanger inDanger2 = nativeParallelHashMap[entity];
				if (m_InDangerData.HasComponent(entity))
				{
					InDanger inDanger3 = m_InDangerData[entity];
					m_InDangerData[entity] = inDanger2;
					if (inDanger2.m_Flags != inDanger3.m_Flags)
					{
						m_CommandBuffer.AddComponent(entity, default(EffectsUpdated));
					}
				}
				else
				{
					m_CommandBuffer.AddComponent(entity, inDanger2);
					m_CommandBuffer.AddComponent(entity, default(EffectsUpdated));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Endanger> __Game_Events_Endanger_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.School> __Game_Buildings_School_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentLookup;

		public ComponentLookup<InDanger> __Game_Events_InDanger_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_Endanger_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Endanger>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_School_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.School>(isReadOnly: true);
			__Game_Buildings_Hospital_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Hospital>(isReadOnly: true);
			__Game_Events_InDanger_RW_ComponentLookup = state.GetComponentLookup<InDanger>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_EndangerQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_EndangerQuery = GetEntityQuery(ComponentType.ReadOnly<Endanger>(), ComponentType.ReadOnly<Game.Common.Event>());
		RequireForUpdate(m_EndangerQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_EndangerQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new EndangerJob
		{
			m_Chunks = chunks,
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_EndangerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Endanger_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SchoolData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_School_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HospitalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InDangerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InDanger_RW_ComponentLookup, ref base.CheckedStateRef),
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
	public EndangerSystem()
	{
	}
}
