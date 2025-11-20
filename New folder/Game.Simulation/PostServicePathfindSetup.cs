using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public struct PostServicePathfindSetup
{
	[BurstCompile]
	private struct SetupPostVansJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.PostFacility> m_PostFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PostVan> m_PostVanType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PostFacility> m_PostFacilityData;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.PostFacility> nativeArray2 = chunk.GetNativeArray(ref m_PostFacilityType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Game.Buildings.PostFacility postFacility = nativeArray2[i];
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out var entity, out var targetSeeker);
						if (((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Export) != SetupTargetFlags.None && (postFacility.m_Flags & PostFacilityFlags.CanDeliverMailWithVan) != 0) || ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Import) != SetupTargetFlags.None && (postFacility.m_Flags & PostFacilityFlags.CanCollectMailWithVan) != 0))
						{
							Entity entity2 = nativeArray[i];
							if (AreaUtils.CheckServiceDistrict(entity, entity2, m_ServiceDistricts))
							{
								float cost = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
								targetSeeker.FindTargets(entity2, cost);
							}
						}
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.PostVan> nativeArray3 = chunk.GetNativeArray(ref m_PostVanType);
			if (nativeArray3.Length == 0)
			{
				return;
			}
			NativeArray<PathOwner> nativeArray4 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Entity entity3 = nativeArray[k];
				Game.Vehicles.PostVan postVan = nativeArray3[k];
				if ((postVan.m_State & PostVanFlags.Disabled) != 0)
				{
					continue;
				}
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out var entity4, out var targetSeeker2);
					if (((targetSeeker2.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Export) != SetupTargetFlags.None && (postVan.m_State & PostVanFlags.EstimatedEmpty) != 0) || ((targetSeeker2.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Import) != SetupTargetFlags.None && (postVan.m_State & PostVanFlags.EstimatedFull) != 0))
					{
						continue;
					}
					if (nativeArray5.Length != 0)
					{
						if (!AreaUtils.CheckServiceDistrict(entity4, nativeArray5[k].m_Owner, m_ServiceDistricts))
						{
							continue;
						}
						if (m_PostFacilityData.HasComponent(nativeArray5[k].m_Owner))
						{
							bool flag = false;
							Game.Buildings.PostFacility postFacility2 = m_PostFacilityData[nativeArray5[k].m_Owner];
							if ((postFacility2.m_Flags & PostFacilityFlags.CanCollectMailWithVan) != 0 && (targetSeeker2.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Import) != SetupTargetFlags.None)
							{
								flag = true;
							}
							if ((targetSeeker2.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Export) != SetupTargetFlags.None && (postFacility2.m_Flags & PostFacilityFlags.CanDeliverMailWithVan) != 0)
							{
								flag = true;
							}
							if (!flag)
							{
								continue;
							}
						}
					}
					if ((postVan.m_State & PostVanFlags.Returning) != 0 || nativeArray4.Length == 0)
					{
						targetSeeker2.FindTargets(entity3, 0f);
						continue;
					}
					PathOwner pathOwner = nativeArray4[k];
					DynamicBuffer<ServiceDispatch> dynamicBuffer = bufferAccessor2[k];
					int num = math.min(postVan.m_RequestCount, dynamicBuffer.Length);
					PathElement pathElement = default(PathElement);
					float num2 = 0f;
					bool flag2 = false;
					if (num >= 1)
					{
						DynamicBuffer<PathElement> dynamicBuffer2 = bufferAccessor[k];
						if (pathOwner.m_ElementIndex < dynamicBuffer2.Length)
						{
							num2 += (float)(dynamicBuffer2.Length - pathOwner.m_ElementIndex) * postVan.m_PathElementTime * targetSeeker2.m_PathfindParameters.m_Weights.time;
							pathElement = dynamicBuffer2[dynamicBuffer2.Length - 1];
							flag2 = true;
						}
					}
					for (int m = 1; m < num; m++)
					{
						Entity request = dynamicBuffer[m].m_Request;
						if (m_PathInformationData.TryGetComponent(request, out var componentData))
						{
							num2 += componentData.m_Duration * targetSeeker2.m_PathfindParameters.m_Weights.time;
						}
						if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
						{
							pathElement = bufferData[bufferData.Length - 1];
							flag2 = true;
						}
					}
					if (flag2)
					{
						targetSeeker2.m_Buffer.Enqueue(new PathTarget(entity3, pathElement.m_Target, pathElement.m_TargetDelta.y, num2));
					}
					else
					{
						targetSeeker2.FindTargets(entity3, entity3, num2, EdgeFlags.DefaultMask, allowAccessRestriction: true, num >= 1);
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
	private struct SetupMailTransferJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.PostFacility> m_PostFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourcesType;

		[ReadOnly]
		public BufferTypeHandle<TradeCost> m_TradeCostType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_StorageLimitData;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			bool flag = chunk.Has(ref m_OutsideConnectionType);
			Entity entity;
			if (!flag)
			{
				NativeArray<Game.Buildings.PostFacility> nativeArray2 = chunk.GetNativeArray(ref m_PostFacilityType);
				if (nativeArray2.Length != 0)
				{
					for (int i = 0; i < nativeArray2.Length; i++)
					{
						Game.Buildings.PostFacility postFacility = nativeArray2[i];
						for (int j = 0; j < m_SetupData.Length; j++)
						{
							m_SetupData.GetItem(j, out entity, out var targetSeeker);
							Resource resource = targetSeeker.m_SetupQueueTarget.m_Resource;
							if ((resource & (Resource)12288uL) == Resource.NoResource || ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.RequireTransport) != SetupTargetFlags.None && (postFacility.m_Flags & PostFacilityFlags.HasAvailableTrucks) == 0))
							{
								continue;
							}
							if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Import) != SetupTargetFlags.None)
							{
								if (((resource & Resource.UnsortedMail) != Resource.NoResource && (postFacility.m_Flags & PostFacilityFlags.AcceptsUnsortedMail) != 0) || ((resource & Resource.LocalMail) != Resource.NoResource && (postFacility.m_Flags & PostFacilityFlags.AcceptsLocalMail) != 0))
								{
									Entity entity2 = nativeArray[i];
									targetSeeker.FindTargets(entity2, 0f);
								}
							}
							else if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Export) != SetupTargetFlags.None && (((resource & Resource.UnsortedMail) != Resource.NoResource && (postFacility.m_Flags & PostFacilityFlags.DeliversUnsortedMail) != 0) || ((resource & Resource.LocalMail) != Resource.NoResource && (postFacility.m_Flags & PostFacilityFlags.DeliversLocalMail) != 0)))
							{
								Entity entity3 = nativeArray[i];
								targetSeeker.FindTargets(entity3, 0f);
							}
						}
					}
					return;
				}
			}
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourcesType);
			BufferAccessor<TradeCost> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TradeCostType);
			BufferAccessor<InstalledUpgrade> bufferAccessor3 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			for (int k = 0; k < nativeArray.Length; k++)
			{
				Entity entity4 = nativeArray[k];
				Entity prefab = nativeArray3[k].m_Prefab;
				StorageCompanyData storageCompanyData = m_StorageCompanyData[prefab];
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out entity, out var targetSeeker2);
					Resource resource2 = targetSeeker2.m_SetupQueueTarget.m_Resource;
					int value = targetSeeker2.m_SetupQueueTarget.m_Value;
					switch (resource2)
					{
					case Resource.LocalMail:
						if ((targetSeeker2.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Import) != 0 && flag)
						{
							continue;
						}
						break;
					case Resource.UnsortedMail:
					case Resource.OutgoingMail:
						if ((targetSeeker2.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Export) != 0 && flag)
						{
							continue;
						}
						break;
					}
					if ((resource2 & storageCompanyData.m_StoredResources) == Resource.NoResource)
					{
						continue;
					}
					float num = EconomyUtils.GetResources(resource2, bufferAccessor[k]);
					if ((targetSeeker2.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Export) != SetupTargetFlags.None)
					{
						if (num >= (float)value)
						{
							num -= EconomyUtils.GetTradeCost(resource2, bufferAccessor2[k]).m_BuyCost * (float)value;
							if (num >= (float)value)
							{
								targetSeeker2.FindTargets(entity4, math.max(0f, 500f - num));
							}
						}
					}
					else
					{
						if ((targetSeeker2.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Import) == 0)
						{
							continue;
						}
						int num2 = value;
						if (m_StorageLimitData.HasComponent(prefab))
						{
							StorageLimitData data = m_StorageLimitData[prefab];
							if (bufferAccessor3.Length != 0)
							{
								UpgradeUtils.CombineStats(ref data, bufferAccessor3[k], ref targetSeeker2.m_PrefabRef, ref m_StorageLimitData);
							}
							num2 = data.m_Limit - EconomyUtils.GetResources(resource2, bufferAccessor[k]);
						}
						if (num2 >= value)
						{
							targetSeeker2.FindTargets(entity4, math.max(0f, -0.1f * (float)num2 + EconomyUtils.GetTradeCost(resource2, bufferAccessor2[k]).m_SellCost * (float)value));
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

	[BurstCompile]
	private struct SetupMailBoxesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.MailBox> m_MailBoxType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.TransportStop> m_TransportStopType;

		[ReadOnly]
		public ComponentLookup<MailBoxData> m_MailBoxData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Routes.MailBox> nativeArray3 = chunk.GetNativeArray(ref m_MailBoxType);
			NativeArray<Game.Routes.TransportStop> nativeArray4 = chunk.GetNativeArray(ref m_TransportStopType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				Game.Routes.MailBox mailBox = nativeArray3[i];
				if ((m_BuildingData.TryGetComponent(entity, out var componentData) && BuildingUtils.CheckOption(componentData, BuildingOption.Inactive)) || !m_MailBoxData.TryGetComponent(prefab, out var componentData2) || mailBox.m_MailAmount >= componentData2.m_MailCapacity)
				{
					continue;
				}
				for (int j = 0; j < m_SetupData.Length; j++)
				{
					m_SetupData.GetItem(j, out var _, out var targetSeeker);
					float num = (float)mailBox.m_MailAmount * 100f / (float)componentData2.m_MailCapacity;
					if (nativeArray4.Length != 0)
					{
						num += 10f * (1f - nativeArray4[i].m_ComfortFactor) * targetSeeker.m_PathfindParameters.m_Weights.m_Value.w;
					}
					targetSeeker.FindTargets(entity, num);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PostVanRequestsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentTypeHandle<PostVanRequest> m_PostVanRequestType;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> m_PostVanRequestData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PostFacility> m_PostFacilityData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PostVan> m_PostVanData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceRequest> nativeArray2 = chunk.GetNativeArray(ref m_ServiceRequestType);
			NativeArray<PostVanRequest> nativeArray3 = chunk.GetNativeArray(ref m_PostVanRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				if (!m_PostVanRequestData.TryGetComponent(owner, out var componentData))
				{
					continue;
				}
				Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				Entity service = Entity.Null;
				bool flag = false;
				bool flag2 = false;
				if (m_PostVanData.TryGetComponent(componentData.m_Target, out var componentData2))
				{
					flag = (componentData2.m_State & PostVanFlags.EstimatedFull) == 0;
					flag2 = (componentData2.m_State & PostVanFlags.EstimatedEmpty) == 0;
					if (targetSeeker.m_Owner.TryGetComponent(componentData.m_Target, out var componentData3))
					{
						service = componentData3.m_Owner;
					}
				}
				else
				{
					if (!m_PostFacilityData.TryGetComponent(componentData.m_Target, out var componentData4))
					{
						continue;
					}
					flag = (componentData4.m_Flags & PostFacilityFlags.CanCollectMailWithVan) != 0;
					flag2 = (componentData4.m_Flags & PostFacilityFlags.CanDeliverMailWithVan) != 0;
					service = componentData.m_Target;
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						continue;
					}
					PostVanRequest postVanRequest = nativeArray3[j];
					if (((postVanRequest.m_Flags & PostVanRequestFlags.Collect) == 0 || flag) && ((postVanRequest.m_Flags & PostVanRequestFlags.Deliver) == 0 || flag2))
					{
						Entity district = Entity.Null;
						if (m_CurrentDistrictData.TryGetComponent(postVanRequest.m_Target, out var componentData5))
						{
							district = componentData5.m_District;
						}
						if (AreaUtils.CheckServiceDistrict(district, service, m_ServiceDistricts))
						{
							float cost = random.NextFloat(30f);
							targetSeeker.FindTargets(nativeArray[j], postVanRequest.m_Target, cost, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
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

	private EntityQuery m_PostVanQuery;

	private EntityQuery m_MailTransferQuery;

	private EntityQuery m_MailBoxQuery;

	private EntityQuery m_PostVanRequestQuery;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<PathOwner> m_PathOwnerType;

	private ComponentTypeHandle<Owner> m_OwnerType;

	private ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

	private ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

	private ComponentTypeHandle<PostVanRequest> m_PostVanRequestType;

	private ComponentTypeHandle<Game.Buildings.PostFacility> m_PostFacilityType;

	private ComponentTypeHandle<Game.Vehicles.PostVan> m_PostVanType;

	private ComponentTypeHandle<Game.Routes.MailBox> m_MailBoxType;

	private ComponentTypeHandle<Game.Routes.TransportStop> m_TransportStopType;

	private ComponentTypeHandle<PrefabRef> m_PrefabRefType;

	private BufferTypeHandle<PathElement> m_PathElementType;

	private BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

	private BufferTypeHandle<Resources> m_ResourcesType;

	private BufferTypeHandle<TradeCost> m_TradeCostType;

	private BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

	private ComponentLookup<PathInformation> m_PathInformationData;

	private ComponentLookup<PostVanRequest> m_PostVanRequestData;

	private ComponentLookup<Game.Buildings.PostFacility> m_PostFacilityData;

	private ComponentLookup<Game.Vehicles.PostVan> m_PostVanData;

	private ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

	private ComponentLookup<StorageLimitData> m_StorageLimitData;

	private ComponentLookup<StorageCompanyData> m_StorageCompanyData;

	private ComponentLookup<MailBoxData> m_MailBoxData;

	private ComponentLookup<Building> m_BuildingData;

	private BufferLookup<PathElement> m_PathElements;

	private BufferLookup<ServiceDistrict> m_ServiceDistricts;

	public PostServicePathfindSetup(PathfindSetupSystem system)
	{
		m_PostVanQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.PostFacility>(),
				ComponentType.ReadOnly<Game.Vehicles.PostVan>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_MailTransferQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.PostFacility>(),
				ComponentType.ReadOnly<ServiceDispatch>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<Resources>(),
				ComponentType.ReadOnly<TradeCost>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_MailBoxQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PrefabRef>() },
			Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Routes.MailBox>() },
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_PostVanRequestQuery = system.GetSetupQuery(ComponentType.ReadOnly<PostVanRequest>(), ComponentType.Exclude<Dispatched>(), ComponentType.Exclude<PathInformation>());
		m_EntityType = system.GetEntityTypeHandle();
		m_PathOwnerType = system.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
		m_OwnerType = system.GetComponentTypeHandle<Owner>(isReadOnly: true);
		m_OutsideConnectionType = system.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_ServiceRequestType = system.GetComponentTypeHandle<ServiceRequest>(isReadOnly: true);
		m_PostVanRequestType = system.GetComponentTypeHandle<PostVanRequest>(isReadOnly: true);
		m_PostFacilityType = system.GetComponentTypeHandle<Game.Buildings.PostFacility>(isReadOnly: true);
		m_PostVanType = system.GetComponentTypeHandle<Game.Vehicles.PostVan>(isReadOnly: true);
		m_MailBoxType = system.GetComponentTypeHandle<Game.Routes.MailBox>(isReadOnly: true);
		m_TransportStopType = system.GetComponentTypeHandle<Game.Routes.TransportStop>(isReadOnly: true);
		m_PrefabRefType = system.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		m_PathElementType = system.GetBufferTypeHandle<PathElement>(isReadOnly: true);
		m_ServiceDispatchType = system.GetBufferTypeHandle<ServiceDispatch>(isReadOnly: true);
		m_ResourcesType = system.GetBufferTypeHandle<Resources>(isReadOnly: true);
		m_TradeCostType = system.GetBufferTypeHandle<TradeCost>(isReadOnly: true);
		m_InstalledUpgradeType = system.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
		m_PathInformationData = system.GetComponentLookup<PathInformation>(isReadOnly: true);
		m_PostVanRequestData = system.GetComponentLookup<PostVanRequest>(isReadOnly: true);
		m_PostFacilityData = system.GetComponentLookup<Game.Buildings.PostFacility>(isReadOnly: true);
		m_PostVanData = system.GetComponentLookup<Game.Vehicles.PostVan>(isReadOnly: true);
		m_CurrentDistrictData = system.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
		m_StorageLimitData = system.GetComponentLookup<StorageLimitData>(isReadOnly: true);
		m_StorageCompanyData = system.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
		m_MailBoxData = system.GetComponentLookup<MailBoxData>(isReadOnly: true);
		m_PathElements = system.GetBufferLookup<PathElement>(isReadOnly: true);
		m_ServiceDistricts = system.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
		m_BuildingData = system.GetComponentLookup<Building>(isReadOnly: true);
	}

	public JobHandle SetupPostVans(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_PostFacilityType.Update(system);
		m_PostVanType.Update(system);
		m_PathOwnerType.Update(system);
		m_OwnerType.Update(system);
		m_PathElementType.Update(system);
		m_ServiceDispatchType.Update(system);
		m_PathInformationData.Update(system);
		m_PathElements.Update(system);
		m_ServiceDistricts.Update(system);
		m_PostFacilityData.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupPostVansJob
		{
			m_EntityType = m_EntityType,
			m_PostFacilityType = m_PostFacilityType,
			m_PostVanType = m_PostVanType,
			m_PathOwnerType = m_PathOwnerType,
			m_OwnerType = m_OwnerType,
			m_PathElementType = m_PathElementType,
			m_ServiceDispatchType = m_ServiceDispatchType,
			m_PathInformationData = m_PathInformationData,
			m_PathElements = m_PathElements,
			m_ServiceDistricts = m_ServiceDistricts,
			m_PostFacilityData = m_PostFacilityData,
			m_SetupData = setupData
		}, m_PostVanQuery, inputDeps);
	}

	public JobHandle SetupMailTransfer(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_PostFacilityType.Update(system);
		m_PrefabRefType.Update(system);
		m_OutsideConnectionType.Update(system);
		m_ResourcesType.Update(system);
		m_TradeCostType.Update(system);
		m_InstalledUpgradeType.Update(system);
		m_StorageCompanyData.Update(system);
		m_StorageLimitData.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupMailTransferJob
		{
			m_EntityType = m_EntityType,
			m_PostFacilityType = m_PostFacilityType,
			m_PrefabRefType = m_PrefabRefType,
			m_OutsideConnectionType = m_OutsideConnectionType,
			m_ResourcesType = m_ResourcesType,
			m_TradeCostType = m_TradeCostType,
			m_InstalledUpgradeType = m_InstalledUpgradeType,
			m_StorageCompanyData = m_StorageCompanyData,
			m_StorageLimitData = m_StorageLimitData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_MailTransferQuery, inputDeps);
	}

	public JobHandle SetupMailBoxes(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_PrefabRefType.Update(system);
		m_MailBoxType.Update(system);
		m_TransportStopType.Update(system);
		m_MailBoxData.Update(system);
		m_BuildingData.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupMailBoxesJob
		{
			m_EntityType = m_EntityType,
			m_PrefabRefType = m_PrefabRefType,
			m_MailBoxType = m_MailBoxType,
			m_TransportStopType = m_TransportStopType,
			m_MailBoxData = m_MailBoxData,
			m_BuildingData = m_BuildingData,
			m_SetupData = setupData
		}, m_MailBoxQuery, inputDeps);
	}

	public JobHandle SetupPostVanRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_PostVanRequestType.Update(system);
		m_PostVanRequestData.Update(system);
		m_CurrentDistrictData.Update(system);
		m_PostFacilityData.Update(system);
		m_PostVanData.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new PostVanRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_PostVanRequestType = m_PostVanRequestType,
			m_PostVanRequestData = m_PostVanRequestData,
			m_CurrentDistrictData = m_CurrentDistrictData,
			m_PostFacilityData = m_PostFacilityData,
			m_PostVanData = m_PostVanData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_PostVanRequestQuery, inputDeps);
	}
}
