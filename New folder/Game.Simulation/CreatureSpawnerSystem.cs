using System.Runtime.CompilerServices;
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
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CreatureSpawnerSystem : GameSystemBase
{
	[BurstCompile]
	private struct CreatureSpawnerJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.SpawnLocation> m_SpawnLocationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public BufferTypeHandle<OwnedCreature> m_OwnedCreatureType;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<GroupMember> m_GroupMemberData;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Domesticated> m_DomesticatedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<CreatureSpawnData> m_PrefabCreatureSpawnData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_PrefabObjectData;

		[ReadOnly]
		public ComponentLookup<AnimalData> m_PrefabAnimalData;

		[ReadOnly]
		public ComponentLookup<WildlifeData> m_PrefabWildlifeData;

		[ReadOnly]
		public ComponentLookup<DomesticatedData> m_PrefabDomesticatedData;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderObjects;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public ComponentTypeSet m_AnimalSpawnTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Game.Objects.SpawnLocation> nativeArray3 = chunk.GetNativeArray(ref m_SpawnLocationType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<OwnedCreature> bufferAccessor = chunk.GetBufferAccessor(ref m_OwnedCreatureType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				Entity spawner = nativeArray[i];
				Transform transform = nativeArray2[i];
				PrefabRef prefabRef = nativeArray4[i];
				DynamicBuffer<OwnedCreature> dynamicBuffer = bufferAccessor[i];
				CreatureSpawnData creatureSpawnData = m_PrefabCreatureSpawnData[prefabRef.m_Prefab];
				int num = 0;
				Entity group = Entity.Null;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity creature = dynamicBuffer[j].m_Creature;
					if (m_CreatureData.HasComponent(creature))
					{
						if (!m_GroupMemberData.HasComponent(creature))
						{
							num++;
						}
						if (group == Entity.Null && m_DomesticatedData.HasComponent(creature))
						{
							PrefabRef prefabRef2 = m_PrefabRefData[creature];
							group = ((!m_PrefabSpawnableObjectData.TryGetComponent(prefabRef2.m_Prefab, out var componentData)) ? prefabRef2.m_Prefab : componentData.m_RandomizationGroup);
						}
					}
					else
					{
						dynamicBuffer.RemoveAtSwapBack(j--);
					}
				}
				if (num >= random.NextInt(creatureSpawnData.m_MaxGroupCount + 1))
				{
					continue;
				}
				DynamicBuffer<PlaceholderObjectElement> placeholderObjects = m_PrefabPlaceholderObjects[prefabRef.m_Prefab];
				Game.Objects.SpawnLocation spawnLocation = default(Game.Objects.SpawnLocation);
				if (nativeArray3.Length != 0)
				{
					spawnLocation = nativeArray3[i];
				}
				Entity entity = SelectPrefab(placeholderObjects, ref random, ref group);
				int num2 = 1;
				if (m_PrefabWildlifeData.HasComponent(entity))
				{
					WildlifeData wildlifeData = m_PrefabWildlifeData[entity];
					num2 = random.NextInt(wildlifeData.m_GroupMemberCount.x, wildlifeData.m_GroupMemberCount.y + 1);
				}
				else if (m_PrefabDomesticatedData.HasComponent(entity))
				{
					DomesticatedData domesticatedData = m_PrefabDomesticatedData[entity];
					num2 = random.NextInt(domesticatedData.m_GroupMemberCount.x, domesticatedData.m_GroupMemberCount.y + 1);
				}
				for (int k = 0; k < num2; k++)
				{
					if (k != 0)
					{
						entity = SelectPrefab(placeholderObjects, ref random, ref group);
					}
					if (!(entity == Entity.Null))
					{
						SpawnCreature(unfilteredChunkIndex, spawner, entity, transform, spawnLocation, new PseudoRandomSeed(ref random));
					}
				}
			}
		}

		private Entity SelectPrefab(DynamicBuffer<PlaceholderObjectElement> placeholderObjects, ref Random random, ref Entity group)
		{
			int num = 0;
			Entity result = Entity.Null;
			Entity entity = Entity.Null;
			for (int i = 0; i < placeholderObjects.Length; i++)
			{
				PlaceholderObjectElement placeholderObjectElement = placeholderObjects[i];
				if (!m_PrefabSpawnableObjectData.HasComponent(placeholderObjectElement.m_Object))
				{
					continue;
				}
				SpawnableObjectData spawnableObjectData = m_PrefabSpawnableObjectData[placeholderObjectElement.m_Object];
				Entity entity2 = ((spawnableObjectData.m_RandomizationGroup != Entity.Null) ? spawnableObjectData.m_RandomizationGroup : placeholderObjectElement.m_Object);
				if (!(group != Entity.Null) || !(group != entity2))
				{
					num += spawnableObjectData.m_Probability;
					if (random.NextInt(num) < spawnableObjectData.m_Probability)
					{
						result = placeholderObjectElement.m_Object;
						entity = entity2;
					}
				}
			}
			group = entity;
			return result;
		}

		private void SpawnCreature(int jobIndex, Entity spawner, Entity prefab, Transform transform, Game.Objects.SpawnLocation spawnLocation, PseudoRandomSeed randomSeed)
		{
			ObjectData objectData = m_PrefabObjectData[prefab];
			if (m_PrefabAnimalData.HasComponent(prefab))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
				m_CommandBuffer.AddComponent(jobIndex, e, in m_AnimalSpawnTypes);
				m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(prefab));
				m_CommandBuffer.SetComponent(jobIndex, e, transform);
				m_CommandBuffer.SetComponent(jobIndex, e, randomSeed);
				m_CommandBuffer.SetComponent(jobIndex, e, new Owner(spawner));
				m_CommandBuffer.SetComponent(jobIndex, e, new TripSource(spawner));
				if (spawnLocation.m_ConnectedLane1 == Entity.Null)
				{
					m_CommandBuffer.SetComponent(jobIndex, e, new Animal(AnimalFlags.Roaming));
					m_CommandBuffer.SetComponent(jobIndex, e, new AnimalNavigation(transform.m_Position));
					m_CommandBuffer.SetComponent(jobIndex, e, default(AnimalCurrentLane));
				}
				else
				{
					m_CommandBuffer.SetComponent(jobIndex, e, new AnimalCurrentLane(spawnLocation.m_ConnectedLane1, spawnLocation.m_CurvePosition1, (CreatureLaneFlags)0u));
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public BufferTypeHandle<OwnedCreature> __Game_Creatures_OwnedCreature_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Creature> __Game_Creatures_Creature_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GroupMember> __Game_Creatures_GroupMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Domesticated> __Game_Creatures_Domesticated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CreatureSpawnData> __Game_Prefabs_CreatureSpawnData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalData> __Game_Prefabs_AnimalData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WildlifeData> __Game_Prefabs_WildlifeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DomesticatedData> __Game_Prefabs_DomesticatedData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Creatures_OwnedCreature_RW_BufferTypeHandle = state.GetBufferTypeHandle<OwnedCreature>();
			__Game_Creatures_Creature_RO_ComponentLookup = state.GetComponentLookup<Creature>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentLookup = state.GetComponentLookup<GroupMember>(isReadOnly: true);
			__Game_Creatures_Domesticated_RO_ComponentLookup = state.GetComponentLookup<Game.Creatures.Domesticated>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CreatureSpawnData_RO_ComponentLookup = state.GetComponentLookup<CreatureSpawnData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Prefabs_AnimalData_RO_ComponentLookup = state.GetComponentLookup<AnimalData>(isReadOnly: true);
			__Game_Prefabs_WildlifeData_RO_ComponentLookup = state.GetComponentLookup<WildlifeData>(isReadOnly: true);
			__Game_Prefabs_DomesticatedData_RO_ComponentLookup = state.GetComponentLookup<DomesticatedData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_SpawnerQuery;

	private ComponentTypeSet m_AnimalSpawnTypes;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SpawnerQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Creatures.CreatureSpawner>(), ComponentType.ReadWrite<OwnedCreature>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		m_AnimalSpawnTypes = new ComponentTypeSet(ComponentType.ReadWrite<AnimalCurrentLane>(), ComponentType.ReadWrite<Owner>(), ComponentType.ReadWrite<TripSource>(), ComponentType.ReadWrite<Unspawned>());
		RequireForUpdate(m_SpawnerQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CreatureSpawnerJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnedCreatureType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Creatures_OwnedCreature_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_CreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GroupMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DomesticatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Domesticated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCreatureSpawnData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CreatureSpawnData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AnimalData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWildlifeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WildlifeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDomesticatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DomesticatedData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_AnimalSpawnTypes = m_AnimalSpawnTypes,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_SpawnerQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public CreatureSpawnerSystem()
	{
	}
}
