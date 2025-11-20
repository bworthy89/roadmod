#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class SpawnableAmbienceSystem : GameSystemBase
{
	private struct GroupAmbienceEffect
	{
		public GroupAmbienceType m_Type;

		public float m_Amount;

		public int m_CellIndex;
	}

	[BurstCompile]
	private struct ApplyAmbienceJob : IJobParallelFor
	{
		public NativeParallelQueue<GroupAmbienceEffect>.Reader m_SpawnableQueue;

		public NativeArray<ZoneAmbienceCell> m_ZoneAmbienceMap;

		public void Execute(int index)
		{
			NativeParallelQueue<GroupAmbienceEffect>.Enumerator enumerator = m_SpawnableQueue.GetEnumerator(index);
			while (enumerator.MoveNext())
			{
				GroupAmbienceEffect current = enumerator.Current;
				m_ZoneAmbienceMap.ElementAt(current.m_CellIndex).m_Accumulator.AddAmbience(current.m_Type, current.m_Amount);
			}
			enumerator.Dispose();
		}
	}

	[BurstCompile]
	private struct EmitterAmbienceJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<AmbienceEmitterData> m_AmbienceEmitterDatas;

		public NativeParallelQueue<GroupAmbienceEffect>.Writer m_Queue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Transform> nativeArray = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < chunk.Count; i++)
			{
				float3 position = nativeArray[i].m_Position;
				PrefabRef prefabRef = nativeArray2[i];
				if (!m_AmbienceEmitterDatas.HasComponent(prefabRef.m_Prefab))
				{
					continue;
				}
				AmbienceEmitterData ambienceEmitterData = m_AmbienceEmitterDatas[prefabRef.m_Prefab];
				if (ambienceEmitterData.m_Intensity != 0f)
				{
					int2 cell = CellMapSystem<ZoneAmbienceCell>.GetCell(position, CellMapSystem<ZoneAmbienceCell>.kMapSize, ZoneAmbienceSystem.kTextureSize);
					int num = cell.x + cell.y * ZoneAmbienceSystem.kTextureSize;
					int hashCode = num * m_Queue.HashRange / (ZoneAmbienceSystem.kTextureSize * ZoneAmbienceSystem.kTextureSize);
					if (cell.x >= 0 && cell.y >= 0 && cell.x < ZoneAmbienceSystem.kTextureSize && cell.y < ZoneAmbienceSystem.kTextureSize)
					{
						m_Queue.Enqueue(hashCode, new GroupAmbienceEffect
						{
							m_Amount = ambienceEmitterData.m_Intensity,
							m_Type = ambienceEmitterData.m_AmbienceType,
							m_CellIndex = num
						});
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
	private struct SpawnableAmbienceJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<GroupAmbienceData> m_SpawnableAmbienceDatas;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		public NativeParallelQueue<GroupAmbienceEffect>.Writer m_Queue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Transform> nativeArray = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			if (bufferAccessor.Length != 0)
			{
				NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
				BufferAccessor<Efficiency> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EfficiencyType);
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity prefab = nativeArray2[i].m_Prefab;
					if (m_SpawnableAmbienceDatas.TryGetComponent(prefab, out var componentData) && m_BuildingDatas.TryGetComponent(prefab, out var componentData2))
					{
						float3 position = nativeArray[i].m_Position;
						int num = componentData2.m_LotSize.x * componentData2.m_LotSize.y;
						float amount = (float)(bufferAccessor[i].Length * num) * BuildingUtils.GetEfficiency(bufferAccessor2, i);
						int2 cell = CellMapSystem<ZoneAmbienceCell>.GetCell(position, CellMapSystem<ZoneAmbienceCell>.kMapSize, ZoneAmbienceSystem.kTextureSize);
						int num2 = cell.x + cell.y * ZoneAmbienceSystem.kTextureSize;
						int hashCode = num2 * m_Queue.HashRange / (ZoneAmbienceSystem.kTextureSize * ZoneAmbienceSystem.kTextureSize);
						if (cell.x >= 0 && cell.y >= 0 && cell.x < ZoneAmbienceSystem.kTextureSize && cell.y < ZoneAmbienceSystem.kTextureSize)
						{
							m_Queue.Enqueue(hashCode, new GroupAmbienceEffect
							{
								m_Amount = amount,
								m_Type = componentData.m_AmbienceType,
								m_CellIndex = num2
							});
						}
					}
				}
				return;
			}
			for (int j = 0; j < chunk.Count; j++)
			{
				int2 cell2 = CellMapSystem<ZoneAmbienceCell>.GetCell(nativeArray[j].m_Position, CellMapSystem<ZoneAmbienceCell>.kMapSize, ZoneAmbienceSystem.kTextureSize);
				int num3 = cell2.x + cell2.y * ZoneAmbienceSystem.kTextureSize;
				int hashCode2 = num3 * m_Queue.HashRange / (ZoneAmbienceSystem.kTextureSize * ZoneAmbienceSystem.kTextureSize);
				if (cell2.x >= 0 && cell2.y >= 0 && cell2.x < ZoneAmbienceSystem.kTextureSize && cell2.y < ZoneAmbienceSystem.kTextureSize)
				{
					m_Queue.Enqueue(hashCode2, new GroupAmbienceEffect
					{
						m_Amount = 1f,
						m_Type = GroupAmbienceType.Forest,
						m_CellIndex = num3
					});
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GroupAmbienceData> __Game_Prefabs_GroupAmbienceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AmbienceEmitterData> __Game_Prefabs_AmbienceEmitterData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_GroupAmbienceData_RO_ComponentLookup = state.GetComponentLookup<GroupAmbienceData>(isReadOnly: true);
			__Game_Prefabs_AmbienceEmitterData_RO_ComponentLookup = state.GetComponentLookup<AmbienceEmitterData>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 128;

	private SimulationSystem m_SimulationSystem;

	private ZoneAmbienceSystem m_ZoneAmbienceSystem;

	private EntityQuery m_SpawnableQuery;

	private EntityQuery m_EmitterQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ZoneAmbienceSystem = base.World.GetOrCreateSystemManaged<ZoneAmbienceSystem>();
		m_SpawnableQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<BuildingCondition>(),
				ComponentType.ReadOnly<UpdateFrame>(),
				ComponentType.ReadOnly<Renter>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<ResidentialProperty>(),
				ComponentType.ReadOnly<Efficiency>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Abandoned>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Tree>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_EmitterQuery = GetEntityQuery(ComponentType.ReadOnly<AmbienceEmitter>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		m_SpawnableQuery.ResetFilter();
		m_SpawnableQuery.SetSharedComponentFilter(new UpdateFrame(updateFrame));
		NativeParallelQueue<GroupAmbienceEffect> nativeParallelQueue = new NativeParallelQueue<GroupAmbienceEffect>(math.max(1, JobsUtility.JobWorkerCount / 2), Allocator.TempJob);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new SpawnableAmbienceJob
		{
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableAmbienceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GroupAmbienceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Queue = nativeParallelQueue.AsWriter()
		}, m_SpawnableQuery, base.Dependency);
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new EmitterAmbienceJob
		{
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AmbienceEmitterDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AmbienceEmitterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Queue = nativeParallelQueue.AsWriter()
		}, m_EmitterQuery, jobHandle);
		JobHandle dependencies;
		JobHandle jobHandle3 = IJobParallelForExtensions.Schedule(new ApplyAmbienceJob
		{
			m_SpawnableQueue = nativeParallelQueue.AsReader(),
			m_ZoneAmbienceMap = m_ZoneAmbienceSystem.GetMap(readOnly: false, out dependencies)
		}, nativeParallelQueue.HashRange, 1, JobHandle.CombineDependencies(jobHandle, dependencies, jobHandle2));
		m_ZoneAmbienceSystem.AddWriter(jobHandle3);
		nativeParallelQueue.Dispose(jobHandle3);
		base.Dependency = jobHandle2;
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
	public SpawnableAmbienceSystem()
	{
	}
}
