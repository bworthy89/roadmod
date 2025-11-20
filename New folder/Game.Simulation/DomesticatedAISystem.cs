using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Net;
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
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class DomesticatedAISystem : GameSystemBase
{
	[BurstCompile]
	private struct DomesticatedGroupTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> m_GroupMemberType;

		public ComponentTypeHandle<AnimalCurrentLane> m_CurrentLaneType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Animal> m_AnimalData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<GroupMember> nativeArray2 = chunk.GetNativeArray(ref m_GroupMemberType);
			NativeArray<AnimalCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_CurrentLaneType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				GroupMember groupMember = nativeArray2[i];
				if (!CollectionUtils.TryGet(nativeArray3, i, out var value))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(AnimalCurrentLane));
				}
				Animal animal = m_AnimalData[entity];
				CreatureUtils.CheckUnspawned(unfilteredChunkIndex, entity, value, animal, isUnspawned, m_CommandBuffer);
				TickGroupMemberWalking(unfilteredChunkIndex, entity, groupMember, ref animal, ref value);
				m_AnimalData[entity] = animal;
				CollectionUtils.TrySet(nativeArray3, i, value);
			}
		}

		private void TickGroupMemberWalking(int jobIndex, Entity entity, GroupMember groupMember, ref Animal animal, ref AnimalCurrentLane currentLane)
		{
			if (CreatureUtils.IsStuck(currentLane))
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
			}
			else if (m_AnimalData.HasComponent(groupMember.m_Leader))
			{
				Animal animal2 = m_AnimalData[groupMember.m_Leader];
				animal.m_Flags = (AnimalFlags)(((uint)animal.m_Flags & 0xFFFFFFF9u) | (uint)(animal2.m_Flags & (AnimalFlags.SwimmingTarget | AnimalFlags.FlyingTarget)));
				if (((animal.m_Flags ^ animal2.m_Flags) & AnimalFlags.Roaming) != 0 && (currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0)
				{
					animal.m_Flags ^= AnimalFlags.Roaming;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DomesticatedTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Animal> m_AnimalType;

		public ComponentTypeHandle<Game.Creatures.Domesticated> m_DomesticatedType;

		public ComponentTypeHandle<AnimalCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<AnimalNavigation> m_NavigationType;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<AnimalData> m_PrefabAnimalData;

		[ReadOnly]
		public ComponentLookup<DomesticatedData> m_PrefabDomesticatedData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Animal> nativeArray3 = chunk.GetNativeArray(ref m_AnimalType);
			NativeArray<Game.Creatures.Domesticated> nativeArray4 = chunk.GetNativeArray(ref m_DomesticatedType);
			NativeArray<AnimalCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<AnimalNavigation> nativeArray6 = chunk.GetNativeArray(ref m_NavigationType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				Animal animal = nativeArray3[i];
				Game.Creatures.Domesticated domesticated = nativeArray4[i];
				AnimalNavigation navigation = nativeArray6[i];
				if (!CollectionUtils.TryGet(nativeArray5, i, out var value))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(AnimalCurrentLane));
				}
				CreatureUtils.CheckUnspawned(unfilteredChunkIndex, entity, value, animal, isUnspawned, m_CommandBuffer);
				TickWalking(unfilteredChunkIndex, entity, prefabRef, ref random, ref animal, ref domesticated, ref value, ref navigation);
				nativeArray3[i] = animal;
				nativeArray4[i] = domesticated;
				nativeArray6[i] = navigation;
				CollectionUtils.TrySet(nativeArray5, i, value);
			}
		}

		private void TickWalking(int jobIndex, Entity entity, PrefabRef prefabRef, ref Unity.Mathematics.Random random, ref Animal animal, ref Game.Creatures.Domesticated domesticated, ref AnimalCurrentLane currentLane, ref AnimalNavigation navigation)
		{
			if (CreatureUtils.IsStuck(currentLane))
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
				return;
			}
			if (currentLane.m_Lane != Entity.Null)
			{
				animal.m_Flags &= ~AnimalFlags.Roaming;
			}
			else
			{
				animal.m_Flags |= AnimalFlags.Roaming;
			}
			if (CreatureUtils.PathEndReached(currentLane))
			{
				PathEndReached(jobIndex, entity, prefabRef, ref random, ref animal, ref domesticated, ref currentLane, ref navigation);
			}
		}

		private bool PathEndReached(int jobIndex, Entity entity, PrefabRef prefabRef, ref Unity.Mathematics.Random random, ref Animal animal, ref Game.Creatures.Domesticated domesticated, ref AnimalCurrentLane currentLane, ref AnimalNavigation navigation)
		{
			AnimalData animalData = m_PrefabAnimalData[prefabRef.m_Prefab];
			DomesticatedData domesticatedData = m_PrefabDomesticatedData[prefabRef.m_Prefab];
			if ((domesticated.m_Flags & DomesticatedFlags.Idling) != DomesticatedFlags.None)
			{
				if (--domesticated.m_StateTime > 0)
				{
					return false;
				}
				domesticated.m_Flags &= ~DomesticatedFlags.Idling;
				domesticated.m_Flags |= DomesticatedFlags.Wandering;
			}
			else if ((domesticated.m_Flags & DomesticatedFlags.Wandering) != DomesticatedFlags.None)
			{
				if ((animal.m_Flags & AnimalFlags.FlyingTarget) == 0)
				{
					float num = 3.75f;
					int min = Mathf.RoundToInt(domesticatedData.m_IdleTime.min * num);
					int num2 = Mathf.RoundToInt(domesticatedData.m_IdleTime.max * num);
					domesticated.m_StateTime = (ushort)math.clamp(random.NextInt(min, num2 + 1), 0, 65535);
					if (domesticated.m_StateTime > 0)
					{
						domesticated.m_Flags &= ~DomesticatedFlags.Wandering;
						domesticated.m_Flags |= DomesticatedFlags.Idling;
						return false;
					}
				}
			}
			else
			{
				domesticated.m_Flags |= DomesticatedFlags.Wandering;
			}
			if (m_OwnerData.TryGetComponent(entity, out var componentData) && m_TransformData.TryGetComponent(componentData.m_Owner, out var componentData2))
			{
				if ((animal.m_Flags & AnimalFlags.Roaming) != 0)
				{
					Game.Objects.Transform transform = m_TransformData[entity];
					if ((animal.m_Flags & AnimalFlags.FlyingTarget) != 0)
					{
						if (random.NextInt(5) == 0)
						{
							animal.m_Flags &= ~AnimalFlags.FlyingTarget;
						}
					}
					else if (animalData.m_FlySpeed > 0f && (animalData.m_MoveSpeed == 0f || random.NextInt(3) == 0))
					{
						animal.m_Flags |= AnimalFlags.FlyingTarget;
					}
					Entity owner = componentData.m_Owner;
					while (m_OwnerData.HasComponent(owner) && !m_BuildingData.HasComponent(owner))
					{
						owner = m_OwnerData[owner].m_Owner;
					}
					float2 x = 16f;
					if (owner != Entity.Null)
					{
						PrefabRef prefabRef2 = m_PrefabRefData[owner];
						if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData3))
						{
							x = componentData3.m_Size.xz;
						}
					}
					float2 @float = math.length(x) * new float2(0.2f, 0.9f);
					currentLane.m_Flags &= ~(CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached);
					navigation.m_TargetPosition = math.lerp(transform.m_Position, componentData2.m_Position, 0.5f);
					navigation.m_TargetPosition.xz += random.NextFloat2Direction() * random.NextFloat(@float.x, @float.y);
					if ((animal.m_Flags & AnimalFlags.SwimmingTarget) != 0)
					{
						Bounds1 bounds = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition) - MathUtils.Invert(animalData.m_SwimDepth);
						navigation.m_TargetPosition.y = random.NextFloat(bounds.min, bounds.max);
					}
					else if ((animal.m_Flags & AnimalFlags.FlyingTarget) != 0)
					{
						Bounds1 bounds2 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition) + animalData.m_FlyHeight;
						navigation.m_TargetPosition.y = random.NextFloat(bounds2.min, bounds2.max);
					}
					else
					{
						if (animalData.m_FlySpeed > 0f)
						{
							float2 float2 = navigation.m_TargetPosition.xz - transform.m_Position.xz;
							navigation.m_TargetPosition.xz = transform.m_Position.xz + float2 * (animalData.m_MoveSpeed / animalData.m_FlySpeed);
						}
						navigation.m_TargetPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, navigation.m_TargetPosition);
					}
					return false;
				}
				if ((currentLane.m_Flags & CreatureLaneFlags.Area) != 0 && m_LaneData.TryGetComponent(currentLane.m_Lane, out var componentData4) && m_OwnerData.TryGetComponent(currentLane.m_Lane, out var componentData5) && m_SubLanes.TryGetBuffer(componentData5.m_Owner, out var bufferData))
				{
					int num3 = 0;
					int num4 = 0;
					Entity a = Entity.Null;
					Entity b = Entity.Null;
					bool a2 = false;
					bool b2 = false;
					for (int i = 0; i < bufferData.Length; i++)
					{
						Game.Net.SubLane subLane = bufferData[i];
						if (subLane.m_SubLane == currentLane.m_Lane || !m_LaneData.TryGetComponent(subLane.m_SubLane, out var componentData6))
						{
							continue;
						}
						int num5 = 100;
						if (componentData6.m_StartNode.Equals(componentData4.m_EndNode) || componentData6.m_EndNode.Equals(componentData4.m_EndNode))
						{
							num3 += num5;
							if (random.NextInt(num3) < num5)
							{
								a = subLane.m_SubLane;
								a2 = componentData6.m_StartNode.Equals(componentData4.m_EndNode);
							}
						}
						if (componentData6.m_StartNode.Equals(componentData4.m_StartNode) || componentData6.m_EndNode.Equals(componentData4.m_StartNode))
						{
							num4 += num5;
							if (random.NextInt(num4) < num5)
							{
								b = subLane.m_SubLane;
								b2 = componentData6.m_StartNode.Equals(componentData4.m_StartNode);
							}
						}
					}
					float num6;
					if ((currentLane.m_Flags & CreatureLaneFlags.Backward) != 0)
					{
						CommonUtils.Swap(ref a, ref b);
						CommonUtils.Swap(ref a2, ref b2);
						num6 = 0f;
					}
					else
					{
						num6 = 1f;
					}
					if (a == Entity.Null)
					{
						a = b;
						a2 = b2;
						num6 = math.select(0f, 1f, num6 == 0f);
					}
					currentLane.m_Flags &= ~(CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached);
					if (a != Entity.Null)
					{
						currentLane.m_NextLane = a;
						currentLane.m_NextPosition.x = math.select(1f, 0f, a2);
						currentLane.m_NextPosition.y = random.NextFloat(0f, 1f);
						currentLane.m_NextFlags = currentLane.m_Flags;
						currentLane.m_CurvePosition.y = num6;
						if (currentLane.m_NextPosition.y > currentLane.m_NextPosition.x)
						{
							currentLane.m_NextFlags &= ~CreatureLaneFlags.Backward;
						}
						else if (currentLane.m_NextPosition.y < currentLane.m_NextPosition.x)
						{
							currentLane.m_NextFlags |= CreatureLaneFlags.Backward;
						}
					}
					else
					{
						currentLane.m_NextLane = Entity.Null;
						currentLane.m_CurvePosition.y = random.NextFloat(0f, 1f);
					}
					if (currentLane.m_CurvePosition.y > currentLane.m_CurvePosition.x)
					{
						currentLane.m_Flags &= ~CreatureLaneFlags.Backward;
					}
					else if (currentLane.m_CurvePosition.y < currentLane.m_CurvePosition.x)
					{
						currentLane.m_Flags |= CreatureLaneFlags.Backward;
					}
					return false;
				}
			}
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
			return true;
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
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Animal> __Game_Creatures_Animal_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Creatures.Domesticated> __Game_Creatures_Domesticated_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AnimalNavigation> __Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalData> __Game_Prefabs_AnimalData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DomesticatedData> __Game_Prefabs_DomesticatedData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> __Game_Creatures_GroupMember_RO_ComponentTypeHandle;

		public ComponentLookup<Animal> __Game_Creatures_Animal_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Creatures_Animal_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Animal>();
			__Game_Creatures_Domesticated_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Creatures.Domesticated>();
			__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalCurrentLane>();
			__Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalNavigation>();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_AnimalData_RO_ComponentLookup = state.GetComponentLookup<AnimalData>(isReadOnly: true);
			__Game_Prefabs_DomesticatedData_RO_ComponentLookup = state.GetComponentLookup<DomesticatedData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
			__Game_Creatures_Animal_RW_ComponentLookup = state.GetComponentLookup<Animal>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_CreatureQuery;

	private EntityQuery m_GroupCreatureQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 9;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_CreatureQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Creatures.Domesticated>(), ComponentType.ReadWrite<Animal>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<GroupMember>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Stumbling>());
		m_GroupCreatureQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Creatures.Domesticated>(), ComponentType.ReadWrite<Animal>(), ComponentType.ReadOnly<GroupMember>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Stumbling>());
		RequireAnyForUpdate(m_CreatureQuery, m_GroupCreatureQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		DomesticatedTickJob jobData = new DomesticatedTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Animal_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DomesticatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Domesticated_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AnimalData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDomesticatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DomesticatedData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		DomesticatedGroupTickJob jobData2 = new DomesticatedGroupTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroupMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Animal_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = jobData.m_CommandBuffer
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_CreatureQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData2, m_GroupCreatureQuery, jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
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
	public DomesticatedAISystem()
	{
	}
}
