using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public struct HealthcarePathfindSetup
{
	[BurstCompile]
	private struct SetupAmbulancesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Hospital> m_HospitalType;

		[ReadOnly]
		public ComponentTypeHandle<Ambulance> m_AmbulanceType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		[ReadOnly]
		public ComponentLookup<Game.City.City> m_CityData;

		[ReadOnly]
		public Entity m_City;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has<Game.Objects.OutsideConnection>() && !CityUtils.CheckOption(m_CityData[m_City], CityOption.ImportOutsideServices))
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Hospital> nativeArray2 = chunk.GetNativeArray(ref m_HospitalType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					Hospital hospital = nativeArray2[i];
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out var entity2, out var targetSeeker);
						RoadTypes roadTypes = RoadTypes.None;
						if (AreaUtils.CheckServiceDistrict(entity2, entity, m_ServiceDistricts))
						{
							if ((hospital.m_Flags & HospitalFlags.HasAvailableAmbulances) != 0)
							{
								roadTypes |= RoadTypes.Car;
							}
							if ((hospital.m_Flags & HospitalFlags.HasAvailableMedicalHelicopters) != 0)
							{
								roadTypes |= RoadTypes.Helicopter;
							}
						}
						roadTypes &= targetSeeker.m_SetupQueueTarget.m_RoadTypes | targetSeeker.m_SetupQueueTarget.m_FlyingTypes;
						if (roadTypes != RoadTypes.None)
						{
							float cost = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
							RoadTypes roadTypes2 = targetSeeker.m_SetupQueueTarget.m_RoadTypes;
							RoadTypes flyingTypes = targetSeeker.m_SetupQueueTarget.m_FlyingTypes;
							targetSeeker.m_SetupQueueTarget.m_RoadTypes &= roadTypes;
							targetSeeker.m_SetupQueueTarget.m_FlyingTypes &= roadTypes;
							targetSeeker.FindTargets(entity, cost);
							targetSeeker.m_SetupQueueTarget.m_RoadTypes = roadTypes2;
							targetSeeker.m_SetupQueueTarget.m_FlyingTypes = flyingTypes;
						}
					}
				}
				return;
			}
			NativeArray<Ambulance> nativeArray3 = chunk.GetNativeArray(ref m_AmbulanceType);
			if (nativeArray3.Length == 0)
			{
				return;
			}
			NativeArray<PathOwner> nativeArray4 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Ambulance ambulance = nativeArray3[k];
				if (nativeArray4.Length != 0)
				{
					if ((ambulance.m_State & (AmbulanceFlags.Returning | AmbulanceFlags.Dispatched | AmbulanceFlags.Transporting | AmbulanceFlags.Disabled)) != AmbulanceFlags.Returning)
					{
						continue;
					}
				}
				else if ((ambulance.m_State & AmbulanceFlags.Disabled) != 0)
				{
					continue;
				}
				Entity entity3 = nativeArray[k];
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out var entity4, out var targetSeeker2);
					if (nativeArray5.Length == 0 || AreaUtils.CheckServiceDistrict(entity4, nativeArray5[k].m_Owner, m_ServiceDistricts))
					{
						targetSeeker2.FindTargets(entity3, 0f);
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
	private struct SetupHospitalsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Hospital> m_HospitalType;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenData;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemData;

		[ReadOnly]
		public ComponentLookup<Ambulance> m_AmbulanceData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Hospital> nativeArray2 = chunk.GetNativeArray(ref m_HospitalType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var entity, out var owner, out var targetSeeker);
				int num = 0;
				HealthProblemFlags healthProblemFlags = HealthProblemFlags.None;
				Entity entity2 = owner;
				Entity entity3 = Entity.Null;
				if (m_AmbulanceData.HasComponent(owner))
				{
					entity2 = m_AmbulanceData[owner].m_TargetPatient;
					if (targetSeeker.m_Owner.HasComponent(owner))
					{
						entity3 = targetSeeker.m_Owner[owner].m_Owner;
					}
				}
				if (m_CitizenData.HasComponent(entity2))
				{
					num = m_CitizenData[entity2].m_Health;
				}
				if (m_HealthProblemData.HasComponent(entity2))
				{
					healthProblemFlags = m_HealthProblemData[entity2].m_Flags;
				}
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity entity4 = nativeArray[j];
					Hospital hospital = nativeArray2[j];
					if (((healthProblemFlags & HealthProblemFlags.Sick) != HealthProblemFlags.None && (hospital.m_Flags & HospitalFlags.CanCureDisease) == 0) || ((healthProblemFlags & HealthProblemFlags.Injured) != HealthProblemFlags.None && (hospital.m_Flags & HospitalFlags.CanCureInjury) == 0) || num < hospital.m_MinHealth || num > hospital.m_MaxHealth)
					{
						continue;
					}
					PathMethod pathMethod = targetSeeker.m_SetupQueueTarget.m_Methods;
					RoadTypes roadTypes = targetSeeker.m_SetupQueueTarget.m_RoadTypes;
					if (!AreaUtils.CheckServiceDistrict(entity, entity4, m_ServiceDistricts))
					{
						pathMethod &= ~PathMethod.Pedestrian;
					}
					if ((pathMethod & PathMethod.Pedestrian) != 0 || roadTypes != RoadTypes.None)
					{
						float num2 = (255f - (float)(int)hospital.m_TreatmentBonus) * 200f / (20f + (float)num);
						if (entity4 != entity3)
						{
							num2 += 10f;
						}
						if ((hospital.m_Flags & HospitalFlags.HasRoomForPatients) == 0)
						{
							num2 += 120f;
						}
						PathMethod methods = targetSeeker.m_SetupQueueTarget.m_Methods;
						RoadTypes roadTypes2 = targetSeeker.m_SetupQueueTarget.m_RoadTypes;
						targetSeeker.m_SetupQueueTarget.m_Methods = pathMethod;
						targetSeeker.m_SetupQueueTarget.m_RoadTypes = roadTypes;
						targetSeeker.FindTargets(entity4, num2);
						targetSeeker.m_SetupQueueTarget.m_Methods = methods;
						targetSeeker.m_SetupQueueTarget.m_RoadTypes = roadTypes2;
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
	private struct SetupHearsesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<DeathcareFacility> m_DeathcareFacilityType;

		[ReadOnly]
		public ComponentLookup<Game.City.City> m_CityData;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public ComponentTypeHandle<Hearse> m_HearseType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has<Game.Objects.OutsideConnection>() && !CityUtils.CheckOption(m_CityData[m_City], CityOption.ImportOutsideServices))
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<DeathcareFacility> nativeArray2 = chunk.GetNativeArray(ref m_DeathcareFacilityType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					if ((nativeArray2[i].m_Flags & (DeathcareFacilityFlags.HasAvailableHearses | DeathcareFacilityFlags.HasRoomForBodies)) != (DeathcareFacilityFlags.HasAvailableHearses | DeathcareFacilityFlags.HasRoomForBodies))
					{
						continue;
					}
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out var entity2, out var targetSeeker);
						if (AreaUtils.CheckServiceDistrict(entity2, entity, m_ServiceDistricts))
						{
							float cost = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
							targetSeeker.FindTargets(entity, cost);
						}
					}
				}
				return;
			}
			NativeArray<Hearse> nativeArray3 = chunk.GetNativeArray(ref m_HearseType);
			if (nativeArray3.Length == 0)
			{
				return;
			}
			NativeArray<PathOwner> nativeArray4 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Hearse hearse = nativeArray3[k];
				if (nativeArray4.Length != 0)
				{
					if ((hearse.m_State & (HearseFlags.Returning | HearseFlags.Dispatched | HearseFlags.Transporting | HearseFlags.Disabled)) != HearseFlags.Returning)
					{
						continue;
					}
				}
				else if ((hearse.m_State & HearseFlags.Disabled) != 0)
				{
					continue;
				}
				Entity entity3 = nativeArray[k];
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out var entity4, out var targetSeeker2);
					if (nativeArray5.Length == 0 || AreaUtils.CheckServiceDistrict(entity4, nativeArray5[k].m_Owner, m_ServiceDistricts))
					{
						targetSeeker2.FindTargets(entity3, 0f);
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
	private struct HealthcareRequestsJob : IJobChunk
	{
		private struct DistrictIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public float2 m_Position;

			public ComponentLookup<District> m_DistrictData;

			public BufferLookup<Game.Areas.Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public Entity m_Result;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Position);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Position) && m_DistrictData.HasComponent(areaItem.m_Area))
				{
					DynamicBuffer<Game.Areas.Node> nodes = m_Nodes[areaItem.m_Area];
					DynamicBuffer<Triangle> dynamicBuffer = m_Triangles[areaItem.m_Area];
					if (dynamicBuffer.Length > areaItem.m_Triangle && MathUtils.Intersect(AreaUtils.GetTriangle2(nodes, dynamicBuffer[areaItem.m_Triangle]), m_Position, out var _))
					{
						m_Result = areaItem.m_Area;
					}
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentTypeHandle<HealthcareRequest> m_HealthcareRequestType;

		[ReadOnly]
		public ComponentLookup<HealthcareRequest> m_HealthcareRequestData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceRequest> nativeArray2 = chunk.GetNativeArray(ref m_ServiceRequestType);
			NativeArray<HealthcareRequest> nativeArray3 = chunk.GetNativeArray(ref m_HealthcareRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				if (!m_HealthcareRequestData.TryGetComponent(owner, out var componentData))
				{
					continue;
				}
				Entity service = Entity.Null;
				if (m_VehicleData.HasComponent(componentData.m_Citizen))
				{
					if (targetSeeker.m_Owner.TryGetComponent(componentData.m_Citizen, out var componentData2))
					{
						service = componentData2.m_Owner;
					}
				}
				else
				{
					if (!targetSeeker.m_PrefabRef.HasComponent(componentData.m_Citizen))
					{
						continue;
					}
					service = componentData.m_Citizen;
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						continue;
					}
					HealthcareRequest healthcareRequest = nativeArray3[j];
					if (healthcareRequest.m_Type != componentData.m_Type)
					{
						continue;
					}
					Entity district = Entity.Null;
					CurrentTransport componentData4;
					Transform componentData5;
					if (targetSeeker.m_CurrentBuilding.TryGetComponent(healthcareRequest.m_Citizen, out var componentData3))
					{
						if (m_CurrentDistrictData.HasComponent(componentData3.m_CurrentBuilding))
						{
							district = m_CurrentDistrictData[componentData3.m_CurrentBuilding].m_District;
						}
					}
					else if (targetSeeker.m_CurrentTransport.TryGetComponent(healthcareRequest.m_Citizen, out componentData4) && targetSeeker.m_Transform.TryGetComponent(componentData4.m_CurrentTransport, out componentData5))
					{
						DistrictIterator iterator = new DistrictIterator
						{
							m_Position = componentData5.m_Position.xz,
							m_DistrictData = m_DistrictData,
							m_Nodes = targetSeeker.m_AreaNode,
							m_Triangles = targetSeeker.m_AreaTriangle
						};
						m_AreaTree.Iterate(ref iterator);
						district = iterator.m_Result;
					}
					if (!AreaUtils.CheckServiceDistrict(district, service, m_ServiceDistricts))
					{
						continue;
					}
					targetSeeker.FindTargets(nativeArray[j], healthcareRequest.m_Citizen, 0f, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
					Entity entity2 = healthcareRequest.m_Citizen;
					if (targetSeeker.m_CurrentTransport.HasComponent(entity2))
					{
						entity2 = targetSeeker.m_CurrentTransport[entity2].m_CurrentTransport;
					}
					else if (targetSeeker.m_CurrentBuilding.HasComponent(entity2))
					{
						entity2 = targetSeeker.m_CurrentBuilding[entity2].m_CurrentBuilding;
					}
					if (targetSeeker.m_Transform.TryGetComponent(entity2, out var componentData6) && (targetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Flying) != 0 && (targetSeeker.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Helicopter) != RoadTypes.None)
					{
						Entity lane = Entity.Null;
						float curvePos = 0f;
						float distance = float.MaxValue;
						targetSeeker.m_AirwayData.helicopterMap.FindClosestLane(componentData6.m_Position, targetSeeker.m_Curve, ref lane, ref curvePos, ref distance);
						if (lane != Entity.Null)
						{
							targetSeeker.m_Buffer.Enqueue(new PathTarget(nativeArray[j], lane, curvePos, 0f));
						}
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private EntityQuery m_AmbulanceQuery;

	private EntityQuery m_HospitalQuery;

	private EntityQuery m_HearseQuery;

	private EntityQuery m_HealthcareRequestQuery;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<Owner> m_OwnerType;

	private ComponentTypeHandle<PathOwner> m_PathOwnerType;

	private ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

	private ComponentTypeHandle<HealthcareRequest> m_HealthcareRequestType;

	private ComponentTypeHandle<Hospital> m_HospitalType;

	private ComponentTypeHandle<DeathcareFacility> m_DeathcareFacilityType;

	private ComponentTypeHandle<Hearse> m_HearseType;

	private ComponentTypeHandle<Ambulance> m_AmbulanceType;

	private ComponentLookup<HealthcareRequest> m_HealthcareRequestData;

	private ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

	private ComponentLookup<District> m_DistrictData;

	private ComponentLookup<Citizen> m_CitizenData;

	private ComponentLookup<HealthProblem> m_HealthProblemData;

	private ComponentLookup<Vehicle> m_VehicleData;

	private ComponentLookup<Ambulance> m_AmbulanceData;

	private BufferLookup<ServiceDistrict> m_ServiceDistricts;

	private ComponentLookup<Game.City.City> m_CityData;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private CitySystem m_CitySystem;

	public HealthcarePathfindSetup(PathfindSetupSystem system)
	{
		m_AmbulanceQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hospital>(),
				ComponentType.ReadOnly<Ambulance>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_HospitalQuery = system.GetSetupQuery(ComponentType.ReadOnly<Hospital>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>());
		m_HearseQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<DeathcareFacility>(),
				ComponentType.ReadOnly<Hearse>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_HealthcareRequestQuery = system.GetSetupQuery(ComponentType.ReadOnly<HealthcareRequest>(), ComponentType.Exclude<Dispatched>(), ComponentType.Exclude<PathInformation>());
		m_EntityType = system.GetEntityTypeHandle();
		m_OwnerType = system.GetComponentTypeHandle<Owner>(isReadOnly: true);
		m_PathOwnerType = system.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
		m_ServiceRequestType = system.GetComponentTypeHandle<ServiceRequest>(isReadOnly: true);
		m_HealthcareRequestType = system.GetComponentTypeHandle<HealthcareRequest>(isReadOnly: true);
		m_HospitalType = system.GetComponentTypeHandle<Hospital>(isReadOnly: true);
		m_DeathcareFacilityType = system.GetComponentTypeHandle<DeathcareFacility>(isReadOnly: true);
		m_HearseType = system.GetComponentTypeHandle<Hearse>(isReadOnly: true);
		m_AmbulanceType = system.GetComponentTypeHandle<Ambulance>(isReadOnly: true);
		m_HealthcareRequestData = system.GetComponentLookup<HealthcareRequest>(isReadOnly: true);
		m_CurrentDistrictData = system.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
		m_DistrictData = system.GetComponentLookup<District>(isReadOnly: true);
		m_CitizenData = system.GetComponentLookup<Citizen>(isReadOnly: true);
		m_HealthProblemData = system.GetComponentLookup<HealthProblem>(isReadOnly: true);
		m_VehicleData = system.GetComponentLookup<Vehicle>(isReadOnly: true);
		m_AmbulanceData = system.GetComponentLookup<Ambulance>(isReadOnly: true);
		m_ServiceDistricts = system.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
		m_CityData = system.GetComponentLookup<Game.City.City>(isReadOnly: true);
		m_AreaSearchSystem = system.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_CitySystem = system.World.GetOrCreateSystemManaged<CitySystem>();
	}

	public JobHandle SetupAmbulances(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_HospitalType.Update(system);
		m_AmbulanceType.Update(system);
		m_OwnerType.Update(system);
		m_PathOwnerType.Update(system);
		m_ServiceDistricts.Update(system);
		m_CityData.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupAmbulancesJob
		{
			m_EntityType = m_EntityType,
			m_HospitalType = m_HospitalType,
			m_AmbulanceType = m_AmbulanceType,
			m_OwnerType = m_OwnerType,
			m_PathOwnerType = m_PathOwnerType,
			m_ServiceDistricts = m_ServiceDistricts,
			m_CityData = m_CityData,
			m_City = m_CitySystem.City,
			m_SetupData = setupData
		}, m_AmbulanceQuery, inputDeps);
	}

	public JobHandle SetupHospitals(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_HospitalType.Update(system);
		m_CitizenData.Update(system);
		m_HealthProblemData.Update(system);
		m_AmbulanceData.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupHospitalsJob
		{
			m_EntityType = m_EntityType,
			m_HospitalType = m_HospitalType,
			m_CitizenData = m_CitizenData,
			m_HealthProblemData = m_HealthProblemData,
			m_AmbulanceData = m_AmbulanceData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_HospitalQuery, inputDeps);
	}

	public JobHandle SetupHearses(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_DeathcareFacilityType.Update(system);
		m_HearseType.Update(system);
		m_OwnerType.Update(system);
		m_PathOwnerType.Update(system);
		m_ServiceDistricts.Update(system);
		m_CityData.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupHearsesJob
		{
			m_EntityType = m_EntityType,
			m_DeathcareFacilityType = m_DeathcareFacilityType,
			m_HearseType = m_HearseType,
			m_CityData = m_CityData,
			m_OwnerType = m_OwnerType,
			m_PathOwnerType = m_PathOwnerType,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData,
			m_City = m_CitySystem.City
		}, m_HearseQuery, inputDeps);
	}

	public JobHandle SetupHealthcareRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_HealthcareRequestType.Update(system);
		m_HealthcareRequestData.Update(system);
		m_CurrentDistrictData.Update(system);
		m_DistrictData.Update(system);
		m_VehicleData.Update(system);
		m_ServiceDistricts.Update(system);
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new HealthcareRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_HealthcareRequestType = m_HealthcareRequestType,
			m_HealthcareRequestData = m_HealthcareRequestData,
			m_CurrentDistrictData = m_CurrentDistrictData,
			m_DistrictData = m_DistrictData,
			m_VehicleData = m_VehicleData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_SetupData = setupData
		}, m_HealthcareRequestQuery, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		return jobHandle;
	}
}
