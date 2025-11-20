using System;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
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
public class DestroySystem : GameSystemBase
{
	[BurstCompile]
	private struct DestroyObjectsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Destroy> m_DestroyType;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumers;

		[ReadOnly]
		public ComponentLookup<Clip> m_ClipAreas;

		[ReadOnly]
		public ComponentLookup<Space> m_SpaceAreas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<AreaData> m_AreaData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_MeshData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

		[ReadOnly]
		public BufferLookup<SubMesh> m_PrefabSubMeshes;

		public NativeHashSet<Entity> m_ProcessedObjects;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<Entity> m_UpdatedElectricityRoadEdges;

		public NativeQueue<Entity> m_UpdatedWaterPipeRoadEdges;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public ComponentTypeSet m_DestroyedBuildingComponents;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Destroy> nativeArray = chunk.GetNativeArray(ref m_DestroyType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < chunk.Count; i++)
			{
				Destroy destroyEvent = nativeArray[i];
				DestroyObject(ref random, destroyEvent.m_Object, destroyEvent);
			}
		}

		private void DestroyObject(ref Unity.Mathematics.Random random, Entity entity, Destroy destroyEvent)
		{
			if (m_DestroyedData.HasComponent(entity) || !m_ProcessedObjects.Add(entity) || !m_PrefabRefs.TryGetComponent(entity, out var componentData))
			{
				return;
			}
			float num = 0f;
			if (m_PrefabObjectGeometryData.TryGetComponent(componentData.m_Prefab, out var componentData2) && (componentData2.m_Flags & (GeometryFlags.Physical | GeometryFlags.HasLot)) == (GeometryFlags.Physical | GeometryFlags.HasLot))
			{
				num = BuildingUtils.GetCollapseTime(componentData2.m_Size.y);
				bool flag = false;
				if (m_PrefabSubMeshes.TryGetBuffer(componentData.m_Prefab, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						SubMesh subMesh = bufferData[i];
						if (m_MeshData.TryGetComponent(subMesh.m_SubMesh, out var componentData3))
						{
							float2 @float = MathUtils.Center(componentData3.m_Bounds.xz);
							float2 float2 = MathUtils.Extents(componentData3.m_Bounds.xz);
							float3 v = math.rotate(subMesh.m_Rotation, new float3(float2.x, 0f, 0f));
							float3 v2 = math.rotate(subMesh.m_Rotation, new float3(0f, 0f, float2.y));
							float3 position = subMesh.m_Position + math.rotate(subMesh.m_Rotation, new float3(@float.x, 0f, @float.y));
							Transform transform = m_TransformData[entity];
							v = math.rotate(transform.m_Rotation, v);
							v2 = math.rotate(transform.m_Rotation, v2);
							position = ObjectUtils.LocalToWorld(transform, position);
							Entity result = m_BuildingConfigurationData.m_CollapsedSurface;
							if (m_PrefabPlaceholderElements.TryGetBuffer(result, out var bufferData2))
							{
								AreaUtils.SelectAreaPrefab(bufferData2, m_PrefabSpawnableObjectData, default(NativeParallelHashMap<Entity, int>), ref random, out result, out var _);
							}
							AreaData areaData = m_AreaData[result];
							Entity entity2 = m_CommandBuffer.CreateEntity(areaData.m_Archetype);
							m_CommandBuffer.SetComponent(entity2, new PrefabRef(result));
							m_CommandBuffer.AddComponent(entity2, new Owner(entity));
							if (m_NativeData.HasComponent(entity))
							{
								m_CommandBuffer.AddComponent(entity2, default(Native));
							}
							DynamicBuffer<Node> dynamicBuffer = m_CommandBuffer.SetBuffer<Node>(entity2);
							dynamicBuffer.ResizeUninitialized(32);
							float4 float3 = random.NextInt4(3, 10);
							float4 float4 = random.NextFloat(-MathF.PI, MathF.PI);
							float num2 = MathF.PI * -2f / (float)dynamicBuffer.Length;
							for (int j = 0; j < dynamicBuffer.Length; j++)
							{
								float num3 = (float)j * num2;
								float2 x = new float2(math.cos(num3), math.sin(num3));
								x = math.sign(x) * math.sqrt(math.abs(x));
								x *= 1f + math.dot(math.sin(num3 * float3 + float4), 0.025f);
								float3 position2 = position + v * x.x + v2 * x.y;
								dynamicBuffer[j] = new Node(position2, float.MinValue);
							}
							m_CommandBuffer.SetComponent(entity2, new Area(AreaFlags.Complete));
							flag = true;
						}
					}
				}
				if (m_SubAreas.TryGetBuffer(entity, out var bufferData3))
				{
					for (int k = 0; k < bufferData3.Length; k++)
					{
						Entity area = bufferData3[k].m_Area;
						if (m_ClipAreas.HasComponent(area) || (m_SpaceAreas.HasComponent(area) && !IsAnyOnGround(m_AreaNodes[area])))
						{
							m_CommandBuffer.AddComponent<Deleted>(bufferData3[k].m_Area);
						}
					}
				}
				else if (flag)
				{
					m_CommandBuffer.AddBuffer<Game.Areas.SubArea>(entity);
				}
			}
			Destroyed component = new Destroyed(destroyEvent.m_Event);
			if (num != 0f)
			{
				component.m_Cleared = 0.5f - math.max(1f, num);
			}
			m_CommandBuffer.AddComponent(entity, component);
			if (num != 0f)
			{
				m_CommandBuffer.AddComponent(entity, new InterpolatedTransform(m_TransformData[entity]));
			}
			m_CommandBuffer.AddComponent<Updated>(entity);
			if (m_Buildings.TryGetComponent(entity, out var componentData4))
			{
				m_CommandBuffer.RemoveComponent(entity, in m_DestroyedBuildingComponents);
				if (componentData4.m_RoadEdge != Entity.Null)
				{
					if (m_ElectricityConsumers.HasComponent(entity))
					{
						m_UpdatedElectricityRoadEdges.Enqueue(componentData4.m_RoadEdge);
					}
					if (m_WaterConsumers.HasComponent(entity))
					{
						m_UpdatedWaterPipeRoadEdges.Enqueue(componentData4.m_RoadEdge);
					}
				}
			}
			if (!m_SubObjects.TryGetBuffer(entity, out var bufferData4))
			{
				return;
			}
			for (int l = 0; l < bufferData4.Length; l++)
			{
				Entity subObject = bufferData4[l].m_SubObject;
				if (!m_Buildings.HasComponent(subObject))
				{
					DestroyObject(ref random, subObject, destroyEvent);
				}
			}
		}

		private bool IsAnyOnGround(DynamicBuffer<Node> nodes)
		{
			for (int i = 0; i < nodes.Length; i++)
			{
				if (nodes[i].m_Elevation == float.MinValue)
				{
					return true;
				}
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Destroy> __Game_Objects_Destroy_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Clip> __Game_Areas_Clip_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Space> __Game_Areas_Space_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaData> __Game_Prefabs_AreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Objects_Destroy_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroy>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Areas_Clip_RO_ComponentLookup = state.GetComponentLookup<Clip>(isReadOnly: true);
			__Game_Areas_Space_RO_ComponentLookup = state.GetComponentLookup<Space>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_AreaData_RO_ComponentLookup = state.GetComponentLookup<AreaData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Node>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
		}
	}

	private ModificationBarrier2 m_ModificationBarrier;

	private ElectricityRoadConnectionGraphSystem m_ElectricityRoadConnectionGraphSystem;

	private WaterPipeRoadConnectionGraphSystem m_WaterPipeRoadConnectionGraphSystem;

	private EntityQuery m_EventQuery;

	private EntityQuery m_BuildingConfigurationQuery;

	private ComponentTypeSet m_DestroyedBuildingComponents;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2>();
		m_ElectricityRoadConnectionGraphSystem = base.World.GetOrCreateSystemManaged<ElectricityRoadConnectionGraphSystem>();
		m_WaterPipeRoadConnectionGraphSystem = base.World.GetOrCreateSystemManaged<WaterPipeRoadConnectionGraphSystem>();
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Destroy>());
		m_BuildingConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		m_DestroyedBuildingComponents = new ComponentTypeSet(ComponentType.ReadOnly<ElectricityConsumer>(), ComponentType.ReadOnly<WaterConsumer>(), ComponentType.ReadOnly<GarbageProducer>(), ComponentType.ReadOnly<MailProducer>());
		RequireForUpdate(m_EventQuery);
		RequireForUpdate(m_BuildingConfigurationQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		JobHandle deps2;
		DestroyObjectsJob jobData = new DestroyObjectsJob
		{
			m_DestroyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Destroy_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ClipAreas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Clip_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpaceAreas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Space_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProcessedObjects = new NativeHashSet<Entity>(32, Allocator.TempJob),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
			m_UpdatedElectricityRoadEdges = m_ElectricityRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps),
			m_UpdatedWaterPipeRoadEdges = m_WaterPipeRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps2),
			m_RandomSeed = RandomSeed.Next(),
			m_DestroyedBuildingComponents = m_DestroyedBuildingComponents,
			m_BuildingConfigurationData = m_BuildingConfigurationQuery.GetSingleton<BuildingConfigurationData>()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_EventQuery, JobHandle.CombineDependencies(base.Dependency, deps, deps2));
		jobData.m_ProcessedObjects.Dispose(base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		m_ElectricityRoadConnectionGraphSystem.AddQueueWriter(base.Dependency);
		m_WaterPipeRoadConnectionGraphSystem.AddQueueWriter(base.Dependency);
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
	public DestroySystem()
	{
	}
}
