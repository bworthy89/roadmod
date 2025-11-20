using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Simulation;
using Game.Triggers;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ApplyObjectsSystem : GameSystemBase
{
	[BurstCompile]
	private struct PatchTempReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Attached> m_AttachedType;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> m_VehicleType;

		[ReadOnly]
		public ComponentTypeHandle<Creature> m_CreatureType;

		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		public BufferLookup<OwnedCreature> m_OwnedCreatures;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Attached> nativeArray4 = chunk.GetNativeArray(ref m_AttachedType);
			for (int i = 0; i < nativeArray4.Length; i++)
			{
				Entity subObject = nativeArray[i];
				Attached attached = nativeArray4[i];
				Temp temp = nativeArray2[i];
				if ((temp.m_Flags & (TempFlags.Delete | TempFlags.Cancel)) != 0)
				{
					continue;
				}
				Entity entity = attached.m_Parent;
				bool flag = false;
				if (m_TempData.TryGetComponent(attached.m_Parent, out var componentData))
				{
					if (m_PrefabRefData.HasComponent(componentData.m_Original) && (componentData.m_Flags & (TempFlags.Replace | TempFlags.Combine)) == 0)
					{
						entity = componentData.m_Original;
					}
					else
					{
						flag |= m_PrefabRefData.HasComponent(temp.m_Original);
					}
				}
				if (m_AttachedData.TryGetComponent(temp.m_Original, out var componentData2))
				{
					if (componentData2.m_Parent != entity && m_SubObjects.TryGetBuffer(componentData2.m_Parent, out var bufferData))
					{
						CollectionUtils.RemoveValue(bufferData, new Game.Objects.SubObject(temp.m_Original));
					}
				}
				else
				{
					flag |= attached.m_Parent != entity;
				}
				if (flag && m_SubObjects.TryGetBuffer(attached.m_Parent, out var bufferData2))
				{
					CollectionUtils.RemoveValue(bufferData2, new Game.Objects.SubObject(subObject));
				}
			}
			bool flag2 = chunk.Has(ref m_VehicleType);
			bool flag3 = chunk.Has(ref m_CreatureType);
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				Entity entity2 = nativeArray[j];
				Owner owner = nativeArray3[j];
				Temp temp2 = nativeArray2[j];
				if (!(temp2.m_Original == Entity.Null) || (temp2.m_Flags & TempFlags.Delete) != 0)
				{
					continue;
				}
				Owner value = owner;
				if (m_TempData.HasComponent(owner.m_Owner))
				{
					Temp temp3 = m_TempData[owner.m_Owner];
					if (temp3.m_Original != Entity.Null && (temp3.m_Flags & TempFlags.Replace) == 0)
					{
						value.m_Owner = temp3.m_Original;
					}
				}
				if (!(value.m_Owner != owner.m_Owner))
				{
					continue;
				}
				if (flag2)
				{
					if (m_OwnedVehicles.TryGetBuffer(owner.m_Owner, out var bufferData3))
					{
						CollectionUtils.RemoveValue(bufferData3, new OwnedVehicle(entity2));
					}
					if (m_OwnedVehicles.TryGetBuffer(value.m_Owner, out bufferData3))
					{
						CollectionUtils.TryAddUniqueValue(bufferData3, new OwnedVehicle(entity2));
					}
				}
				else if (flag3)
				{
					if (m_OwnedCreatures.TryGetBuffer(owner.m_Owner, out var bufferData4))
					{
						CollectionUtils.RemoveValue(bufferData4, new OwnedCreature(entity2));
					}
					if (m_OwnedCreatures.TryGetBuffer(value.m_Owner, out bufferData4))
					{
						CollectionUtils.TryAddUniqueValue(bufferData4, new OwnedCreature(entity2));
					}
				}
				else
				{
					if (m_SubObjects.TryGetBuffer(owner.m_Owner, out var bufferData5))
					{
						CollectionUtils.RemoveValue(bufferData5, new Game.Objects.SubObject(entity2));
					}
					if (m_SubObjects.TryGetBuffer(value.m_Owner, out bufferData5))
					{
						CollectionUtils.TryAddUniqueValue(bufferData5, new Game.Objects.SubObject(entity2));
					}
				}
				nativeArray3[j] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct HandleTempEntitiesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Attached> m_AttachedType;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> m_WatercraftCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> m_AircraftCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<AnimalCurrentLane> m_AnimalCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<RescueTarget> m_RescueTargetData;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_AbandonedData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TransportStop> m_TransportStopData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Recent> m_RecentData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Damaged> m_DamagedData;

		[ReadOnly]
		public ComponentLookup<Static> m_StaticData;

		[ReadOnly]
		public ComponentLookup<Stopped> m_StoppedData;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[ReadOnly]
		public ComponentLookup<Swaying> m_SwayingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<MeshColor> m_MeshColors;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public ComponentLookup<Signature> m_Signatures;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

		[ReadOnly]
		public ComponentLookup<PropertyToBeOnMarket> m_PropertiesToBeOnMarket;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public EntityArchetype m_PathTargetEventArchetype;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		[ReadOnly]
		public ComponentTypeSet m_TempAnimationTypes;

		[ReadOnly]
		public NativeParallelHashMap<Entity, int> m_InstanceCounts;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Attached> nativeArray4 = chunk.GetNativeArray(ref m_AttachedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Temp temp = nativeArray2[i];
				if ((temp.m_Flags & TempFlags.Cancel) != 0)
				{
					Cancel(unfilteredChunkIndex, entity, temp);
					continue;
				}
				if ((temp.m_Flags & TempFlags.Delete) != 0)
				{
					if (m_ParkedCarData.HasComponent(entity) && !m_ParkedCarData.HasComponent(temp.m_Original))
					{
						Cancel(unfilteredChunkIndex, entity, temp);
					}
					else
					{
						Delete(unfilteredChunkIndex, entity, temp);
					}
					continue;
				}
				if (m_PrefabRefData.HasComponent(temp.m_Original))
				{
					if (m_ParkedCarData.HasComponent(entity) || m_ParkedTrainData.HasComponent(entity))
					{
						if (!m_ParkedCarData.HasComponent(temp.m_Original) && !m_ParkedTrainData.HasComponent(temp.m_Original))
						{
							Cancel(unfilteredChunkIndex, entity, temp);
							continue;
						}
						FixParkingLocation(unfilteredChunkIndex, temp.m_Original);
					}
					if (nativeArray4.Length != 0)
					{
						Attached data = nativeArray4[i];
						if (m_TempData.HasComponent(data.m_Parent))
						{
							Temp temp2 = m_TempData[data.m_Parent];
							if (m_PrefabRefData.HasComponent(temp2.m_Original) && (temp2.m_Flags & (TempFlags.Replace | TempFlags.Combine)) == 0)
							{
								data.m_Parent = temp2.m_Original;
							}
						}
						CopyToOriginal(unfilteredChunkIndex, temp, data);
					}
					if ((temp.m_Flags & TempFlags.Upgrade) != 0)
					{
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_PrefabRefData, updateValue: true);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_DestroyedData, updateValue: false);
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_DamagedData, updateValue: false);
						UpdateBuffer(unfilteredChunkIndex, entity, temp.m_Original, m_MeshColors, out var _, updateValue: false);
						if (!m_DestroyedData.HasComponent(entity))
						{
							if (m_RescueTargetData.HasComponent(temp.m_Original))
							{
								m_CommandBuffer.RemoveComponent<RescueTarget>(unfilteredChunkIndex, temp.m_Original);
							}
							if (m_AbandonedData.HasComponent(temp.m_Original))
							{
								m_CommandBuffer.RemoveComponent<Abandoned>(unfilteredChunkIndex, temp.m_Original);
							}
							if (m_Signatures.HasComponent(temp.m_Original) && !m_PropertiesOnMarket.HasComponent(temp.m_Original) && !m_PropertiesToBeOnMarket.HasComponent(temp.m_Original))
							{
								m_CommandBuffer.AddComponent<PropertyToBeOnMarket>(unfilteredChunkIndex, temp.m_Original);
							}
						}
					}
					UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_ElevationData, updateValue: true);
					UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_LocalTransformCacheData, updateValue: true);
					UpdateBuffer(unfilteredChunkIndex, entity, temp.m_Original, m_SubNets, out var _, updateValue: false);
					UpdateBuffer(unfilteredChunkIndex, entity, temp.m_Original, m_SubAreas, out var _, updateValue: false);
					if (UpdateBuffer(unfilteredChunkIndex, entity, temp.m_Original, m_SubLanes, out var oldBuffer4, updateValue: false))
					{
						RemoveOldSubItems(unfilteredChunkIndex, temp.m_Original, oldBuffer4);
					}
					if (UpdateBuffer(unfilteredChunkIndex, entity, temp.m_Original, m_SubObjects, out var oldBuffer5, updateValue: false))
					{
						RemoveOldSubItems(unfilteredChunkIndex, temp.m_Original, oldBuffer5);
					}
					Update(unfilteredChunkIndex, entity, temp, nativeArray3[i]);
					continue;
				}
				if (m_ParkedCarData.HasComponent(entity) || m_ParkedTrainData.HasComponent(entity))
				{
					FixParkingLocation(unfilteredChunkIndex, entity);
				}
				if (nativeArray4.Length != 0)
				{
					Attached component = nativeArray4[i];
					if (m_TempData.HasComponent(component.m_Parent))
					{
						Temp temp3 = m_TempData[component.m_Parent];
						if (m_PrefabRefData.HasComponent(temp3.m_Original) && (temp3.m_Flags & (TempFlags.Replace | TempFlags.Combine)) == 0)
						{
							component.m_Parent = temp3.m_Original;
						}
					}
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, component);
				}
				Create(unfilteredChunkIndex, entity, temp);
				if (m_PrefabRefData.HasComponent(entity))
				{
					Entity prefab = m_PrefabRefData[entity].m_Prefab;
					int num = (m_InstanceCounts.ContainsKey(prefab) ? m_InstanceCounts[prefab] : 0);
					m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.ObjectCreated, m_PrefabRefData[entity].m_Prefab, entity, entity, num));
				}
			}
		}

		private void FixParkingLocation(int chunkIndex, Entity entity)
		{
			if (!m_ControllerData.TryGetComponent(entity, out var componentData) || !(componentData.m_Controller != entity))
			{
				m_CommandBuffer.AddComponent(chunkIndex, entity, new FixParkingLocation
				{
					m_ResetLocation = entity
				});
			}
		}

		private void RemoveOldSubItems(int chunkIndex, Entity original, DynamicBuffer<Game.Net.SubLane> items)
		{
			for (int i = 0; i < items.Length; i++)
			{
				Entity subLane = items[i].m_SubLane;
				m_CommandBuffer.AddComponent(chunkIndex, subLane, default(Deleted));
			}
		}

		private void RemoveOldSubItems(int chunkIndex, Entity original, DynamicBuffer<Game.Objects.SubObject> items)
		{
			for (int i = 0; i < items.Length; i++)
			{
				Entity subObject = items[i].m_SubObject;
				if (m_OwnerData.TryGetComponent(subObject, out var componentData) && componentData.m_Owner == original)
				{
					m_CommandBuffer.AddComponent(chunkIndex, subObject, default(Deleted));
				}
			}
		}

		private void Cancel(int chunkIndex, Entity entity, Temp temp)
		{
			if (m_HiddenData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(BatchesUpdated));
				m_CommandBuffer.RemoveComponent<Hidden>(chunkIndex, temp.m_Original);
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Delete(int chunkIndex, Entity entity, Temp temp)
		{
			if (m_PrefabRefData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Deleted));
				if (m_SubNets.HasBuffer(temp.m_Original))
				{
					DynamicBuffer<Game.Net.SubNet> dynamicBuffer = m_SubNets[temp.m_Original];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity subNet = dynamicBuffer[i].m_SubNet;
						if (!m_HiddenData.HasComponent(subNet))
						{
							m_CommandBuffer.RemoveComponent<Owner>(chunkIndex, subNet);
							m_CommandBuffer.AddComponent(chunkIndex, subNet, default(Updated));
						}
					}
				}
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void UpdateComponent<T>(int chunkIndex, Entity entity, Entity original, ComponentLookup<T> data, bool updateValue) where T : unmanaged, IComponentData
		{
			if (data.HasComponent(entity))
			{
				if (data.HasComponent(original))
				{
					if (updateValue)
					{
						m_CommandBuffer.SetComponent(chunkIndex, original, data[entity]);
					}
				}
				else if (updateValue)
				{
					m_CommandBuffer.AddComponent(chunkIndex, original, data[entity]);
				}
				else
				{
					m_CommandBuffer.AddComponent(chunkIndex, original, default(T));
				}
			}
			else if (data.HasComponent(original))
			{
				m_CommandBuffer.RemoveComponent<T>(chunkIndex, original);
			}
		}

		private bool UpdateBuffer<T>(int chunkIndex, Entity entity, Entity original, BufferLookup<T> data, out DynamicBuffer<T> oldBuffer, bool updateValue) where T : unmanaged, IBufferElementData
		{
			if (data.HasBuffer(entity))
			{
				if (data.HasBuffer(original))
				{
					if (updateValue)
					{
						m_CommandBuffer.SetBuffer<T>(chunkIndex, original).CopyFrom(data[entity]);
					}
				}
				else if (updateValue)
				{
					m_CommandBuffer.AddBuffer<T>(chunkIndex, original).CopyFrom(data[entity]);
				}
				else
				{
					m_CommandBuffer.AddBuffer<T>(chunkIndex, original);
				}
			}
			else if (data.TryGetBuffer(original, out oldBuffer))
			{
				m_CommandBuffer.RemoveComponent<T>(chunkIndex, original);
				return true;
			}
			oldBuffer = default(DynamicBuffer<T>);
			return false;
		}

		private void Update(int chunkIndex, Entity entity, Temp temp)
		{
			if (m_HiddenData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.RemoveComponent<Hidden>(chunkIndex, temp.m_Original);
			}
			if (temp.m_Cost != 0)
			{
				Recent component = new Recent
				{
					m_ModificationFrame = m_SimulationFrame,
					m_ModificationCost = temp.m_Cost
				};
				if (m_RecentData.TryGetComponent(temp.m_Original, out var componentData))
				{
					component.m_ModificationCost += componentData.m_ModificationCost;
					component.m_ModificationCost += ObjectUtils.GetRefundAmount(componentData, m_SimulationFrame, m_EconomyParameterData);
					componentData.m_ModificationFrame = m_SimulationFrame;
					component.m_ModificationCost -= ObjectUtils.GetRefundAmount(componentData, m_SimulationFrame, m_EconomyParameterData);
					component.m_ModificationCost = math.min(component.m_ModificationCost, temp.m_Value);
					if (component.m_ModificationCost > 0)
					{
						m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, component);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<Recent>(chunkIndex, temp.m_Original);
					}
				}
				else if (component.m_ModificationCost > 0)
				{
					m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, component);
				}
			}
			m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Updated));
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
			if (m_EditorMode && ShouldSaveInstance(entity, temp.m_Original))
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(SaveInstance));
			}
		}

		private bool ShouldSaveInstance(Entity temp, Entity original)
		{
			if (m_OwnerData.HasComponent(original))
			{
				if (!m_ServiceUpgradeData.HasComponent(original))
				{
					return false;
				}
				return true;
			}
			if (m_InstalledUpgrades.TryGetBuffer(original, out var bufferData))
			{
				if (bufferData.Length != 0)
				{
					return false;
				}
				if (m_InstalledUpgrades.TryGetBuffer(temp, out bufferData) && bufferData.Length != 0)
				{
					return false;
				}
			}
			return true;
		}

		private void Update(int chunkIndex, Entity entity, Temp temp, Transform transform)
		{
			Transform transform2 = m_TransformData[temp.m_Original];
			if (!transform2.Equals(transform))
			{
				m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, transform);
				if (m_CarCurrentLaneData.HasComponent(temp.m_Original))
				{
					CarCurrentLane component = m_CarCurrentLaneData[temp.m_Original];
					component.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Obsolete;
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, component);
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, default(Moving));
				}
				else if (m_TrainCurrentLaneData.HasComponent(temp.m_Original))
				{
					TrainCurrentLane component2 = m_TrainCurrentLaneData[temp.m_Original];
					component2.m_Front.m_LaneFlags |= TrainLaneFlags.Obsolete;
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, component2);
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, default(Moving));
				}
				else if (m_WatercraftCurrentLaneData.HasComponent(temp.m_Original))
				{
					WatercraftCurrentLane component3 = m_WatercraftCurrentLaneData[temp.m_Original];
					component3.m_LaneFlags |= WatercraftLaneFlags.Obsolete;
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, component3);
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, default(Moving));
				}
				else if (m_AircraftCurrentLaneData.HasComponent(temp.m_Original))
				{
					AircraftCurrentLane component4 = m_AircraftCurrentLaneData[temp.m_Original];
					component4.m_LaneFlags |= AircraftLaneFlags.Obsolete;
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, component4);
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, default(Moving));
				}
				else if (m_HumanCurrentLaneData.HasComponent(temp.m_Original))
				{
					HumanCurrentLane component5 = m_HumanCurrentLaneData[temp.m_Original];
					component5.m_Flags |= CreatureLaneFlags.Obsolete;
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, component5);
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, default(Moving));
				}
				else if (m_AnimalCurrentLaneData.HasComponent(temp.m_Original))
				{
					AnimalCurrentLane component6 = m_AnimalCurrentLaneData[temp.m_Original];
					component6.m_Flags |= CreatureLaneFlags.Obsolete;
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, component6);
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, default(Moving));
				}
				if (m_BuildingData.HasComponent(temp.m_Original) || m_TransportStopData.HasComponent(temp.m_Original))
				{
					Entity e = m_CommandBuffer.CreateEntity(chunkIndex, m_PathTargetEventArchetype);
					m_CommandBuffer.SetComponent(chunkIndex, e, new PathTargetMoved(temp.m_Original, transform2.m_Position, transform.m_Position));
				}
			}
			Update(chunkIndex, entity, temp);
		}

		private void CopyToOriginal<T>(int chunkIndex, Temp temp, T data) where T : unmanaged, IComponentData
		{
			m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, data);
		}

		private void Create(int chunkIndex, Entity entity, Temp temp)
		{
			m_CommandBuffer.RemoveComponent(chunkIndex, entity, in m_TempAnimationTypes);
			if ((m_StaticData.HasComponent(entity) && !m_SwayingData.HasComponent(entity)) || m_StoppedData.HasComponent(entity))
			{
				m_CommandBuffer.RemoveComponent<InterpolatedTransform>(chunkIndex, entity);
			}
			if (temp.m_Cost > 0)
			{
				Recent component = new Recent
				{
					m_ModificationFrame = m_SimulationFrame,
					m_ModificationCost = temp.m_Cost
				};
				m_CommandBuffer.AddComponent(chunkIndex, entity, component);
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, in m_AppliedTypes);
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
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Attached> __Game_Objects_Attached_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Creature> __Game_Creatures_Creature_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Owner> __Game_Common_Owner_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RW_BufferLookup;

		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RW_BufferLookup;

		public BufferLookup<OwnedCreature> __Game_Creatures_OwnedCreature_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RescueTarget> __Game_Buildings_RescueTarget_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TransportStop> __Game_Routes_TransportStop_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Recent> __Game_Tools_Recent_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Static> __Game_Objects_Static_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Stopped> __Game_Objects_Stopped_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Swaying> __Game_Rendering_Swaying_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshColor> __Game_Rendering_MeshColor_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Signature> __Game_Buildings_Signature_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyToBeOnMarket> __Game_Buildings_PropertyToBeOnMarket_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Attached>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Vehicle>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Creature>(isReadOnly: true);
			__Game_Common_Owner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>();
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Objects_SubObject_RW_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>();
			__Game_Vehicles_OwnedVehicle_RW_BufferLookup = state.GetBufferLookup<OwnedVehicle>();
			__Game_Creatures_OwnedCreature_RW_BufferLookup = state.GetBufferLookup<OwnedCreature>();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentLookup = state.GetComponentLookup<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<AircraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>(isReadOnly: true);
			__Game_Creatures_AnimalCurrentLane_RO_ComponentLookup = state.GetComponentLookup<AnimalCurrentLane>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Buildings_RescueTarget_RO_ComponentLookup = state.GetComponentLookup<RescueTarget>(isReadOnly: true);
			__Game_Routes_TransportStop_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.TransportStop>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Tools_Recent_RO_ComponentLookup = state.GetComponentLookup<Recent>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Objects_Static_RO_ComponentLookup = state.GetComponentLookup<Static>(isReadOnly: true);
			__Game_Objects_Stopped_RO_ComponentLookup = state.GetComponentLookup<Stopped>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Rendering_Swaying_RO_ComponentLookup = state.GetComponentLookup<Swaying>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Rendering_MeshColor_RO_BufferLookup = state.GetBufferLookup<MeshColor>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Buildings_Signature_RO_ComponentLookup = state.GetComponentLookup<Signature>(isReadOnly: true);
			__Game_Buildings_PropertyOnMarket_RO_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>(isReadOnly: true);
			__Game_Buildings_PropertyToBeOnMarket_RO_ComponentLookup = state.GetComponentLookup<PropertyToBeOnMarket>(isReadOnly: true);
		}
	}

	private ToolOutputBarrier m_ToolOutputBarrier;

	private ToolSystem m_ToolSystem;

	private SimulationSystem m_SimulationSystem;

	private TriggerSystem m_TriggerSystem;

	private InstanceCountSystem m_InstanceCountSystem;

	private EntityQuery m_TempQuery;

	private EntityQuery m_EconomyParameterQuery;

	private EntityArchetype m_PathTargetEventArchetype;

	private ComponentTypeSet m_AppliedTypes;

	private ComponentTypeSet m_TempAnimationTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_InstanceCountSystem = base.World.GetOrCreateSystemManaged<InstanceCountSystem>();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Object>());
		m_PathTargetEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<PathTargetMoved>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		m_TempAnimationTypes = new ComponentTypeSet(ComponentType.ReadWrite<Temp>(), ComponentType.ReadWrite<Animation>(), ComponentType.ReadWrite<BackSide>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		RequireForUpdate(m_TempQuery);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PatchTempReferencesJob jobData = new PatchTempReferencesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AttachedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RW_BufferLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RW_BufferLookup, ref base.CheckedStateRef),
			m_OwnedCreatures = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Creatures_OwnedCreature_RW_BufferLookup, ref base.CheckedStateRef)
		};
		NativeQueue<TriggerAction> nativeQueue = (m_TriggerSystem.Enabled ? m_TriggerSystem.CreateActionBuffer() : new NativeQueue<TriggerAction>(Allocator.TempJob));
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new HandleTempEntitiesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AttachedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AircraftCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RescueTargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_RescueTarget_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TransportStop_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RecentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Recent_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AbandonedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StaticData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Static_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StoppedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SwayingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_Swaying_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_Signatures = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Signature_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertiesOnMarket = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertiesToBeOnMarket = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyToBeOnMarket_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_PathTargetEventArchetype = m_PathTargetEventArchetype,
			m_AppliedTypes = m_AppliedTypes,
			m_TempAnimationTypes = m_TempAnimationTypes,
			m_InstanceCounts = m_InstanceCountSystem.GetInstanceCounts(readOnly: true, out dependencies),
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_TriggerBuffer = nativeQueue.AsParallelWriter()
		}, dependsOn: JobHandle.CombineDependencies(JobChunkExtensions.Schedule(jobData, m_TempQuery, base.Dependency), dependencies), query: m_TempQuery);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
		if (m_TriggerSystem.Enabled)
		{
			m_TriggerSystem.AddActionBufferWriter(jobHandle);
		}
		else
		{
			nativeQueue.Dispose(jobHandle);
		}
		m_InstanceCountSystem.AddCountReader(jobHandle);
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
	public ApplyObjectsSystem()
	{
	}
}
