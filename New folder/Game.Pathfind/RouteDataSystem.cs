using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Pathfind;

[CompilerGenerated]
public class RouteDataSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateRouteDataJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Game.Routes.TransportStop> m_TransportStopType;

		public ComponentTypeHandle<Game.Routes.TakeoffLocation> m_TakeoffLocationType;

		public ComponentTypeHandle<Game.Objects.SpawnLocation> m_SpawnLocationType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> m_PrefabRouteConnectionData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Routes.TransportStop> nativeArray = chunk.GetNativeArray(ref m_TransportStopType);
			NativeArray<Game.Routes.TakeoffLocation> nativeArray2 = chunk.GetNativeArray(ref m_TakeoffLocationType);
			NativeArray<Game.Objects.SpawnLocation> nativeArray3 = chunk.GetNativeArray(ref m_SpawnLocationType);
			NativeArray<Owner> nativeArray4 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			if (nativeArray.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Game.Routes.TransportStop value = nativeArray[i];
					value.m_AccessRestriction = Entity.Null;
					value.m_Flags &= ~StopFlags.AllowEnter;
					if (nativeArray4.Length != 0)
					{
						Owner owner = nativeArray4[i];
						PrefabRef prefabRef = nativeArray5[i];
						RouteConnectionData routeConnectionData = m_PrefabRouteConnectionData[prefabRef.m_Prefab];
						Game.Prefabs.BuildingFlags flag = GetRestrictFlag(routeConnectionData.m_AccessConnectionType, routeConnectionData.m_AccessRoadType) | GetRestrictFlag(routeConnectionData.m_RouteConnectionType, routeConnectionData.m_RouteRoadType);
						value.m_AccessRestriction = GetAccessRestriction(owner, flag, isTakeOffLocation: false, out var allowEnter, out var _);
						if (allowEnter)
						{
							value.m_Flags |= StopFlags.AllowEnter;
						}
					}
					nativeArray[i] = value;
				}
			}
			if (nativeArray3.Length != 0)
			{
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					Game.Objects.SpawnLocation value2 = nativeArray3[j];
					value2.m_AccessRestriction = Entity.Null;
					value2.m_Flags &= ~(SpawnLocationFlags.AllowEnter | SpawnLocationFlags.AllowExit);
					if (nativeArray4.Length != 0)
					{
						Owner owner2 = nativeArray4[j];
						PrefabRef prefabRef2 = nativeArray5[j];
						if (m_PrefabSpawnLocationData.TryGetComponent(prefabRef2.m_Prefab, out var componentData))
						{
							Game.Prefabs.BuildingFlags restrictFlag = GetRestrictFlag(componentData.m_ConnectionType, componentData.m_RoadTypes);
							value2.m_AccessRestriction = GetAccessRestriction(owner2, restrictFlag, isTakeOffLocation: false, out var allowEnter2, out var allowExit2);
							if (allowEnter2)
							{
								value2.m_Flags |= SpawnLocationFlags.AllowEnter;
							}
							if (allowExit2)
							{
								value2.m_Flags |= SpawnLocationFlags.AllowExit;
							}
						}
					}
					nativeArray3[j] = value2;
				}
			}
			if (nativeArray2.Length == 0)
			{
				return;
			}
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				Game.Routes.TakeoffLocation value3 = nativeArray2[k];
				value3.m_AccessRestriction = Entity.Null;
				value3.m_Flags &= ~(TakeoffLocationFlags.AllowEnter | TakeoffLocationFlags.AllowExit);
				if (nativeArray3.Length != 0)
				{
					Game.Objects.SpawnLocation spawnLocation = nativeArray3[k];
					value3.m_AccessRestriction = spawnLocation.m_AccessRestriction;
					if ((spawnLocation.m_Flags & SpawnLocationFlags.AllowEnter) != 0)
					{
						value3.m_Flags |= TakeoffLocationFlags.AllowEnter;
					}
					if ((spawnLocation.m_Flags & SpawnLocationFlags.AllowExit) != 0)
					{
						value3.m_Flags |= TakeoffLocationFlags.AllowExit;
					}
				}
				else if (nativeArray4.Length != 0)
				{
					Owner owner3 = nativeArray4[k];
					PrefabRef prefabRef3 = nativeArray5[k];
					RouteConnectionData routeConnectionData2 = m_PrefabRouteConnectionData[prefabRef3.m_Prefab];
					Game.Prefabs.BuildingFlags flag2 = GetRestrictFlag(routeConnectionData2.m_AccessConnectionType, routeConnectionData2.m_AccessRoadType) | GetRestrictFlag(routeConnectionData2.m_RouteConnectionType, routeConnectionData2.m_RouteRoadType);
					value3.m_AccessRestriction = GetAccessRestriction(owner3, flag2, isTakeOffLocation: true, out var allowEnter3, out var allowExit3);
					if (allowEnter3)
					{
						value3.m_Flags |= TakeoffLocationFlags.AllowEnter;
					}
					if (allowExit3)
					{
						value3.m_Flags |= TakeoffLocationFlags.AllowExit;
					}
				}
				nativeArray2[k] = value3;
			}
		}

		private Game.Prefabs.BuildingFlags GetRestrictFlag(RouteConnectionType routeConnectionType, RoadTypes routeRoadType)
		{
			switch (routeConnectionType)
			{
			case RouteConnectionType.Road:
				if ((routeRoadType & (RoadTypes.Car | RoadTypes.Helicopter | RoadTypes.Airplane | RoadTypes.Bicycle)) == 0)
				{
					return (Game.Prefabs.BuildingFlags)0u;
				}
				return Game.Prefabs.BuildingFlags.RestrictedCar;
			case RouteConnectionType.Pedestrian:
				return Game.Prefabs.BuildingFlags.RestrictedPedestrian;
			case RouteConnectionType.Cargo:
				return Game.Prefabs.BuildingFlags.RestrictedCar;
			case RouteConnectionType.Parking:
				if (routeRoadType != RoadTypes.Bicycle)
				{
					return Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar;
				}
				return Game.Prefabs.BuildingFlags.RestrictedPedestrian;
			case RouteConnectionType.Air:
				return Game.Prefabs.BuildingFlags.RestrictedCar;
			case RouteConnectionType.Track:
				return Game.Prefabs.BuildingFlags.RestrictedTrack;
			case RouteConnectionType.Offroad:
				return Game.Prefabs.BuildingFlags.RestrictedCar;
			default:
				return (Game.Prefabs.BuildingFlags)0u;
			}
		}

		private Entity GetAccessRestriction(Owner owner, Game.Prefabs.BuildingFlags flag, bool isTakeOffLocation, out bool allowEnter, out bool allowExit)
		{
			Entity entity = owner.m_Owner;
			Game.Prefabs.BuildingFlags buildingFlags = Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar | Game.Prefabs.BuildingFlags.RestrictedParking | Game.Prefabs.BuildingFlags.RestrictedTrack;
			while (m_OwnerData.TryGetComponent(entity, out owner))
			{
				if (m_BuildingData.HasComponent(entity))
				{
					PrefabRef prefabRef = m_PrefabRefData[entity];
					buildingFlags &= m_PrefabBuildingData[prefabRef.m_Prefab].m_Flags;
				}
				entity = owner.m_Owner;
			}
			if (m_AttachmentData.TryGetComponent(entity, out var componentData) && componentData.m_Attached != Entity.Null)
			{
				entity = componentData.m_Attached;
			}
			if (m_BuildingData.HasComponent(entity))
			{
				PrefabRef prefabRef2 = m_PrefabRefData[entity];
				BuildingData buildingData = m_PrefabBuildingData[prefabRef2.m_Prefab];
				buildingData.m_Flags &= buildingFlags;
				bool flag2 = (buildingData.m_Flags & flag) != 0;
				bool flag3 = (flag & Game.Prefabs.BuildingFlags.RestrictedCar) != 0;
				bool flag4 = (flag & Game.Prefabs.BuildingFlags.RestrictedPedestrian) != 0;
				if (flag2 || flag3 || flag4)
				{
					allowEnter = !flag2;
					if (flag3 && flag4)
					{
						allowExit = (buildingData.m_Flags & Game.Prefabs.BuildingFlags.RestrictedParking) == 0;
					}
					else if (flag4)
					{
						if (allowEnter && (buildingData.m_Flags & Game.Prefabs.BuildingFlags.RestrictedCar) != 0)
						{
							allowEnter &= isTakeOffLocation;
							allowExit = allowEnter;
						}
						else
						{
							allowExit = false;
						}
					}
					else
					{
						allowExit = false;
					}
					return entity;
				}
			}
			allowEnter = false;
			allowExit = false;
			return Entity.Null;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Routes.TransportStop> __Game_Routes_TransportStop_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Routes.TakeoffLocation> __Game_Routes_TakeoffLocation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> __Game_Prefabs_RouteConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Routes_TransportStop_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.TransportStop>();
			__Game_Routes_TakeoffLocation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.TakeoffLocation>();
			__Game_Objects_SpawnLocation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.SpawnLocation>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_RouteConnectionData_RO_ComponentLookup = state.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdateQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Routes.TransportStop>(),
				ComponentType.ReadOnly<Game.Routes.TakeoffLocation>(),
				ComponentType.ReadOnly<Game.Objects.SpawnLocation>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Game.Routes.MailBox>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PathfindUpdated>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Routes.TransportStop>(),
				ComponentType.ReadOnly<Game.Routes.TakeoffLocation>(),
				ComponentType.ReadOnly<Game.Objects.SpawnLocation>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Game.Routes.MailBox>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		RequireForUpdate(m_UpdateQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateRouteDataJob jobData = new UpdateRouteDataJob
		{
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TransportStop_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TakeoffLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TakeoffLocation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_SpawnLocation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRouteConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_UpdateQuery, base.Dependency);
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
	public RouteDataSystem()
	{
	}
}
