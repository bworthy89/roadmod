using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class OutsideConnectionInitializeSystem : GameSystemBase, IPostDeserialize
{
	[BurstCompile]
	private struct CollectOutsideConnectionsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public BufferTypeHandle<RandomLocalizationIndex> m_RandomLocalizationIndexType;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionData;

		public NativeList<OutsideConnectionInfo>.ParallelWriter m_OutsideConnections;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<RandomLocalizationIndex> bufferAccessor = chunk.GetBufferAccessor(ref m_RandomLocalizationIndexType);
			for (int i = 0; i < chunk.Count; i++)
			{
				DynamicBuffer<RandomLocalizationIndex> dynamicBuffer = bufferAccessor[i];
				if (dynamicBuffer.Length == 1)
				{
					m_OutsideConnections.AddNoResize(new OutsideConnectionInfo
					{
						m_TransferType = GetTransferType(nativeArray[i].m_Prefab, ref m_OutsideConnectionData),
						m_Position = nativeArray2[i].m_Position,
						m_RandomIndex = dynamicBuffer[0]
					});
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct InitializeLocalizationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		public BufferTypeHandle<RandomLocalizationIndex> m_RandomLocalizationIndexType;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionData;

		[ReadOnly]
		public BufferLookup<LocalizationCount> m_LocalizationCounts;

		public NativeList<OutsideConnectionInfo> m_OutsideConnections;

		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<RandomLocalizationIndex> bufferAccessor = chunk.GetBufferAccessor(ref m_RandomLocalizationIndexType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				float3 position = nativeArray3[i].m_Position;
				DynamicBuffer<RandomLocalizationIndex> indices = bufferAccessor[i];
				if (m_LocalizationCounts.TryGetBuffer(prefab, out var bufferData))
				{
					OutsideConnectionTransferType transferType = GetTransferType(prefab, ref m_OutsideConnectionData);
					if (bufferData.Length == 1 && TryGetNearestConnectionRandomIndex(m_OutsideConnections, transferType, position, out var randomIndex) && randomIndex.m_Index <= bufferData[0].m_Count)
					{
						indices.ResizeUninitialized(1);
						indices[0] = randomIndex;
					}
					else
					{
						Random random = m_RandomSeed.GetRandom(entity.Index + 1);
						RandomLocalizationIndex.GenerateRandomIndices(indices, bufferData, ref random);
					}
					if (indices.Length == 1)
					{
						ref NativeList<OutsideConnectionInfo> reference = ref m_OutsideConnections;
						OutsideConnectionInfo value = new OutsideConnectionInfo
						{
							m_TransferType = transferType,
							m_Position = position,
							m_RandomIndex = indices[0]
						};
						reference.Add(in value);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct OutsideConnectionInfo
	{
		public OutsideConnectionTransferType m_TransferType;

		public float3 m_Position;

		public RandomLocalizationIndex m_RandomIndex;
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RandomLocalizationIndex> __Game_Common_RandomLocalizationIndex_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public BufferTypeHandle<RandomLocalizationIndex> __Game_Common_RandomLocalizationIndex_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<LocalizationCount> __Game_Prefabs_LocalizationCount_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Common_RandomLocalizationIndex_RO_BufferTypeHandle = state.GetBufferTypeHandle<RandomLocalizationIndex>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_RandomLocalizationIndex_RW_BufferTypeHandle = state.GetBufferTypeHandle<RandomLocalizationIndex>();
			__Game_Prefabs_LocalizationCount_RO_BufferLookup = state.GetBufferLookup<LocalizationCount>(isReadOnly: true);
		}
	}

	private const float kNearbyMaxDistanceSqr = 10000f;

	private EntityQuery m_ExistingQuery;

	private EntityQuery m_CreatedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ExistingQuery = GetEntityQuery(ComponentType.ReadOnly<OutsideConnection>(), ComponentType.ReadOnly<RandomLocalizationIndex>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Created>());
		m_CreatedQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<OutsideConnection>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadWrite<RandomLocalizationIndex>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CreatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		int initialCapacity = m_ExistingQuery.CalculateEntityCount() + m_CreatedQuery.CalculateEntityCount();
		NativeList<OutsideConnectionInfo> outsideConnections = new NativeList<OutsideConnectionInfo>(initialCapacity, Allocator.TempJob);
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(new CollectOutsideConnectionsJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RandomLocalizationIndexType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Common_RandomLocalizationIndex_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnections = outsideConnections.AsParallelWriter()
		}, m_ExistingQuery, base.Dependency);
		InitializeLocalizationJob jobData = new InitializeLocalizationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RandomLocalizationIndexType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Common_RandomLocalizationIndex_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalizationCounts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LocalizationCount_RO_BufferLookup, ref base.CheckedStateRef),
			m_OutsideConnections = outsideConnections,
			m_RandomSeed = RandomSeed.Next()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_CreatedQuery, dependsOn);
		outsideConnections.Dispose(base.Dependency);
	}

	public void PostDeserialize(Context context)
	{
		if (!(context.version < Version.outsideConnNames))
		{
			return;
		}
		EntityQuery entityQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<OutsideConnection>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<RandomLocalizationIndex>());
		if (!entityQuery.IsEmptyIgnoreFilter)
		{
			NativeList<OutsideConnectionInfo> connections = new NativeList<OutsideConnectionInfo>(Allocator.TempJob);
			NativeArray<Entity> nativeArray = entityQuery.ToEntityArray(Allocator.TempJob);
			NativeArray<PrefabRef> nativeArray2 = entityQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
			NativeArray<Transform> nativeArray3 = entityQuery.ToComponentDataArray<Transform>(Allocator.TempJob);
			RandomSeed randomSeed = RandomSeed.Next();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity prefab = nativeArray2[i].m_Prefab;
				OutsideConnectionData component;
				OutsideConnectionTransferType transferType = (base.EntityManager.TryGetComponent<OutsideConnectionData>(prefab, out component) ? component.m_Type : OutsideConnectionTransferType.None);
				float3 position = nativeArray3[i].m_Position;
				if (base.EntityManager.HasBuffer<LocalizationCount>(prefab))
				{
					DynamicBuffer<RandomLocalizationIndex> indices = base.EntityManager.AddBuffer<RandomLocalizationIndex>(nativeArray[i]);
					DynamicBuffer<LocalizationCount> buffer = base.EntityManager.GetBuffer<LocalizationCount>(prefab, isReadOnly: true);
					if (buffer.Length == 1 && TryGetNearestConnectionRandomIndex(connections, transferType, position, out var randomIndex) && randomIndex.m_Index < buffer[0].m_Count)
					{
						indices.ResizeUninitialized(1);
						indices[0] = randomIndex;
					}
					else
					{
						Random random = randomSeed.GetRandom(nativeArray[i].Index + 1);
						RandomLocalizationIndex.GenerateRandomIndices(indices, buffer, ref random);
					}
					if (indices.Length == 1)
					{
						OutsideConnectionInfo value = new OutsideConnectionInfo
						{
							m_TransferType = transferType,
							m_Position = position,
							m_RandomIndex = indices[0]
						};
						connections.Add(in value);
					}
				}
			}
			nativeArray.Dispose();
			nativeArray2.Dispose();
			nativeArray3.Dispose();
			connections.Dispose();
		}
		entityQuery.Dispose();
	}

	private static OutsideConnectionTransferType GetTransferType(Entity prefab, ref ComponentLookup<OutsideConnectionData> outsideConnectionData)
	{
		if (!outsideConnectionData.TryGetComponent(prefab, out var componentData))
		{
			return OutsideConnectionTransferType.None;
		}
		return componentData.m_Type;
	}

	private static bool TryGetNearestConnectionRandomIndex(NativeList<OutsideConnectionInfo> connections, OutsideConnectionTransferType transferType, float3 position, out RandomLocalizationIndex randomIndex)
	{
		randomIndex = default(RandomLocalizationIndex);
		float num = 10000f;
		foreach (OutsideConnectionInfo item in connections)
		{
			if ((item.m_TransferType & transferType) != OutsideConnectionTransferType.None)
			{
				float num2 = math.distancesq(item.m_Position, position);
				if (num2 < num)
				{
					randomIndex = item.m_RandomIndex;
					num = num2;
				}
			}
		}
		return num < 10000f;
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
	public OutsideConnectionInitializeSystem()
	{
	}
}
