#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Events;
using Game.Notifications;
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
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class FireSimulationSystem : GameSystemBase
{
	[BurstCompile]
	private struct FireSimulationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Tree> m_TreeType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public ComponentTypeHandle<OnFire> m_OnFireType;

		public ComponentTypeHandle<Damaged> m_DamagedType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<FireData> m_PrefabFireData;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Resources> m_ResourcesData;

		[ReadOnly]
		public BufferLookup<DistrictModifier> m_DistrictModifiers;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public EntityArchetype m_FireRescueRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_DamageEventArchetype;

		[ReadOnly]
		public EntityArchetype m_DestroyEventArchetype;

		[ReadOnly]
		public FireConfigurationData m_FireConfigurationData;

		[ReadOnly]
		public EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

		[ReadOnly]
		public CellMapData<TelecomCoverage> m_TelecomCoverageData;

		[ReadOnly]
		public float m_TimeOfDay;

		[ReadOnly]
		public LocalEffectSystem.ReadData m_LocalEffectData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			float num = 1.0666667f;
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<OnFire> nativeArray3 = chunk.GetNativeArray(ref m_OnFireType);
			NativeArray<Damaged> nativeArray4 = chunk.GetNativeArray(ref m_DamagedType);
			NativeArray<Transform> nativeArray5 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<CurrentDistrict> nativeArray6 = chunk.GetNativeArray(ref m_CurrentDistrictType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			bool flag = chunk.Has(ref m_BuildingType);
			bool flag2 = chunk.Has(ref m_TreeType);
			bool flag3 = chunk.Has(ref m_DestroyedType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				ref OnFire reference = ref nativeArray3.ElementAt(i);
				bool flag4 = false;
				if (nativeArray4.Length != 0)
				{
					flag4 = nativeArray4[i].m_Damage.y >= 1f;
				}
				if (reference.m_Intensity > 0f)
				{
					if (m_PrefabRefData.TryGetComponent(reference.m_Event, out var componentData))
					{
						FireData fireData = m_PrefabFireData[componentData.m_Prefab];
						if (flag4)
						{
							reference.m_Intensity = math.max(0f, reference.m_Intensity - 2f * fireData.m_EscalationRate * num);
						}
						else
						{
							reference.m_Intensity = math.min(100f, reference.m_Intensity + fireData.m_EscalationRate * num);
						}
					}
					else
					{
						reference.m_Intensity = 0f;
					}
				}
				if (reference.m_Intensity > 0f && !flag4)
				{
					float structuralIntegrity = m_StructuralIntegrityData.GetStructuralIntegrity(prefabRef.m_Prefab, flag);
					float num2 = math.min(0.5f, reference.m_Intensity * num / structuralIntegrity);
					if (nativeArray4.Length != 0)
					{
						Damaged damaged = nativeArray4[i];
						damaged.m_Damage.y = math.min(1f, damaged.m_Damage.y + num2);
						flag4 |= damaged.m_Damage.y >= 1f;
						if (!flag3 && ObjectUtils.GetTotalDamage(damaged) == 1f)
						{
							Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_DestroyEventArchetype);
							m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Destroy(entity, reference.m_Event));
							if (!flag2)
							{
								m_IconCommandBuffer.Remove(entity, m_FireConfigurationData.m_FireNotificationPrefab);
								m_IconCommandBuffer.Remove(entity, IconPriority.Problem);
								m_IconCommandBuffer.Remove(entity, IconPriority.FatalProblem);
								m_IconCommandBuffer.Add(entity, m_FireConfigurationData.m_BurnedDownNotificationPrefab, IconPriority.FatalProblem, IconClusterLayer.Default, IconFlags.IgnoreTarget, reference.m_Event);
							}
						}
						nativeArray4[i] = damaged;
					}
					else
					{
						Entity e2 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_DamageEventArchetype);
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, e2, new Damage(entity, new float3(0f, num2, 0f)));
					}
				}
				if (reference.m_Intensity > 0f)
				{
					if (!flag4)
					{
						if (reference.m_RequestFrame == 0)
						{
							Transform transform = nativeArray5[i];
							CurrentDistrict currentDistrict = default(CurrentDistrict);
							if (nativeArray6.Length != 0)
							{
								currentDistrict = nativeArray6[i];
							}
							InitializeRequestFrame(flag, flag2, transform, currentDistrict, ref reference, ref random);
						}
						RequestFireRescueIfNeeded(unfilteredChunkIndex, entity, ref reference);
					}
				}
				else
				{
					if (flag && nativeArray4.Length > 0)
					{
						ObjectUtils.UpdateResourcesDamage(entity, ObjectUtils.GetTotalDamage(nativeArray4[i]), ref m_RenterData, ref m_ResourcesData);
					}
					m_CommandBuffer.RemoveComponent<OnFire>(unfilteredChunkIndex, entity);
					m_CommandBuffer.AddComponent<BatchesUpdated>(unfilteredChunkIndex, entity);
					m_IconCommandBuffer.Remove(entity, m_FireConfigurationData.m_FireNotificationPrefab);
					if (CollectionUtils.TryGet(bufferAccessor2, i, out var value))
					{
						for (int j = 0; j < value.Length; j++)
						{
							Entity upgrade = value[j].m_Upgrade;
							if (!m_BuildingData.HasComponent(upgrade))
							{
								m_CommandBuffer.AddComponent<BatchesUpdated>(unfilteredChunkIndex, upgrade);
							}
						}
					}
				}
				if (bufferAccessor.Length != 0)
				{
					BuildingUtils.SetEfficiencyFactor(bufferAccessor[i], EfficiencyFactor.Fire, (reference.m_Intensity > 0.01f) ? 0f : 1f);
				}
			}
		}

		private void InitializeRequestFrame(bool isBuilding, bool isTree, Transform transform, CurrentDistrict currentDistrict, ref OnFire onFire, ref Random random)
		{
			float num = math.saturate(math.abs(m_TimeOfDay - 0.5f) * 4f - 1f);
			float num2 = TelecomCoverage.SampleNetworkQuality(m_TelecomCoverageData, transform.m_Position);
			float num3 = random.NextFloat(m_FireConfigurationData.m_ResponseTimeRange.min, m_FireConfigurationData.m_ResponseTimeRange.max);
			num3 += num3 * (m_FireConfigurationData.m_DarknessResponseTimeModifier * num);
			num3 += num3 * (m_FireConfigurationData.m_TelecomResponseTimeModifier * num2);
			if (isBuilding && m_DistrictModifiers.HasBuffer(currentDistrict.m_District))
			{
				DynamicBuffer<DistrictModifier> modifiers = m_DistrictModifiers[currentDistrict.m_District];
				AreaUtils.ApplyModifier(ref num3, modifiers, DistrictModifierType.BuildingFireResponseTime);
			}
			if (isTree)
			{
				m_LocalEffectData.ApplyModifier(ref num3, transform.m_Position, LocalModifierType.ForestFireResponseTime);
			}
			int num4 = (int)(num3 * 60f);
			num4 -= 32;
			num4 -= 128;
			onFire.m_RequestFrame = m_SimulationFrame + (uint)math.max(0, num4);
		}

		private void RequestFireRescueIfNeeded(int jobIndex, Entity entity, ref OnFire onFire)
		{
			if (onFire.m_RequestFrame <= m_SimulationFrame && !m_FireRescueRequestData.HasComponent(onFire.m_RescueRequest))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_FireRescueRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new FireRescueRequest(entity, onFire.m_Intensity, FireRescueRequestType.Fire));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
				m_IconCommandBuffer.Add(entity, m_FireConfigurationData.m_FireNotificationPrefab, IconPriority.MajorProblem, IconClusterLayer.Default, IconFlags.IgnoreTarget, onFire.m_Event);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FireSpreadCheckJob : IJobChunk
	{
		private struct ObjectSpreadIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_Event;

			public Random m_Random;

			public float3 m_Position;

			public Bounds3 m_Bounds;

			public float m_Range;

			public float m_Size;

			public float m_StartIntensity;

			public float m_Probability;

			public uint m_RequestFrame;

			public int m_JobIndex;

			public EventHelpers.FireHazardData m_FireHazardData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

			public ComponentLookup<Tree> m_TreeData;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

			public ComponentLookup<Damaged> m_DamagedData;

			public ComponentLookup<UnderConstruction> m_UnderConstructionData;

			public ComponentLookup<Placeholder> m_PlaceholderData;

			public EntityArchetype m_IgniteEventArchetype;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0)
				{
					return false;
				}
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0 || !MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || m_PlaceholderData.HasComponent(item))
				{
					return;
				}
				float riskFactor;
				if (m_BuildingData.HasComponent(item))
				{
					PrefabRef prefabRef = m_PrefabRefData[item];
					Building building = m_BuildingData[item];
					CurrentDistrict currentDistrict = m_CurrentDistrictData[item];
					Transform transform = m_TransformData[item];
					ObjectGeometryData objectGeometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
					float num = math.distance(transform.m_Position, m_Position) - math.cmin(objectGeometryData.m_Size.xz) * 0.5f - m_Size;
					if (num < m_Range)
					{
						m_DamagedData.TryGetComponent(item, out var componentData);
						if (!m_UnderConstructionData.TryGetComponent(item, out var componentData2))
						{
							componentData2 = new UnderConstruction
							{
								m_Progress = byte.MaxValue
							};
						}
						if (m_FireHazardData.GetFireHazard(prefabRef, building, currentDistrict, componentData, componentData2, out var fireHazard, out riskFactor))
						{
							TrySpreadFire(item, fireHazard, num);
						}
					}
				}
				else if (m_TreeData.HasComponent(item))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[item];
					Transform transform2 = m_TransformData[item];
					ObjectGeometryData objectGeometryData2 = m_ObjectGeometryData[prefabRef2.m_Prefab];
					Damaged damaged = default(Damaged);
					if (m_DamagedData.HasComponent(item))
					{
						damaged = m_DamagedData[item];
					}
					float num2 = math.distance(transform2.m_Position, m_Position) - math.cmin(objectGeometryData2.m_Size.xz) * 0.5f - m_Size;
					if (num2 < m_Range && m_FireHazardData.GetFireHazard(prefabRef2, default(Tree), transform2, damaged, out var fireHazard2, out riskFactor))
					{
						TrySpreadFire(item, fireHazard2, math.max(0f, num2));
					}
				}
			}

			private void TrySpreadFire(Entity entity, float fireHazard, float distance)
			{
				if (m_Random.NextFloat(100f) * m_Range < fireHazard * (m_Range - distance) * m_Probability)
				{
					Ignite component = new Ignite
					{
						m_Target = entity,
						m_Event = m_Event,
						m_Intensity = m_StartIntensity,
						m_RequestFrame = m_RequestFrame + 64
					};
					Entity e = m_CommandBuffer.CreateEntity(m_JobIndex, m_IgniteEventArchetype);
					m_CommandBuffer.SetComponent(m_JobIndex, e, component);
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<OnFire> m_OnFireType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<FireData> m_PrefabFireData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<Damaged> m_DamagedData;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> m_UnderConstructionData;

		[ReadOnly]
		public ComponentLookup<Placeholder> m_PlaceholderData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EventHelpers.FireHazardData m_FireHazardData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public EntityArchetype m_IgniteEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<OnFire> nativeArray3 = chunk.GetNativeArray(ref m_OnFireType);
			NativeArray<Transform> nativeArray4 = chunk.GetNativeArray(ref m_TransformType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				OnFire onFire = nativeArray3[i];
				Transform transform = nativeArray4[i];
				if (onFire.m_Intensity > 0f && m_PrefabRefData.HasComponent(onFire.m_Event))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[onFire.m_Event];
					FireData prefabFireData = m_PrefabFireData[prefabRef2.m_Prefab];
					Random random = m_RandomSeed.GetRandom(entity.Index);
					TrySpreadFire(unfilteredChunkIndex, entity, onFire, ref random, prefabRef, transform, prefabFireData);
				}
			}
		}

		private void TrySpreadFire(int jobIndex, Entity entity, OnFire onFire, ref Random random, PrefabRef prefabRef, Transform transform, FireData prefabFireData)
		{
			float num = 1.0666667f;
			float num2 = math.sqrt(prefabFireData.m_SpreadProbability * 0.01f);
			if (random.NextFloat(100f) < onFire.m_Intensity * num2 * num)
			{
				float num3 = math.cmin(m_ObjectGeometryData[prefabRef.m_Prefab].m_Size.xz) * 0.5f;
				float num4 = prefabFireData.m_SpreadRange + num3;
				ObjectSpreadIterator iterator = new ObjectSpreadIterator
				{
					m_Event = onFire.m_Event,
					m_Random = random,
					m_Position = transform.m_Position,
					m_Bounds = new Bounds3(transform.m_Position - num4, transform.m_Position + num4),
					m_Range = prefabFireData.m_SpreadRange,
					m_Size = num3,
					m_StartIntensity = prefabFireData.m_StartIntensity,
					m_Probability = num2,
					m_RequestFrame = onFire.m_RequestFrame,
					m_JobIndex = jobIndex,
					m_FireHazardData = m_FireHazardData,
					m_PrefabRefData = m_PrefabRefData,
					m_BuildingData = m_BuildingData,
					m_CurrentDistrictData = m_CurrentDistrictData,
					m_TreeData = m_TreeData,
					m_TransformData = m_TransformData,
					m_ObjectGeometryData = m_ObjectGeometryData,
					m_DamagedData = m_DamagedData,
					m_UnderConstructionData = m_UnderConstructionData,
					m_PlaceholderData = m_PlaceholderData,
					m_IgniteEventArchetype = m_IgniteEventArchetype,
					m_CommandBuffer = m_CommandBuffer
				};
				m_ObjectSearchTree.Iterate(ref iterator);
				random = iterator.m_Random;
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Tree> __Game_Objects_Tree_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public ComponentTypeHandle<OnFire> __Game_Events_OnFire_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Damaged> __Game_Objects_Damaged_RW_ComponentTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> __Game_Simulation_FireRescueRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireData> __Game_Prefabs_FireData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<OnFire> __Game_Events_OnFire_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Placeholder> __Game_Objects_Placeholder_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Tree>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Events_OnFire_RW_ComponentTypeHandle = state.GetComponentTypeHandle<OnFire>();
			__Game_Objects_Damaged_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Damaged>();
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Simulation_FireRescueRequest_RO_ComponentLookup = state.GetComponentLookup<FireRescueRequest>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_FireData_RO_ComponentLookup = state.GetComponentLookup<FireData>(isReadOnly: true);
			__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Resources>();
			__Game_Events_OnFire_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OnFire>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentLookup = state.GetComponentLookup<UnderConstruction>(isReadOnly: true);
			__Game_Objects_Placeholder_RO_ComponentLookup = state.GetComponentLookup<Placeholder>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private Game.Objects.SearchSystem m_SearchSystem;

	private IconCommandSystem m_IconCommandSystem;

	private LocalEffectSystem m_LocalEffectSystem;

	private PrefabSystem m_PrefabSystem;

	private TelecomCoverageSystem m_TelecomCoverageSystem;

	private TimeSystem m_TimeSystem;

	private ClimateSystem m_ClimateSystem;

	private FireHazardSystem m_FireHazardSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_FireQuery;

	private EntityQuery m_ConfigQuery;

	private EntityArchetype m_FireRescueRequestArchetype;

	private EntityArchetype m_DamageEventArchetype;

	private EntityArchetype m_DestroyEventArchetype;

	private EntityArchetype m_IgniteEventArchetype;

	private EventHelpers.FireHazardData m_FireHazardData;

	private EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_LocalEffectSystem = base.World.GetOrCreateSystemManaged<LocalEffectSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_FireHazardSystem = base.World.GetOrCreateSystemManaged<FireHazardSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_FireQuery = GetEntityQuery(ComponentType.ReadWrite<OnFire>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		m_FireRescueRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<FireRescueRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_DamageEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Damage>());
		m_DestroyEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Destroy>());
		m_IgniteEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Ignite>());
		m_FireHazardData = new EventHelpers.FireHazardData(this);
		m_StructuralIntegrityData = new EventHelpers.StructuralIntegrityData(this);
		RequireForUpdate(m_FireQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		FireConfigurationData singleton = m_ConfigQuery.GetSingleton<FireConfigurationData>();
		JobHandle dependencies;
		LocalEffectSystem.ReadData readData = m_LocalEffectSystem.GetReadData(out dependencies);
		FireConfigurationPrefab prefab = m_PrefabSystem.GetPrefab<FireConfigurationPrefab>(m_ConfigQuery.GetSingletonEntity());
		m_FireHazardData.Update(this, readData, prefab, m_ClimateSystem.temperature, m_FireHazardSystem.noRainDays);
		m_StructuralIntegrityData.Update(this, singleton);
		JobHandle dependencies2;
		FireSimulationJob jobData = new FireSimulationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TreeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OnFireType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_OnFire_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DamagedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damaged_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_FireRescueRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_FireRescueRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_FireData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DistrictModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_RenterData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourcesData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_FireRescueRequestArchetype = m_FireRescueRequestArchetype,
			m_DamageEventArchetype = m_DamageEventArchetype,
			m_DestroyEventArchetype = m_DestroyEventArchetype,
			m_FireConfigurationData = singleton,
			m_StructuralIntegrityData = m_StructuralIntegrityData,
			m_TelecomCoverageData = m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies2),
			m_TimeOfDay = m_TimeSystem.normalizedTime,
			m_LocalEffectData = readData,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		};
		JobHandle dependencies3;
		FireSpreadCheckJob jobData2 = new FireSpreadCheckJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OnFireType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_FireData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnderConstructionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceholderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_FireHazardData = m_FireHazardData,
			m_ObjectSearchTree = m_SearchSystem.GetStaticSearchTree(readOnly: true, out dependencies3),
			m_IgniteEventArchetype = m_IgniteEventArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_FireQuery, JobHandle.CombineDependencies(base.Dependency, dependencies2, dependencies));
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData2, m_FireQuery, JobHandle.CombineDependencies(jobHandle, dependencies3));
		m_TelecomCoverageSystem.AddReader(jobHandle);
		m_LocalEffectSystem.AddLocalEffectReader(jobHandle2);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
		m_SearchSystem.AddStaticSearchTreeReader(jobHandle2);
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
	public FireSimulationSystem()
	{
	}
}
