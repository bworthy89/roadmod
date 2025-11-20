using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
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
public class WorkCarAISystem : GameSystemBase
{
	private struct WorkAction
	{
		public VehicleWorkType m_WorkType;

		public Entity m_Target;

		public Entity m_Owner;

		public float m_WorkAmount;
	}

	[BurstCompile]
	private struct WorkCarTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		public ComponentTypeHandle<Car> m_CarType;

		public ComponentTypeHandle<CarCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Target> m_TargetType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<Plant> m_PlantData;

		[ReadOnly]
		public ComponentLookup<Damaged> m_DamagedData;

		[ReadOnly]
		public ComponentLookup<Area> m_AreaData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> m_PrefabWorkVehicleData;

		[ReadOnly]
		public ComponentLookup<TreeData> m_PrefabTreeData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Vehicles.WorkVehicle> m_WorkVehicleData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<WorkAction>.ParallelWriter m_WorkQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PathInformation> nativeArray3 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<CarCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Car> nativeArray6 = chunk.GetNativeArray(ref m_CarType);
			NativeArray<Target> nativeArray7 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray8 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
			BufferAccessor<PathElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PathElementType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				PathInformation pathInformation = nativeArray3[i];
				PrefabRef prefabRef = nativeArray4[i];
				Car car = nativeArray6[i];
				CarCurrentLane currentLane = nativeArray5[i];
				PathOwner pathOwner = nativeArray8[i];
				Target target = nativeArray7[i];
				DynamicBuffer<PathElement> path = bufferAccessor2[i];
				DynamicBuffer<LayoutElement> layout = default(DynamicBuffer<LayoutElement>);
				if (bufferAccessor.Length != 0)
				{
					layout = bufferAccessor[i];
				}
				Game.Vehicles.WorkVehicle workVehicle = m_WorkVehicleData[entity];
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, owner, pathInformation, prefabRef, layout, path, ref workVehicle, ref car, ref currentLane, ref pathOwner, ref target);
				m_WorkVehicleData[entity] = workVehicle;
				nativeArray6[i] = car;
				nativeArray5[i] = currentLane;
				nativeArray8[i] = pathOwner;
				nativeArray7[i] = target;
			}
		}

		private void Tick(int jobIndex, Entity vehicleEntity, Owner owner, PathInformation pathInformation, PrefabRef prefabRef, DynamicBuffer<LayoutElement> layout, DynamicBuffer<PathElement> path, ref Game.Vehicles.WorkVehicle workVehicle, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner) && !ResetPath(jobIndex, vehicleEntity, pathInformation, path, layout, ref workVehicle, ref car, ref currentLane, ref target))
			{
				ReturnToDepot(jobIndex, vehicleEntity, owner, ref workVehicle, ref car, ref pathOwner, ref target);
				FindPathIfNeeded(vehicleEntity, owner, prefabRef, layout, ref workVehicle, ref car, ref currentLane, ref pathOwner, ref target);
				return;
			}
			if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if (VehicleUtils.IsStuck(pathOwner) || (workVehicle.m_State & WorkVehicleFlags.Returning) != 0)
				{
					VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicleEntity, layout);
					return;
				}
				ReturnToDepot(jobIndex, vehicleEntity, owner, ref workVehicle, ref car, ref pathOwner, ref target);
			}
			else if (VehicleUtils.PathEndReached(currentLane))
			{
				if ((workVehicle.m_State & WorkVehicleFlags.Returning) != 0)
				{
					VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicleEntity, layout);
					return;
				}
				if (PerformWork(jobIndex, vehicleEntity, owner, prefabRef, layout, ref workVehicle, ref target, ref pathOwner))
				{
					ReturnToDepot(jobIndex, vehicleEntity, owner, ref workVehicle, ref car, ref pathOwner, ref target);
				}
			}
			car.m_Flags |= CarFlags.Warning | CarFlags.Working;
			FindPathIfNeeded(vehicleEntity, owner, prefabRef, layout, ref workVehicle, ref car, ref currentLane, ref pathOwner, ref target);
		}

		private void FindPathIfNeeded(Entity vehicleEntity, Owner owner, PrefabRef prefabRef, DynamicBuffer<LayoutElement> layout, ref Game.Vehicles.WorkVehicle workVehicle, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (!VehicleUtils.RequireNewPath(pathOwner))
			{
				return;
			}
			CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = carData.m_MaxSpeed,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = (PathMethod.Road | PathMethod.Offroad),
				m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.Offroad),
				m_RoadTypes = RoadTypes.Car
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.Offroad),
				m_RoadTypes = RoadTypes.Car,
				m_Entity = target.m_Target
			};
			WorkVehicleData workVehicleData;
			if (layout.IsCreated && layout.Length != 0)
			{
				workVehicleData = default(WorkVehicleData);
				for (int i = 0; i < layout.Length; i++)
				{
					Entity vehicle = layout[i].m_Vehicle;
					PrefabRef prefabRef2 = m_PrefabRefData[vehicle];
					WorkVehicleData workVehicleData2 = m_PrefabWorkVehicleData[prefabRef2.m_Prefab];
					if (workVehicleData2.m_WorkType != VehicleWorkType.None)
					{
						workVehicleData.m_WorkType = workVehicleData2.m_WorkType;
					}
					if (workVehicleData2.m_MapFeature != MapFeature.None)
					{
						workVehicleData.m_MapFeature = workVehicleData2.m_MapFeature;
					}
					workVehicleData.m_Resources |= workVehicleData2.m_Resources;
				}
			}
			else
			{
				workVehicleData = m_PrefabWorkVehicleData[prefabRef.m_Prefab];
			}
			if (workVehicleData.m_WorkType == VehicleWorkType.Move)
			{
				parameters.m_Methods &= ~PathMethod.Road;
				origin.m_Methods |= PathMethod.CargoLoading;
				destination.m_Methods |= PathMethod.CargoLoading;
			}
			if ((workVehicle.m_State & (WorkVehicleFlags.Returning | WorkVehicleFlags.ExtractorVehicle)) == WorkVehicleFlags.ExtractorVehicle)
			{
				if (workVehicleData.m_MapFeature == MapFeature.Forest)
				{
					destination.m_Type = SetupTargetType.WoodResource;
				}
				else
				{
					destination.m_Type = SetupTargetType.AreaLocation;
				}
				destination.m_Entity = owner.m_Owner;
				destination.m_Value = (int)workVehicleData.m_WorkType;
				target.m_Target = owner.m_Owner;
			}
			else if ((workVehicle.m_State & (WorkVehicleFlags.Returning | WorkVehicleFlags.StorageVehicle)) == WorkVehicleFlags.StorageVehicle || (workVehicle.m_State & (WorkVehicleFlags.Returning | WorkVehicleFlags.CargoMoveVehicle)) == WorkVehicleFlags.CargoMoveVehicle)
			{
				destination.m_Type = SetupTargetType.AreaLocation;
				destination.m_Entity = owner.m_Owner;
				destination.m_Value = (int)workVehicleData.m_WorkType;
				target.m_Target = owner.m_Owner;
			}
			VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
		}

		private bool ResetPath(int jobIndex, Entity vehicleEntity, PathInformation pathInformation, DynamicBuffer<PathElement> path, DynamicBuffer<LayoutElement> layout, ref Game.Vehicles.WorkVehicle workVehicle, ref Car car, ref CarCurrentLane currentLane, ref Target target)
		{
			PathUtils.ResetPath(ref currentLane, path, m_SlaveLaneData, m_OwnerData, m_SubLanes);
			if (layout.IsCreated && layout.Length >= 2)
			{
				car.m_Flags |= CarFlags.CannotReverse;
			}
			else
			{
				car.m_Flags &= ~CarFlags.CannotReverse;
			}
			if ((workVehicle.m_State & (WorkVehicleFlags.Returning | WorkVehicleFlags.CargoMoveVehicle)) == WorkVehicleFlags.Returning)
			{
				car.m_Flags &= ~CarFlags.StayOnRoad;
			}
			else
			{
				car.m_Flags |= CarFlags.StayOnRoad;
			}
			if ((workVehicle.m_State & WorkVehicleFlags.Returning) == 0)
			{
				target.m_Target = pathInformation.m_Destination;
			}
			return true;
		}

		private void ReturnToDepot(int jobIndex, Entity vehicleEntity, Owner ownerData, ref Game.Vehicles.WorkVehicle workVehicle, ref Car car, ref PathOwner pathOwner, ref Target target)
		{
			workVehicle.m_State |= WorkVehicleFlags.Returning;
			Entity newTarget = ownerData.m_Owner;
			if (m_AreaData.HasComponent(ownerData.m_Owner) && m_OwnerData.TryGetComponent(ownerData.m_Owner, out var componentData))
			{
				newTarget = ((!m_AttachmentData.TryGetComponent(componentData.m_Owner, out var componentData2)) ? componentData.m_Owner : componentData2.m_Attached);
			}
			VehicleUtils.SetTarget(ref pathOwner, ref target, newTarget);
		}

		private bool PerformWork(int jobIndex, Entity vehicleEntity, Owner owner, PrefabRef prefabRef, DynamicBuffer<LayoutElement> layout, ref Game.Vehicles.WorkVehicle workVehicle, ref Target target, ref PathOwner pathOwner)
		{
			WorkVehicleData workVehicleData;
			if (layout.IsCreated && layout.Length != 0)
			{
				workVehicleData = default(WorkVehicleData);
				for (int i = 0; i < layout.Length; i++)
				{
					Entity vehicle = layout[i].m_Vehicle;
					PrefabRef prefabRef2 = m_PrefabRefData[vehicle];
					WorkVehicleData workVehicleData2 = m_PrefabWorkVehicleData[prefabRef2.m_Prefab];
					if (workVehicleData2.m_WorkType != VehicleWorkType.None)
					{
						workVehicleData.m_WorkType = workVehicleData2.m_WorkType;
						workVehicleData.m_MaxWorkAmount += workVehicleData2.m_MaxWorkAmount;
					}
					if (workVehicleData2.m_MapFeature != MapFeature.None)
					{
						workVehicleData.m_MapFeature = workVehicleData2.m_MapFeature;
					}
					workVehicleData.m_Resources |= workVehicleData2.m_Resources;
				}
			}
			else
			{
				workVehicleData = m_PrefabWorkVehicleData[prefabRef.m_Prefab];
			}
			float num = workVehicleData.m_MaxWorkAmount;
			if ((workVehicle.m_State & WorkVehicleFlags.ExtractorVehicle) != 0)
			{
				switch (workVehicleData.m_WorkType)
				{
				case VehicleWorkType.Harvest:
					num = 1000f;
					if (m_TreeData.HasComponent(target.m_Target))
					{
						Tree tree = m_TreeData[target.m_Target];
						Plant plant = m_PlantData[target.m_Target];
						PrefabRef prefabRef3 = m_PrefabRefData[target.m_Target];
						m_DamagedData.TryGetComponent(target.m_Target, out var componentData);
						if (m_PrefabTreeData.TryGetComponent(prefabRef3.m_Prefab, out var componentData2))
						{
							num = ObjectUtils.CalculateWoodAmount(tree, plant, componentData, componentData2);
						}
						m_CommandBuffer.AddComponent(jobIndex, target.m_Target, default(BatchesUpdated));
					}
					m_WorkQueue.Enqueue(new WorkAction
					{
						m_WorkType = workVehicleData.m_WorkType,
						m_Target = target.m_Target,
						m_Owner = owner.m_Owner,
						m_WorkAmount = num
					});
					break;
				case VehicleWorkType.Collect:
					if (m_TreeData.HasComponent(target.m_Target))
					{
						m_WorkQueue.Enqueue(new WorkAction
						{
							m_WorkType = workVehicleData.m_WorkType,
							m_Target = target.m_Target
						});
					}
					num = workVehicleData.m_MaxWorkAmount * 0.25f;
					break;
				}
			}
			else if ((workVehicle.m_State & (WorkVehicleFlags.StorageVehicle | WorkVehicleFlags.CargoMoveVehicle)) != 0)
			{
				num = workVehicleData.m_MaxWorkAmount * 0.25f;
			}
			VehicleUtils.SetTarget(ref pathOwner, ref target, Entity.Null);
			if (layout.IsCreated && layout.Length != 0)
			{
				float num2 = 0f;
				float num3 = 0f;
				for (int j = 0; j < layout.Length; j++)
				{
					Entity vehicle2 = layout[j].m_Vehicle;
					if (vehicle2 == vehicleEntity)
					{
						float num4 = math.min(num, workVehicle.m_WorkAmount - workVehicle.m_DoneAmount);
						if (num4 > 0f)
						{
							workVehicle.m_DoneAmount += num4;
							num -= num4;
							QuantityUpdated(jobIndex, vehicle2);
						}
						num2 += workVehicle.m_DoneAmount;
						num3 += workVehicle.m_WorkAmount;
						continue;
					}
					Game.Vehicles.WorkVehicle value = m_WorkVehicleData[vehicle2];
					float num5 = math.min(num, value.m_WorkAmount - value.m_DoneAmount);
					if (num5 > 0f)
					{
						value.m_DoneAmount += num5;
						num -= num5;
						m_WorkVehicleData[vehicle2] = value;
						QuantityUpdated(jobIndex, vehicle2);
					}
					num2 += value.m_DoneAmount;
					num3 += value.m_WorkAmount;
				}
				if (num < 1f)
				{
					return num2 > num3 - 1f;
				}
				for (int k = 0; k < layout.Length; k++)
				{
					Entity vehicle3 = layout[k].m_Vehicle;
					if (vehicle3 == vehicleEntity)
					{
						if (workVehicle.m_WorkAmount >= 1f)
						{
							workVehicle.m_DoneAmount += num * workVehicle.m_WorkAmount / num3;
						}
						continue;
					}
					Game.Vehicles.WorkVehicle value2 = m_WorkVehicleData[vehicle3];
					if (value2.m_WorkAmount >= 1f)
					{
						value2.m_DoneAmount += num * value2.m_WorkAmount / num3;
						m_WorkVehicleData[vehicle3] = value2;
					}
				}
				return true;
			}
			QuantityUpdated(jobIndex, vehicleEntity);
			workVehicle.m_DoneAmount += num;
			return workVehicle.m_DoneAmount > workVehicle.m_WorkAmount - 1f;
		}

		private void QuantityUpdated(int jobIndex, Entity vehicleEntity)
		{
			if (m_SubObjects.HasBuffer(vehicleEntity))
			{
				DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = m_SubObjects[vehicleEntity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subObject = dynamicBuffer[i].m_SubObject;
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(BatchesUpdated));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct WorkCarWorkJob : IJob
	{
		public ComponentLookup<Tree> m_TreeData;

		public ComponentLookup<Extractor> m_ExtractorData;

		public NativeQueue<WorkAction> m_WorkQueue;

		public void Execute()
		{
			int count = m_WorkQueue.Count;
			for (int i = 0; i < count; i++)
			{
				WorkAction workAction = m_WorkQueue.Dequeue();
				switch (workAction.m_WorkType)
				{
				case VehicleWorkType.Harvest:
				{
					float num = 0f;
					if (m_TreeData.HasComponent(workAction.m_Target))
					{
						Tree value2 = m_TreeData[workAction.m_Target];
						if ((value2.m_State & TreeState.Stump) == 0)
						{
							value2.m_State &= ~(TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Collected);
							value2.m_State |= TreeState.Stump;
							value2.m_Growth = 0;
							m_TreeData[workAction.m_Target] = value2;
							num = workAction.m_WorkAmount;
						}
					}
					if (m_ExtractorData.HasComponent(workAction.m_Owner))
					{
						Extractor value3 = m_ExtractorData[workAction.m_Owner];
						value3.m_ExtractedAmount -= num;
						value3.m_HarvestedAmount += workAction.m_WorkAmount;
						m_ExtractorData[workAction.m_Owner] = value3;
					}
					break;
				}
				case VehicleWorkType.Collect:
					if (m_TreeData.HasComponent(workAction.m_Target))
					{
						Tree value = m_TreeData[workAction.m_Target];
						if ((value.m_State & TreeState.Collected) == 0)
						{
							value.m_State |= TreeState.Collected;
							m_TreeData[workAction.m_Target] = value;
						}
					}
					break;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<Car> __Game_Vehicles_Car_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Plant> __Game_Objects_Plant_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Area> __Game_Areas_Area_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> __Game_Prefabs_WorkVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		public ComponentLookup<Game.Vehicles.WorkVehicle> __Game_Vehicles_WorkVehicle_RW_ComponentLookup;

		public ComponentLookup<Tree> __Game_Objects_Tree_RW_ComponentLookup;

		public ComponentLookup<Extractor> __Game_Areas_Extractor_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_Car_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Car>();
			__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Pathfind_PathElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_Plant_RO_ComponentLookup = state.GetComponentLookup<Plant>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Areas_Area_RO_ComponentLookup = state.GetComponentLookup<Area>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_WorkVehicleData_RO_ComponentLookup = state.GetComponentLookup<WorkVehicleData>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Vehicles_WorkVehicle_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.WorkVehicle>();
			__Game_Objects_Tree_RW_ComponentLookup = state.GetComponentLookup<Tree>();
			__Game_Areas_Extractor_RW_ComponentLookup = state.GetComponentLookup<Extractor>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EntityQuery m_VehicleQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 12;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Vehicles.WorkVehicle>(), ComponentType.ReadWrite<CarCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<WorkAction> workQueue = new NativeQueue<WorkAction>(Allocator.TempJob);
		WorkCarTickJob jobData = new WorkCarTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Car_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlantData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Area_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWorkVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_WorkVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WorkVehicle_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_WorkQueue = workQueue.AsParallelWriter()
		};
		WorkCarWorkJob jobData2 = new WorkCarWorkJob
		{
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ExtractorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Extractor_RW_ComponentLookup, ref base.CheckedStateRef),
			m_WorkQueue = workQueue
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		workQueue.Dispose(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public WorkCarAISystem()
	{
	}
}
