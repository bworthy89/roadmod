using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ValidationSystem : GameSystemBase
{
	public struct ChunkType
	{
		public EntityTypeHandle m_Entity;

		public ComponentTypeHandle<Temp> m_Temp;

		public ComponentTypeHandle<Owner> m_Owner;

		public ComponentTypeHandle<Native> m_Native;

		public ComponentTypeHandle<Brush> m_Brush;

		public ComponentTypeHandle<PrefabRef> m_PrefabRef;

		public ComponentTypeHandle<Game.Objects.Object> m_Object;

		public ComponentTypeHandle<Game.Objects.Transform> m_Transform;

		public ComponentTypeHandle<Attached> m_Attached;

		public ComponentTypeHandle<Game.Objects.NetObject> m_NetObject;

		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnection;

		public ComponentTypeHandle<Building> m_Building;

		public ComponentTypeHandle<Game.Buildings.ServiceUpgrade> m_ServiceUpgrade;

		public ComponentTypeHandle<Game.Routes.TransportStop> m_TransportStop;

		public ComponentTypeHandle<Game.Net.Edge> m_Edge;

		public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometry;

		public ComponentTypeHandle<StartNodeGeometry> m_StartNodeGeometry;

		public ComponentTypeHandle<EndNodeGeometry> m_EndNodeGeometry;

		public ComponentTypeHandle<Composition> m_Composition;

		public ComponentTypeHandle<Lane> m_Lane;

		public ComponentTypeHandle<Game.Net.TrackLane> m_TrackLane;

		public ComponentTypeHandle<Curve> m_Curve;

		public ComponentTypeHandle<EdgeLane> m_EdgeLane;

		public ComponentTypeHandle<Fixed> m_Fixed;

		public ComponentTypeHandle<Area> m_Area;

		public ComponentTypeHandle<Geometry> m_AreaGeometry;

		public ComponentTypeHandle<Storage> m_AreaStorage;

		public BufferTypeHandle<Game.Areas.Node> m_AreaNode;

		public BufferTypeHandle<RouteWaypoint> m_RouteWaypoint;

		public BufferTypeHandle<RouteSegment> m_RouteSegment;

		public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_WaterSourceData;

		public ChunkType(SystemBase system)
		{
			m_Entity = system.GetEntityTypeHandle();
			m_Temp = system.GetComponentTypeHandle<Temp>(isReadOnly: true);
			m_Owner = system.GetComponentTypeHandle<Owner>(isReadOnly: true);
			m_Native = system.GetComponentTypeHandle<Native>(isReadOnly: true);
			m_Brush = system.GetComponentTypeHandle<Brush>(isReadOnly: true);
			m_PrefabRef = system.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			m_Object = system.GetComponentTypeHandle<Game.Objects.Object>(isReadOnly: true);
			m_Transform = system.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			m_Attached = system.GetComponentTypeHandle<Attached>(isReadOnly: true);
			m_NetObject = system.GetComponentTypeHandle<Game.Objects.NetObject>(isReadOnly: true);
			m_OutsideConnection = system.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
			m_Building = system.GetComponentTypeHandle<Building>(isReadOnly: true);
			m_ServiceUpgrade = system.GetComponentTypeHandle<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			m_TransportStop = system.GetComponentTypeHandle<Game.Routes.TransportStop>(isReadOnly: true);
			m_Edge = system.GetComponentTypeHandle<Game.Net.Edge>(isReadOnly: true);
			m_EdgeGeometry = system.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
			m_StartNodeGeometry = system.GetComponentTypeHandle<StartNodeGeometry>(isReadOnly: true);
			m_EndNodeGeometry = system.GetComponentTypeHandle<EndNodeGeometry>(isReadOnly: true);
			m_Composition = system.GetComponentTypeHandle<Composition>(isReadOnly: true);
			m_Lane = system.GetComponentTypeHandle<Lane>(isReadOnly: true);
			m_TrackLane = system.GetComponentTypeHandle<Game.Net.TrackLane>(isReadOnly: true);
			m_Curve = system.GetComponentTypeHandle<Curve>(isReadOnly: true);
			m_EdgeLane = system.GetComponentTypeHandle<EdgeLane>(isReadOnly: true);
			m_Fixed = system.GetComponentTypeHandle<Fixed>(isReadOnly: true);
			m_Area = system.GetComponentTypeHandle<Area>(isReadOnly: true);
			m_AreaGeometry = system.GetComponentTypeHandle<Geometry>(isReadOnly: true);
			m_AreaStorage = system.GetComponentTypeHandle<Storage>(isReadOnly: true);
			m_AreaNode = system.GetBufferTypeHandle<Game.Areas.Node>(isReadOnly: true);
			m_RouteWaypoint = system.GetBufferTypeHandle<RouteWaypoint>(isReadOnly: true);
			m_RouteSegment = system.GetBufferTypeHandle<RouteSegment>(isReadOnly: true);
			m_WaterSourceData = system.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(isReadOnly: true);
		}

		public void Update(SystemBase system)
		{
			m_Entity.Update(system);
			m_Temp.Update(system);
			m_Owner.Update(system);
			m_Native.Update(system);
			m_Brush.Update(system);
			m_PrefabRef.Update(system);
			m_Object.Update(system);
			m_Transform.Update(system);
			m_Attached.Update(system);
			m_NetObject.Update(system);
			m_OutsideConnection.Update(system);
			m_Building.Update(system);
			m_ServiceUpgrade.Update(system);
			m_TransportStop.Update(system);
			m_Edge.Update(system);
			m_EdgeGeometry.Update(system);
			m_StartNodeGeometry.Update(system);
			m_EndNodeGeometry.Update(system);
			m_Composition.Update(system);
			m_Lane.Update(system);
			m_TrackLane.Update(system);
			m_Curve.Update(system);
			m_EdgeLane.Update(system);
			m_Fixed.Update(system);
			m_Area.Update(system);
			m_AreaGeometry.Update(system);
			m_AreaStorage.Update(system);
			m_AreaNode.Update(system);
			m_RouteWaypoint.Update(system);
			m_RouteSegment.Update(system);
			m_WaterSourceData.Update(system);
		}
	}

	public struct EntityData
	{
		public ComponentLookup<Owner> m_Owner;

		public ComponentLookup<Hidden> m_Hidden;

		public ComponentLookup<Temp> m_Temp;

		public ComponentLookup<Native> m_Native;

		public ComponentLookup<Game.Objects.Transform> m_Transform;

		public ComponentLookup<Game.Objects.Elevation> m_ObjectElevation;

		public ComponentLookup<Secondary> m_Secondary;

		public ComponentLookup<AssetStamp> m_AssetStamp;

		public ComponentLookup<Attachment> m_Attachment;

		public ComponentLookup<Attached> m_Attached;

		public ComponentLookup<Stack> m_Stack;

		public ComponentLookup<Building> m_Building;

		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgrade;

		public BufferLookup<InstalledUpgrade> m_Upgrades;

		public ComponentLookup<Game.Net.Node> m_Node;

		public ComponentLookup<Game.Net.Edge> m_Edge;

		public ComponentLookup<EdgeGeometry> m_EdgeGeometry;

		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometry;

		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometry;

		public ComponentLookup<Composition> m_Composition;

		public ComponentLookup<Game.Net.Elevation> m_NetElevation;

		public ComponentLookup<Lane> m_Lane;

		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLane;

		public ComponentLookup<Game.Net.CarLane> m_CarLane;

		public ComponentLookup<Game.Net.TrackLane> m_TrackLane;

		public ComponentLookup<Curve> m_Curve;

		public BufferLookup<Game.Net.SubLane> m_Lanes;

		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		public ComponentLookup<Area> m_Area;

		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		public BufferLookup<Triangle> m_AreaTriangles;

		public ComponentLookup<PathInformation> m_PathInformation;

		public ComponentLookup<Route> m_Route;

		public ComponentLookup<Connected> m_RouteConnected;

		public ComponentLookup<OnFire> m_OnFire;

		public ComponentLookup<PrefabRef> m_PrefabRef;

		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometry;

		public ComponentLookup<BuildingData> m_PrefabBuilding;

		public ComponentLookup<PlaceableObjectData> m_PlaceableObject;

		public ComponentLookup<StackData> m_PrefabStackData;

		public ComponentLookup<NetObjectData> m_PrefabNetObject;

		public ComponentLookup<PlaceableNetData> m_PlaceableNet;

		public ComponentLookup<NetCompositionData> m_PrefabComposition;

		public ComponentLookup<NetGeometryData> m_PrefabNetGeometry;

		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometry;

		public ComponentLookup<StorageAreaData> m_PrefabStorageArea;

		public ComponentLookup<LotData> m_PrefabLotData;

		public ComponentLookup<CarLaneData> m_CarLaneData;

		public ComponentLookup<TrackLaneData> m_TrackLaneData;

		public ComponentLookup<RouteConnectionData> m_RouteConnectionData;

		public ComponentLookup<TransportStopData> m_TransportStopData;

		public ComponentLookup<TransportLineData> m_TransportLineData;

		public ComponentLookup<WaterPumpingStationData> m_WaterPumpingStationData;

		public ComponentLookup<GroundWaterPoweredData> m_GroundWaterPoweredData;

		public ComponentLookup<TerraformingData> m_TerraformingData;

		public ComponentLookup<ServiceUpgradeData> m_ServiceUpgradeData;

		public BufferLookup<NetCompositionArea> m_PrefabCompositionAreas;

		public BufferLookup<FixedNetElement> m_PrefabFixedElements;

		public EntityData(SystemBase system)
		{
			m_Owner = system.GetComponentLookup<Owner>(isReadOnly: true);
			m_Hidden = system.GetComponentLookup<Hidden>(isReadOnly: true);
			m_Temp = system.GetComponentLookup<Temp>(isReadOnly: true);
			m_Native = system.GetComponentLookup<Native>(isReadOnly: true);
			m_Transform = system.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			m_ObjectElevation = system.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			m_Secondary = system.GetComponentLookup<Secondary>(isReadOnly: true);
			m_AssetStamp = system.GetComponentLookup<AssetStamp>(isReadOnly: true);
			m_Attachment = system.GetComponentLookup<Attachment>(isReadOnly: true);
			m_Attached = system.GetComponentLookup<Attached>(isReadOnly: true);
			m_Stack = system.GetComponentLookup<Stack>(isReadOnly: true);
			m_Building = system.GetComponentLookup<Building>(isReadOnly: true);
			m_ServiceUpgrade = system.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			m_Upgrades = system.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			m_Node = system.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			m_Edge = system.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
			m_EdgeGeometry = system.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			m_StartNodeGeometry = system.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			m_EndNodeGeometry = system.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			m_Composition = system.GetComponentLookup<Composition>(isReadOnly: true);
			m_NetElevation = system.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			m_Lane = system.GetComponentLookup<Lane>(isReadOnly: true);
			m_PedestrianLane = system.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			m_CarLane = system.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			m_TrackLane = system.GetComponentLookup<Game.Net.TrackLane>(isReadOnly: true);
			m_Curve = system.GetComponentLookup<Curve>(isReadOnly: true);
			m_Lanes = system.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			m_ConnectedNodes = system.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			m_ConnectedEdges = system.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			m_Area = system.GetComponentLookup<Area>(isReadOnly: true);
			m_AreaNodes = system.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			m_AreaTriangles = system.GetBufferLookup<Triangle>(isReadOnly: true);
			m_PathInformation = system.GetComponentLookup<PathInformation>(isReadOnly: true);
			m_Route = system.GetComponentLookup<Route>(isReadOnly: true);
			m_RouteConnected = system.GetComponentLookup<Connected>(isReadOnly: true);
			m_OnFire = system.GetComponentLookup<OnFire>(isReadOnly: true);
			m_PrefabRef = system.GetComponentLookup<PrefabRef>(isReadOnly: true);
			m_PrefabObjectGeometry = system.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			m_PrefabBuilding = system.GetComponentLookup<BuildingData>(isReadOnly: true);
			m_PlaceableObject = system.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			m_PrefabStackData = system.GetComponentLookup<StackData>(isReadOnly: true);
			m_PrefabNetObject = system.GetComponentLookup<NetObjectData>(isReadOnly: true);
			m_PlaceableNet = system.GetComponentLookup<PlaceableNetData>(isReadOnly: true);
			m_PrefabComposition = system.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			m_PrefabNetGeometry = system.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			m_PrefabAreaGeometry = system.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			m_PrefabStorageArea = system.GetComponentLookup<StorageAreaData>(isReadOnly: true);
			m_PrefabLotData = system.GetComponentLookup<LotData>(isReadOnly: true);
			m_CarLaneData = system.GetComponentLookup<CarLaneData>(isReadOnly: true);
			m_TrackLaneData = system.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			m_RouteConnectionData = system.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			m_TransportStopData = system.GetComponentLookup<TransportStopData>(isReadOnly: true);
			m_TransportLineData = system.GetComponentLookup<TransportLineData>(isReadOnly: true);
			m_WaterPumpingStationData = system.GetComponentLookup<WaterPumpingStationData>(isReadOnly: true);
			m_GroundWaterPoweredData = system.GetComponentLookup<GroundWaterPoweredData>(isReadOnly: true);
			m_TerraformingData = system.GetComponentLookup<TerraformingData>(isReadOnly: true);
			m_ServiceUpgradeData = system.GetComponentLookup<ServiceUpgradeData>(isReadOnly: true);
			m_PrefabCompositionAreas = system.GetBufferLookup<NetCompositionArea>(isReadOnly: true);
			m_PrefabFixedElements = system.GetBufferLookup<FixedNetElement>(isReadOnly: true);
		}

		public void Update(SystemBase system)
		{
			m_Owner.Update(system);
			m_Hidden.Update(system);
			m_Temp.Update(system);
			m_Native.Update(system);
			m_Transform.Update(system);
			m_ObjectElevation.Update(system);
			m_Secondary.Update(system);
			m_AssetStamp.Update(system);
			m_Attachment.Update(system);
			m_Attached.Update(system);
			m_Stack.Update(system);
			m_Building.Update(system);
			m_ServiceUpgrade.Update(system);
			m_Upgrades.Update(system);
			m_Node.Update(system);
			m_Edge.Update(system);
			m_EdgeGeometry.Update(system);
			m_StartNodeGeometry.Update(system);
			m_EndNodeGeometry.Update(system);
			m_Composition.Update(system);
			m_NetElevation.Update(system);
			m_Lane.Update(system);
			m_PedestrianLane.Update(system);
			m_CarLane.Update(system);
			m_TrackLane.Update(system);
			m_Curve.Update(system);
			m_Lanes.Update(system);
			m_ConnectedNodes.Update(system);
			m_ConnectedEdges.Update(system);
			m_Area.Update(system);
			m_AreaNodes.Update(system);
			m_AreaTriangles.Update(system);
			m_PathInformation.Update(system);
			m_Route.Update(system);
			m_RouteConnected.Update(system);
			m_OnFire.Update(system);
			m_PrefabRef.Update(system);
			m_PrefabObjectGeometry.Update(system);
			m_PrefabBuilding.Update(system);
			m_PlaceableObject.Update(system);
			m_PrefabStackData.Update(system);
			m_PrefabNetObject.Update(system);
			m_PlaceableNet.Update(system);
			m_PrefabComposition.Update(system);
			m_PrefabNetGeometry.Update(system);
			m_PrefabAreaGeometry.Update(system);
			m_PrefabStorageArea.Update(system);
			m_PrefabLotData.Update(system);
			m_CarLaneData.Update(system);
			m_TrackLaneData.Update(system);
			m_RouteConnectionData.Update(system);
			m_TransportStopData.Update(system);
			m_TransportLineData.Update(system);
			m_WaterPumpingStationData.Update(system);
			m_GroundWaterPoweredData.Update(system);
			m_TerraformingData.Update(system);
			m_ServiceUpgradeData.Update(system);
			m_PrefabCompositionAreas.Update(system);
			m_PrefabFixedElements.Update(system);
		}
	}

	[CompilerGenerated]
	public class Components : GameSystemBase
	{
		[BurstCompile]
		private struct UpdateComponentsJob : IJob
		{
			[ReadOnly]
			public EntityTypeHandle m_EntityType;

			[ReadOnly]
			public ComponentTypeHandle<Error> m_ErrorType;

			[ReadOnly]
			public ComponentTypeHandle<Warning> m_WarningType;

			[ReadOnly]
			public ComponentTypeHandle<Override> m_OverrideType;

			[ReadOnly]
			public NativeList<ArchetypeChunk> m_Chunks;

			[NativeDisableContainerSafetyRestriction]
			public NativeHashMap<Entity, ErrorSeverity> m_ErrorMap;

			public EntityCommandBuffer m_CommandBuffer;

			public void Execute()
			{
				NativeList<Entity> nativeList = new NativeList<Entity>(32, Allocator.Temp);
				NativeList<Entity> nativeList2 = new NativeList<Entity>(32, Allocator.Temp);
				NativeList<Entity> nativeList3 = new NativeList<Entity>(32, Allocator.Temp);
				NativeList<Entity> nativeList4 = new NativeList<Entity>(32, Allocator.Temp);
				for (int i = 0; i < m_Chunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = m_Chunks[i];
					NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
					if (archetypeChunk.Has(ref m_ErrorType))
					{
						if (m_ErrorMap.IsCreated)
						{
							for (int j = 0; j < nativeArray.Length; j++)
							{
								Entity value = nativeArray[j];
								if (m_ErrorMap.TryGetValue(value, out var item) && item == ErrorSeverity.Error)
								{
									m_ErrorMap.Remove(value);
									continue;
								}
								nativeList.Add(in value);
								nativeList4.Add(in value);
							}
						}
						else
						{
							nativeList.AddRange(nativeArray);
							nativeList4.AddRange(nativeArray);
						}
					}
					if (archetypeChunk.Has(ref m_WarningType))
					{
						if (m_ErrorMap.IsCreated)
						{
							for (int k = 0; k < nativeArray.Length; k++)
							{
								Entity value2 = nativeArray[k];
								if (m_ErrorMap.TryGetValue(value2, out var item2) && item2 == ErrorSeverity.Warning)
								{
									m_ErrorMap.Remove(value2);
									continue;
								}
								nativeList2.Add(in value2);
								nativeList4.Add(in value2);
							}
						}
						else
						{
							nativeList2.AddRange(nativeArray);
							nativeList4.AddRange(nativeArray);
						}
					}
					if (!archetypeChunk.Has(ref m_OverrideType))
					{
						continue;
					}
					if (m_ErrorMap.IsCreated)
					{
						for (int l = 0; l < nativeArray.Length; l++)
						{
							Entity value3 = nativeArray[l];
							if (m_ErrorMap.TryGetValue(value3, out var item3) && item3 == ErrorSeverity.Override)
							{
								m_ErrorMap.Remove(value3);
								continue;
							}
							nativeList3.Add(in value3);
							nativeList4.Add(in value3);
						}
					}
					else
					{
						nativeList3.AddRange(nativeArray);
						nativeList4.AddRange(nativeArray);
					}
				}
				if (nativeList4.Length != 0)
				{
					m_CommandBuffer.AddComponent<BatchesUpdated>(nativeList4.AsArray());
					nativeList4.Clear();
				}
				if (nativeList.Length != 0)
				{
					m_CommandBuffer.RemoveComponent<Error>(nativeList.AsArray());
					nativeList.Clear();
				}
				if (nativeList2.Length != 0)
				{
					m_CommandBuffer.RemoveComponent<Warning>(nativeList2.AsArray());
					nativeList2.Clear();
				}
				if (nativeList3.Length != 0)
				{
					m_CommandBuffer.RemoveComponent<Override>(nativeList3.AsArray());
					nativeList3.Clear();
				}
				if (m_ErrorMap.IsCreated)
				{
					NativeHashMap<Entity, ErrorSeverity>.Enumerator enumerator = m_ErrorMap.GetEnumerator();
					while (enumerator.MoveNext())
					{
						switch (enumerator.Current.Value)
						{
						case ErrorSeverity.Error:
							nativeList.Add(enumerator.Current.Key);
							nativeList4.Add(enumerator.Current.Key);
							break;
						case ErrorSeverity.Warning:
							nativeList2.Add(enumerator.Current.Key);
							nativeList4.Add(enumerator.Current.Key);
							break;
						case ErrorSeverity.Override:
							nativeList3.Add(enumerator.Current.Key);
							nativeList4.Add(enumerator.Current.Key);
							break;
						}
					}
					enumerator.Dispose();
					if (nativeList.Length != 0)
					{
						m_CommandBuffer.AddComponent<Error>(nativeList.AsArray());
					}
					if (nativeList2.Length != 0)
					{
						m_CommandBuffer.AddComponent<Warning>(nativeList2.AsArray());
					}
					if (nativeList3.Length != 0)
					{
						m_CommandBuffer.AddComponent<Override>(nativeList3.AsArray());
					}
					if (nativeList4.Length != 0)
					{
						m_CommandBuffer.AddComponent<BatchesUpdated>(nativeList4.AsArray());
					}
				}
				nativeList.Dispose();
				nativeList2.Dispose();
				nativeList3.Dispose();
				nativeList4.Dispose();
			}
		}

		private struct TypeHandle
		{
			[ReadOnly]
			public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Error> __Game_Tools_Error_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Warning> __Game_Tools_Warning_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Override> __Game_Tools_Override_RO_ComponentTypeHandle;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
				__Game_Tools_Error_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Error>(isReadOnly: true);
				__Game_Tools_Warning_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Warning>(isReadOnly: true);
				__Game_Tools_Override_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Override>(isReadOnly: true);
			}
		}

		private ToolSystem m_ToolSystem;

		private ModificationEndBarrier m_ModificationBarrier;

		private EntityQuery m_ComponentQuery;

		public NativeHashMap<Entity, ErrorSeverity> m_ErrorMap;

		public JobHandle m_ErrorMapDeps;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
			m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
			m_ComponentQuery = GetEntityQuery(new EntityQueryDesc
			{
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Error>(),
					ComponentType.ReadOnly<Warning>(),
					ComponentType.ReadOnly<Override>()
				},
				None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
			});
		}

		[Preserve]
		protected override void OnUpdate()
		{
			if (m_ErrorMap.IsCreated || (m_ToolSystem.applyMode != ApplyMode.None && !m_ComponentQuery.IsEmptyIgnoreFilter))
			{
				JobHandle outJobHandle;
				NativeList<ArchetypeChunk> chunks = m_ComponentQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
				JobHandle jobHandle = IJobExtensions.Schedule(new UpdateComponentsJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_ErrorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Error_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_WarningType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Warning_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OverrideType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Override_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_Chunks = chunks,
					m_ErrorMap = m_ErrorMap,
					m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
				}, JobHandle.CombineDependencies(base.Dependency, m_ErrorMapDeps, outJobHandle));
				if (m_ErrorMap.IsCreated)
				{
					m_ErrorMap.Dispose(jobHandle);
				}
				chunks.Dispose(jobHandle);
				m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
				base.Dependency = jobHandle;
			}
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
		public Components()
		{
		}
	}

	public struct BoundsData
	{
		public Bounds3 m_Bounds;

		public Entity m_Entity;
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct BoundsComparerX : IComparer<BoundsData>
	{
		public int Compare(BoundsData x, BoundsData y)
		{
			return math.select(math.select(1, -1, x.m_Bounds.min.x < y.m_Bounds.min.x), 0, x.m_Bounds.min.x == y.m_Bounds.min.x);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct BoundsComparerZ : IComparer<BoundsData>
	{
		public int Compare(BoundsData x, BoundsData y)
		{
			return math.select(math.select(1, -1, x.m_Bounds.min.z < y.m_Bounds.min.z), 0, x.m_Bounds.min.z == y.m_Bounds.min.z);
		}
	}

	[BurstCompile]
	private struct BoundsListJob : IJob
	{
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ChunkType m_ChunkType;

		[ReadOnly]
		public EntityData m_EntityData;

		public NativeList<BoundsData> m_EdgeList;

		public NativeList<BoundsData> m_ObjectList;

		public void Execute()
		{
			Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
			Bounds3 bounds2 = new Bounds3(float.MaxValue, float.MinValue);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Game.Objects.Transform> nativeArray = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Transform);
				NativeArray<EdgeGeometry> nativeArray2 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_EdgeGeometry);
				if (nativeArray2.Length != 0)
				{
					NativeArray<Entity> nativeArray3 = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
					NativeArray<Temp> nativeArray4 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Temp);
					NativeArray<StartNodeGeometry> nativeArray5 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_StartNodeGeometry);
					NativeArray<EndNodeGeometry> nativeArray6 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_EndNodeGeometry);
					for (int j = 0; j < nativeArray3.Length; j++)
					{
						if ((nativeArray4[j].m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) == 0)
						{
							EdgeGeometry edgeGeometry = nativeArray2[j];
							StartNodeGeometry startNodeGeometry = nativeArray5[j];
							EndNodeGeometry endNodeGeometry = nativeArray6[j];
							BoundsData value = new BoundsData
							{
								m_Entity = nativeArray3[j],
								m_Bounds = (edgeGeometry.m_Bounds | startNodeGeometry.m_Geometry.m_Bounds | endNodeGeometry.m_Geometry.m_Bounds)
							};
							if (math.any(math.isnan(value.m_Bounds.min) | math.isnan(value.m_Bounds.max)))
							{
								UnityEngine.Debug.LogWarning($"Edge has NaN bounds: {value.m_Entity.Index}");
								continue;
							}
							m_EdgeList.Add(in value);
							bounds |= value.m_Bounds;
						}
					}
				}
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<Entity> nativeArray7 = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
				NativeArray<Temp> nativeArray8 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Temp);
				NativeArray<PrefabRef> nativeArray9 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_PrefabRef);
				for (int k = 0; k < nativeArray7.Length; k++)
				{
					if ((nativeArray8[k].m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) != 0)
					{
						continue;
					}
					PrefabRef prefabRef = nativeArray9[k];
					if (m_EntityData.m_PrefabObjectGeometry.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						BoundsData value2 = new BoundsData
						{
							m_Entity = nativeArray7[k]
						};
						Game.Objects.Transform transform = nativeArray[k];
						if (m_EntityData.m_Stack.TryGetComponent(value2.m_Entity, out var componentData2) && m_EntityData.m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
						{
							value2.m_Bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, componentData2, componentData, componentData3);
						}
						else
						{
							value2.m_Bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, componentData);
						}
						if (math.any(math.isnan(value2.m_Bounds.min) | math.isnan(value2.m_Bounds.max)))
						{
							UnityEngine.Debug.LogWarning($"Object has NaN bounds: {value2.m_Entity.Index}");
							continue;
						}
						m_ObjectList.Add(in value2);
						bounds2 |= value2.m_Bounds;
					}
				}
			}
			if (m_EdgeList.Length >= 2)
			{
				float3 @float = MathUtils.Size(bounds);
				if (@float.z > @float.x)
				{
					m_EdgeList.Sort(default(BoundsComparerZ));
				}
				else
				{
					m_EdgeList.Sort(default(BoundsComparerX));
				}
			}
			if (m_ObjectList.Length >= 2)
			{
				float3 float2 = MathUtils.Size(bounds2);
				if (float2.z > float2.x)
				{
					m_ObjectList.Sort(default(BoundsComparerZ));
				}
				else
				{
					m_ObjectList.Sort(default(BoundsComparerX));
				}
			}
		}
	}

	[BurstCompile]
	private struct ValidationJob : IJobParallelForDefer
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ChunkType m_ChunkType;

		[ReadOnly]
		public EntityData m_EntityData;

		[ReadOnly]
		public NativeList<BoundsData> m_EdgeList;

		[ReadOnly]
		public NativeList<BoundsData> m_ObjectList;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public NativeParallelHashMap<Entity, int> m_InstanceCounts;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public NativeArray<GroundWater> m_GroundWaterMap;

		[ReadOnly]
		public Bounds3 m_worldBounds;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		[NativeDisableContainerSafetyRestriction]
		private NativeList<ConnectedNode> m_TempNodes;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_Chunks[index];
			TempFlags tempFlags = (archetypeChunk.Has(ref m_ChunkType.m_Native) ? (TempFlags.Select | TempFlags.Duplicate) : (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate));
			if (archetypeChunk.Has(ref m_ChunkType.m_Object))
			{
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
				NativeArray<Temp> nativeArray2 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Temp);
				NativeArray<Owner> nativeArray3 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Owner);
				NativeArray<Game.Objects.Transform> nativeArray4 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Transform);
				NativeArray<Attached> nativeArray5 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Attached);
				NativeArray<Game.Objects.NetObject> nativeArray6 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_NetObject);
				NativeArray<PrefabRef> nativeArray7 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_PrefabRef);
				NativeArray<Building> nativeArray8 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Building);
				bool flag = archetypeChunk.Has(ref m_ChunkType.m_OutsideConnection);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Temp temp = nativeArray2[i];
					if ((temp.m_Flags & tempFlags) == 0)
					{
						Entity entity = nativeArray[i];
						Game.Objects.Transform transform = nativeArray4[i];
						PrefabRef prefabRef = nativeArray7[i];
						Owner owner = default(Owner);
						if (nativeArray3.Length != 0)
						{
							owner = nativeArray3[i];
						}
						Attached attached = default(Attached);
						if (nativeArray5.Length != 0)
						{
							attached = nativeArray5[i];
						}
						Game.Objects.ValidationHelpers.ValidateObject(entity, temp, owner, transform, prefabRef, attached, flag, m_EditorMode, m_EntityData, m_EdgeList, m_ObjectList, m_ObjectSearchTree, m_NetSearchTree, m_AreaSearchTree, m_InstanceCounts, m_WaterSurfaceData, m_TerrainHeightData, m_ErrorQueue);
					}
					if ((temp.m_Flags & (TempFlags.Delete | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) != 0 && temp.m_Original != Entity.Null && m_EntityData.m_OnFire.HasComponent(temp.m_Original))
					{
						ErrorData value = new ErrorData
						{
							m_ErrorType = ErrorType.OnFire,
							m_ErrorSeverity = ErrorSeverity.Error,
							m_TempEntity = nativeArray[i],
							m_Position = float.NaN
						};
						m_ErrorQueue.Enqueue(value);
					}
				}
				for (int j = 0; j < nativeArray8.Length; j++)
				{
					if ((nativeArray2[j].m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) == 0)
					{
						Entity entity2 = nativeArray[j];
						Building building = nativeArray8[j];
						Game.Objects.Transform transform2 = nativeArray4[j];
						PrefabRef prefabRef2 = nativeArray7[j];
						Game.Buildings.ValidationHelpers.ValidateBuilding(entity2, building, transform2, prefabRef2, m_EntityData, m_GroundWaterMap, m_ErrorQueue);
					}
				}
				for (int k = 0; k < nativeArray6.Length; k++)
				{
					if ((nativeArray2[k].m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) == 0)
					{
						Entity entity3 = nativeArray[k];
						Game.Objects.NetObject netObject = nativeArray6[k];
						Game.Objects.Transform transform3 = nativeArray4[k];
						PrefabRef prefabRef3 = nativeArray7[k];
						Owner owner2 = default(Owner);
						if (nativeArray3.Length != 0)
						{
							owner2 = nativeArray3[k];
						}
						Attached attached2 = default(Attached);
						if (nativeArray5.Length != 0)
						{
							attached2 = nativeArray5[k];
						}
						Game.Objects.ValidationHelpers.ValidateNetObject(entity3, owner2, netObject, transform3, prefabRef3, attached2, m_EntityData, m_ErrorQueue);
					}
				}
				if (archetypeChunk.Has(ref m_ChunkType.m_TransportStop))
				{
					for (int l = 0; l < nativeArray.Length; l++)
					{
						Temp temp2 = nativeArray2[l];
						if ((temp2.m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) == 0)
						{
							Entity entity4 = nativeArray[l];
							Game.Objects.Transform transform4 = nativeArray4[l];
							PrefabRef prefabRef4 = nativeArray7[l];
							Owner owner3 = default(Owner);
							if (nativeArray3.Length != 0)
							{
								owner3 = nativeArray3[l];
							}
							Attached attached3 = default(Attached);
							if (nativeArray5.Length != 0)
							{
								attached3 = nativeArray5[l];
							}
							Game.Routes.ValidationHelpers.ValidateStop(m_EditorMode, entity4, temp2, owner3, transform4, prefabRef4, attached3, m_EntityData, m_ErrorQueue);
						}
					}
				}
				if (flag)
				{
					for (int m = 0; m < nativeArray.Length; m++)
					{
						if ((nativeArray2[m].m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) == 0)
						{
							Entity entity5 = nativeArray[m];
							Game.Objects.Transform transform5 = nativeArray4[m];
							Game.Objects.ValidationHelpers.ValidateOutsideConnection(entity5, transform5, m_TerrainHeightData, m_ErrorQueue);
						}
					}
				}
			}
			if (archetypeChunk.Has(ref m_ChunkType.m_ServiceUpgrade))
			{
				NativeArray<Entity> nativeArray9 = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
				NativeArray<PrefabRef> nativeArray10 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_PrefabRef);
				NativeArray<Owner> nativeArray11 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Owner);
				for (int n = 0; n < nativeArray9.Length; n++)
				{
					Entity entity6 = nativeArray9[n];
					PrefabRef prefabRef5 = nativeArray10[n];
					Owner owner4 = default(Owner);
					if (nativeArray11.Length != 0)
					{
						owner4 = nativeArray11[n];
					}
					Game.Buildings.ValidationHelpers.ValidateUpgrade(entity6, owner4, prefabRef5, m_EntityData, m_ErrorQueue);
				}
			}
			if (archetypeChunk.Has(ref m_ChunkType.m_Edge))
			{
				NativeArray<Entity> nativeArray12 = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
				NativeArray<Temp> nativeArray13 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Temp);
				NativeArray<Owner> nativeArray14 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Owner);
				NativeArray<Game.Net.Edge> nativeArray15 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Edge);
				NativeArray<EdgeGeometry> nativeArray16 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_EdgeGeometry);
				NativeArray<StartNodeGeometry> nativeArray17 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_StartNodeGeometry);
				NativeArray<EndNodeGeometry> nativeArray18 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_EndNodeGeometry);
				NativeArray<Composition> nativeArray19 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Composition);
				NativeArray<Fixed> nativeArray20 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Fixed);
				NativeArray<PrefabRef> nativeArray21 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_PrefabRef);
				if (!m_TempNodes.IsCreated)
				{
					m_TempNodes = new NativeList<ConnectedNode>(16, Allocator.Temp);
				}
				bool flag2 = nativeArray20.Length != 0;
				for (int num = 0; num < nativeArray16.Length; num++)
				{
					Temp temp3 = nativeArray13[num];
					if ((temp3.m_Flags & tempFlags) == 0)
					{
						Entity entity7 = nativeArray12[num];
						Game.Net.Edge edge = nativeArray15[num];
						EdgeGeometry edgeGeometry = nativeArray16[num];
						StartNodeGeometry startNodeGeometry = nativeArray17[num];
						EndNodeGeometry endNodeGeometry = nativeArray18[num];
						Composition composition = nativeArray19[num];
						PrefabRef prefabRef6 = nativeArray21[num];
						Owner owner5 = default(Owner);
						if (nativeArray14.Length != 0)
						{
							owner5 = nativeArray14[num];
						}
						Fixed obj = new Fixed
						{
							m_Index = -1
						};
						if (flag2)
						{
							obj = nativeArray20[num];
						}
						Game.Net.ValidationHelpers.ValidateEdge(entity7, temp3, owner5, obj, edge, edgeGeometry, startNodeGeometry, endNodeGeometry, composition, prefabRef6, m_EditorMode, m_EntityData, m_EdgeList, m_ObjectSearchTree, m_NetSearchTree, m_AreaSearchTree, m_WaterSurfaceData, m_TerrainHeightData, m_ErrorQueue, m_TempNodes);
					}
				}
			}
			if (archetypeChunk.Has(ref m_ChunkType.m_Lane))
			{
				NativeArray<Entity> nativeArray22 = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
				NativeArray<Temp> nativeArray23 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Temp);
				NativeArray<Owner> nativeArray24 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Owner);
				NativeArray<Lane> nativeArray25 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Lane);
				NativeArray<Game.Net.TrackLane> nativeArray26 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_TrackLane);
				NativeArray<Curve> nativeArray27 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Curve);
				NativeArray<EdgeLane> nativeArray28 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_EdgeLane);
				NativeArray<PrefabRef> nativeArray29 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_PrefabRef);
				for (int num2 = 0; num2 < nativeArray26.Length; num2++)
				{
					if ((nativeArray23[num2].m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) == 0)
					{
						Entity entity8 = nativeArray22[num2];
						Lane lane = nativeArray25[num2];
						Game.Net.TrackLane trackLane = nativeArray26[num2];
						Curve curve = nativeArray27[num2];
						PrefabRef prefabRef7 = nativeArray29[num2];
						Owner owner6 = default(Owner);
						if (nativeArray24.Length != 0)
						{
							owner6 = nativeArray24[num2];
						}
						EdgeLane edgeLane = default(EdgeLane);
						if (nativeArray28.Length != 0)
						{
							edgeLane = nativeArray28[num2];
						}
						Game.Net.ValidationHelpers.ValidateLane(entity8, owner6, lane, trackLane, curve, edgeLane, prefabRef7, m_EntityData, m_ErrorQueue);
					}
				}
			}
			if (archetypeChunk.Has(ref m_ChunkType.m_Area))
			{
				NativeArray<Entity> nativeArray30 = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
				NativeArray<Temp> nativeArray31 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Temp);
				NativeArray<Owner> nativeArray32 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Owner);
				NativeArray<Area> nativeArray33 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Area);
				NativeArray<Geometry> nativeArray34 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_AreaGeometry);
				NativeArray<Storage> nativeArray35 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_AreaStorage);
				BufferAccessor<Game.Areas.Node> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_ChunkType.m_AreaNode);
				NativeArray<PrefabRef> nativeArray36 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_PrefabRef);
				for (int num3 = 0; num3 < nativeArray30.Length; num3++)
				{
					Temp temp4 = nativeArray31[num3];
					if ((temp4.m_Flags & tempFlags) == 0)
					{
						Entity entity9 = nativeArray30[num3];
						Area area = nativeArray33[num3];
						DynamicBuffer<Game.Areas.Node> nodes = bufferAccessor[num3];
						PrefabRef prefabRef8 = nativeArray36[num3];
						Geometry geometry = default(Geometry);
						if (nativeArray34.Length != 0)
						{
							geometry = nativeArray34[num3];
						}
						Storage storage = default(Storage);
						if (nativeArray35.Length != 0)
						{
							storage = nativeArray35[num3];
						}
						Owner owner7 = default(Owner);
						if (nativeArray32.Length != 0)
						{
							owner7 = nativeArray32[num3];
						}
						Game.Areas.ValidationHelpers.ValidateArea(m_EditorMode, entity9, temp4, owner7, area, geometry, storage, nodes, prefabRef8, m_EntityData, m_ObjectSearchTree, m_NetSearchTree, m_AreaSearchTree, m_WaterSurfaceData, m_TerrainHeightData, m_ErrorQueue);
					}
				}
			}
			if (archetypeChunk.Has(ref m_ChunkType.m_RouteSegment))
			{
				NativeArray<Entity> nativeArray37 = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
				NativeArray<Temp> nativeArray38 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Temp);
				NativeArray<PrefabRef> nativeArray39 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_PrefabRef);
				BufferAccessor<RouteWaypoint> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_ChunkType.m_RouteWaypoint);
				BufferAccessor<RouteSegment> bufferAccessor3 = archetypeChunk.GetBufferAccessor(ref m_ChunkType.m_RouteSegment);
				for (int num4 = 0; num4 < nativeArray37.Length; num4++)
				{
					Temp temp5 = nativeArray38[num4];
					if ((temp5.m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) == 0)
					{
						Game.Routes.ValidationHelpers.ValidateRoute(nativeArray37[num4], prefabRef: nativeArray39[num4], waypoints: bufferAccessor2[num4], segments: bufferAccessor3[num4], temp: temp5, data: m_EntityData, errorQueue: m_ErrorQueue);
					}
				}
			}
			if (!m_EditorMode && archetypeChunk.Has(ref m_ChunkType.m_Brush))
			{
				NativeArray<Entity> nativeArray40 = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
				NativeArray<Brush> nativeArray41 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Brush);
				for (int num5 = 0; num5 < nativeArray40.Length; num5++)
				{
					Brush brush = nativeArray41[num5];
					if (m_EntityData.m_TerraformingData.HasComponent(brush.m_Tool))
					{
						Entity entity10 = nativeArray40[num5];
						Bounds3 bounds = new Bounds3(brush.m_Position - brush.m_Size * 0.4f, brush.m_Position + brush.m_Size * 0.4f);
						Game.Areas.ValidationHelpers.BrushAreaIterator iterator = new Game.Areas.ValidationHelpers.BrushAreaIterator
						{
							m_BrushEntity = entity10,
							m_Brush = brush,
							m_BrushBounds = bounds,
							m_Data = m_EntityData,
							m_ErrorQueue = m_ErrorQueue
						};
						m_AreaSearchTree.Iterate(ref iterator);
						Game.Objects.ValidationHelpers.ValidateWorldBounds(entity10, default(Owner), bounds, m_EntityData, m_TerrainHeightData, m_ErrorQueue);
					}
				}
			}
			if (!archetypeChunk.Has(ref m_ChunkType.m_WaterSourceData))
			{
				return;
			}
			NativeArray<Entity> nativeArray42 = archetypeChunk.GetNativeArray(m_ChunkType.m_Entity);
			NativeArray<Temp> nativeArray43 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Temp);
			NativeArray<Game.Objects.Transform> nativeArray44 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_Transform);
			NativeArray<Game.Simulation.WaterSourceData> nativeArray45 = archetypeChunk.GetNativeArray(ref m_ChunkType.m_WaterSourceData);
			for (int num6 = 0; num6 < nativeArray42.Length; num6++)
			{
				Temp temp6 = nativeArray43[num6];
				if ((temp6.m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) == 0 || (temp6.m_Flags & TempFlags.Dragging) != 0)
				{
					Entity entity11 = nativeArray42[num6];
					Game.Objects.Transform transform6 = nativeArray44[num6];
					Game.Simulation.WaterSourceData waterSourceData = nativeArray45[num6];
					Game.Objects.ValidationHelpers.ValidateWaterSource(entity11, transform6, waterSourceData, m_TerrainHeightData, m_worldBounds, m_ErrorQueue);
				}
			}
		}
	}

	[BurstCompile]
	private struct CollectAreaTrianglesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Native> m_NativeType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		public NativeList<AreaSearchItem> m_AreaTriangles;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			BufferAccessor<Triangle> bufferAccessor = chunk.GetBufferAccessor(ref m_TriangleType);
			TempFlags tempFlags = (chunk.Has(ref m_NativeType) ? TempFlags.Select : (TempFlags.Delete | TempFlags.Select));
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if ((nativeArray2[i].m_Flags & tempFlags) == 0)
				{
					Entity area = nativeArray[i];
					DynamicBuffer<Triangle> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						m_AreaTriangles.Add(new AreaSearchItem(area, j));
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
	private struct ValidateAreaTrianglesJob : IJobParallelForDefer
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public NativeArray<AreaSearchItem> m_AreaTriangles;

		[ReadOnly]
		public EntityData m_EntityData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public void Execute(int index)
		{
			AreaSearchItem areaSearchItem = m_AreaTriangles[index];
			Area area = m_EntityData.m_Area[areaSearchItem.m_Area];
			if ((area.m_Flags & AreaFlags.Slave) == 0)
			{
				DynamicBuffer<Triangle> dynamicBuffer = m_EntityData.m_AreaTriangles[areaSearchItem.m_Area];
				Temp temp = m_EntityData.m_Temp[areaSearchItem.m_Area];
				Owner owner = default(Owner);
				if (m_EntityData.m_Owner.HasComponent(areaSearchItem.m_Area))
				{
					owner = m_EntityData.m_Owner[areaSearchItem.m_Area];
				}
				bool noErrors = (area.m_Flags & AreaFlags.Complete) == 0;
				bool isCounterClockwise = (area.m_Flags & AreaFlags.CounterClockwise) != 0;
				Game.Areas.ValidationHelpers.ValidateTriangle(m_EditorMode, noErrors, isCounterClockwise, areaSearchItem.m_Area, temp, owner, dynamicBuffer[areaSearchItem.m_Triangle], m_EntityData, m_ObjectSearchTree, m_NetSearchTree, m_AreaSearchTree, m_WaterSurfaceData, m_TerrainHeightData, m_ErrorQueue);
			}
		}
	}

	[BurstCompile]
	private struct FillErrorPrefabsJob : IJobChunk
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ToolErrorData> m_ToolErrorType;

		public NativeArray<Entity> m_ErrorPrefabs;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ToolErrorData> nativeArray2 = chunk.GetNativeArray(ref m_ToolErrorType);
			ToolErrorFlags toolErrorFlags = (m_EditorMode ? ToolErrorFlags.DisableInEditor : ToolErrorFlags.DisableInGame);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity value = nativeArray[i];
				ToolErrorData toolErrorData = nativeArray2[i];
				if ((toolErrorData.m_Flags & toolErrorFlags) == 0)
				{
					m_ErrorPrefabs[(int)toolErrorData.m_Error] = value;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct IconKey : IEquatable<IconKey>
	{
		public Entity m_Owner;

		public Entity m_Target;

		public Entity m_Prefab;

		public bool Equals(IconKey other)
		{
			if (m_Owner.Equals(other.m_Owner) && m_Target.Equals(other.m_Target))
			{
				return m_Prefab.Equals(other.m_Prefab);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((17 * 31 + m_Owner.GetHashCode()) * 31 + m_Target.GetHashCode()) * 31 + m_Prefab.GetHashCode();
		}
	}

	public struct IconValue
	{
		public Bounds3 m_Bounds;

		public IconPriority m_Severity;

		public bool m_Cancelled;
	}

	[BurstCompile]
	private struct ProcessValidationResultsJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Brush> m_BrushType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PlayerMoney> m_PlayerMoney;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<Entity> m_ErrorPrefabs;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public Entity m_City;

		public NativeHashMap<Entity, ErrorSeverity> m_ErrorMap;

		public NativeQueue<ErrorData> m_ErrorQueue1;

		public NativeQueue<ErrorData> m_ErrorQueue2;

		public EntityCommandBuffer m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute()
		{
			NativeHashMap<IconKey, IconValue> iconMap = new NativeHashMap<IconKey, IconValue>(32, Allocator.Temp);
			int totalCost = 0;
			Entity brushEntity = Entity.Null;
			float4 brushPosition = default(float4);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				CalculateCost(m_Chunks[i], ref totalCost, ref brushEntity, ref brushPosition);
			}
			int num = totalCost;
			ProcessQueue(m_ErrorQueue1, iconMap, brushEntity, brushPosition, ref totalCost);
			ProcessQueue(m_ErrorQueue2, iconMap, brushEntity, brushPosition, ref totalCost);
			if (m_PlayerMoney.TryGetComponent(m_City, out var componentData))
			{
				int num2 = math.max(0, componentData.money);
				bool flag = totalCost > num2;
				if (flag)
				{
					for (int j = 0; j < m_Chunks.Length; j++)
					{
						CancelOptionalWithMoneyError(m_Chunks[j], iconMap, brushEntity, brushPosition, ref totalCost, num2);
						if (totalCost <= num2)
						{
							break;
						}
					}
					flag = totalCost > num2;
				}
				if (!flag && num > num2)
				{
					flag = true;
					for (int k = 0; k < m_Chunks.Length; k++)
					{
						if (!AllCancelled(m_Chunks[k], ErrorSeverity.Cancel))
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					for (int l = 0; l < m_Chunks.Length; l++)
					{
						ProcessMoneyErrors(m_Chunks[l], iconMap, brushEntity, brushPosition, ref totalCost);
					}
				}
			}
			bool flag2 = false;
			NativeHashMap<Entity, ErrorSeverity>.Enumerator enumerator = m_ErrorMap.GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Value == ErrorSeverity.CancelError)
				{
					flag2 = brushEntity != Entity.Null;
				}
			}
			enumerator.Dispose();
			if (flag2)
			{
				for (int m = 0; m < m_Chunks.Length; m++)
				{
					if (!AllCancelled(m_Chunks[m], ErrorSeverity.CancelError))
					{
						flag2 = false;
						break;
					}
				}
			}
			NativeHashMap<IconKey, IconValue>.Enumerator enumerator2 = iconMap.GetEnumerator();
			while (enumerator2.MoveNext())
			{
				IconKey key = enumerator2.Current.Key;
				IconValue value = enumerator2.Current.Value;
				ErrorSeverity item;
				if (value.m_Cancelled)
				{
					if (!flag2)
					{
						continue;
					}
					if (key.m_Owner != Entity.Null)
					{
						AddError(key.m_Owner, ErrorSeverity.Error);
					}
					if (key.m_Target != Entity.Null)
					{
						AddError(key.m_Target, ErrorSeverity.Error);
					}
				}
				else if (m_ErrorMap.TryGetValue(key.m_Owner, out item) && item >= ErrorSeverity.Cancel)
				{
					continue;
				}
				if (math.any(math.isnan(value.m_Bounds.min)))
				{
					m_IconCommandBuffer.Add(key.m_Owner, key.m_Prefab, value.m_Severity, IconClusterLayer.Default, (IconFlags)0, key.m_Target, isTemp: true);
					continue;
				}
				float3 location = MathUtils.Center(value.m_Bounds);
				m_IconCommandBuffer.Add(key.m_Owner, key.m_Prefab, location, value.m_Severity, IconClusterLayer.Default, (IconFlags)0, key.m_Target, isTemp: true);
			}
			enumerator2.Dispose();
			iconMap.Dispose();
		}

		private bool AllCancelled(ArchetypeChunk chunk, ErrorSeverity limit)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				if ((nativeArray2[i].m_Flags & TempFlags.Cancel) == 0 && (!m_ErrorMap.TryGetValue(nativeArray[i], out var item) || item < limit))
				{
					return false;
				}
			}
			return true;
		}

		private void CalculateCost(ArchetypeChunk chunk, ref int totalCost, ref Entity brushEntity, ref float4 brushPosition)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Brush> nativeArray3 = chunk.GetNativeArray(ref m_BrushType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				totalCost += nativeArray2[i].m_Cost;
			}
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				Brush brush = nativeArray3[j];
				brushEntity = nativeArray[j];
				brushPosition += new float4(brush.m_Position * brush.m_Strength, brush.m_Strength);
			}
		}

		private void CancelOptionalWithMoneyError(ArchetypeChunk chunk, NativeHashMap<IconKey, IconValue> iconMap, Entity brushEntity, float4 brushPosition, ref int totalCost, int costLimit)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Curve> nativeArray4 = chunk.GetNativeArray(ref m_CurveType);
			ErrorData error = new ErrorData
			{
				m_ErrorSeverity = ErrorSeverity.Error,
				m_ErrorType = ErrorType.NotEnoughMoney,
				m_Position = float.NaN
			};
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				if (nativeArray2[i].m_Cost > 0)
				{
					error.m_TempEntity = nativeArray[i];
					if (CancelOptional(error, iconMap, brushEntity, brushPosition, ref totalCost) && totalCost <= costLimit)
					{
						return;
					}
				}
			}
			for (int j = 0; j < nativeArray4.Length; j++)
			{
				if (nativeArray2[j].m_Cost > 0)
				{
					error.m_TempEntity = nativeArray[j];
					if (CancelOptional(error, iconMap, brushEntity, brushPosition, ref totalCost) && totalCost <= costLimit)
					{
						break;
					}
				}
			}
		}

		private void ProcessMoneyErrors(ArchetypeChunk chunk, NativeHashMap<IconKey, IconValue> iconMap, Entity brushEntity, float4 brushPosition, ref int totalCost)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Curve> nativeArray4 = chunk.GetNativeArray(ref m_CurveType);
			ErrorData error = new ErrorData
			{
				m_ErrorSeverity = ErrorSeverity.Error,
				m_ErrorType = ErrorType.NotEnoughMoney,
				m_Position = float.NaN
			};
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				if (nativeArray2[i].m_Cost > 0)
				{
					error.m_TempEntity = nativeArray[i];
					ProcessError(error, iconMap, brushEntity, brushPosition, ref totalCost);
				}
			}
			for (int j = 0; j < nativeArray4.Length; j++)
			{
				if (nativeArray2[j].m_Cost > 0)
				{
					error.m_TempEntity = nativeArray[j];
					ProcessError(error, iconMap, brushEntity, brushPosition, ref totalCost);
				}
			}
		}

		private void ProcessQueue(NativeQueue<ErrorData> errorQueue, NativeHashMap<IconKey, IconValue> iconMap, Entity brushEntity, float4 brushPosition, ref int totalCost)
		{
			ErrorData item;
			while (errorQueue.TryDequeue(out item))
			{
				ProcessError(item, iconMap, brushEntity, brushPosition, ref totalCost);
			}
		}

		private void ProcessError(ErrorData error, NativeHashMap<IconKey, IconValue> iconMap, Entity brushEntity, float4 brushPosition, ref int totalCost)
		{
			if (!(m_ErrorPrefabs[(int)error.m_ErrorType] != Entity.Null) || CancelOptional(error, iconMap, brushEntity, brushPosition, ref totalCost))
			{
				return;
			}
			if (error.m_ErrorSeverity >= ErrorSeverity.Cancel)
			{
				if (m_TempData.TryGetComponent(error.m_TempEntity, out var componentData))
				{
					Cancel(error, componentData, iconMap, brushEntity, brushPosition, addCancelledError: false, ref totalCost);
				}
				if (m_TempData.TryGetComponent(error.m_PermanentEntity, out componentData))
				{
					error.m_TempEntity = error.m_PermanentEntity;
					Cancel(error, componentData, iconMap, brushEntity, brushPosition, addCancelledError: false, ref totalCost);
				}
				return;
			}
			if (error.m_ErrorSeverity != ErrorSeverity.Override)
			{
				AddIcon(error, iconMap, cancelled: false);
			}
			if (error.m_TempEntity != Entity.Null && (error.m_ErrorSeverity >= ErrorSeverity.Error || (error.m_PermanentEntity == Entity.Null && error.m_ErrorSeverity == ErrorSeverity.Override)))
			{
				AddError(error.m_TempEntity, error.m_ErrorSeverity);
			}
			if (error.m_PermanentEntity != Entity.Null && error.m_ErrorSeverity >= ErrorSeverity.Override)
			{
				AddError(error.m_PermanentEntity, error.m_ErrorSeverity);
			}
		}

		private bool CancelOptional(ErrorData error, NativeHashMap<IconKey, IconValue> iconMap, Entity brushEntity, float4 brushPosition, ref int totalCost)
		{
			while (error.m_TempEntity != Entity.Null)
			{
				if (m_TempData.TryGetComponent(error.m_TempEntity, out var componentData) && (componentData.m_Flags & TempFlags.Optional) != 0)
				{
					bool flag = error.m_ErrorSeverity == ErrorSeverity.Error;
					if (flag)
					{
						ErrorData error2 = error;
						error2.m_TempEntity = error.m_PermanentEntity;
						while (error2.m_TempEntity != Entity.Null)
						{
							if (m_TempData.TryGetComponent(error2.m_TempEntity, out var componentData2) && (componentData2.m_Flags & TempFlags.Optional) != 0)
							{
								flag = false;
								Cancel(error2, componentData2, iconMap, brushEntity, brushPosition, addCancelledError: false, ref totalCost);
								break;
							}
							if (!m_OwnerData.TryGetComponent(error2.m_TempEntity, out var componentData3))
							{
								break;
							}
							error2.m_TempEntity = componentData3.m_Owner;
						}
					}
					Cancel(error, componentData, iconMap, brushEntity, brushPosition, flag, ref totalCost);
					return true;
				}
				if (!m_OwnerData.TryGetComponent(error.m_TempEntity, out var componentData4))
				{
					break;
				}
				error.m_TempEntity = componentData4.m_Owner;
			}
			return false;
		}

		private void Cancel(ErrorData error, Temp temp, NativeHashMap<IconKey, IconValue> iconMap, Entity brushEntity, float4 brushPosition, bool addCancelledError, ref int totalCost)
		{
			if (AddError(error.m_TempEntity, (error.m_ErrorSeverity == ErrorSeverity.Error || error.m_ErrorSeverity == ErrorSeverity.CancelError) ? ErrorSeverity.CancelError : ErrorSeverity.Cancel))
			{
				totalCost -= temp.m_Cost;
				temp.m_Flags |= TempFlags.Hidden | TempFlags.Cancel;
				m_CommandBuffer.SetComponent(error.m_TempEntity, temp);
			}
			if (m_SubObjects.TryGetBuffer(error.m_TempEntity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					error.m_TempEntity = bufferData[i].m_SubObject;
					if (m_TempData.TryGetComponent(error.m_TempEntity, out var componentData))
					{
						Cancel(error, componentData, iconMap, brushEntity, brushPosition, addCancelledError: false, ref totalCost);
					}
				}
			}
			if (m_SubLanes.TryGetBuffer(error.m_TempEntity, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					error.m_TempEntity = bufferData2[j].m_SubLane;
					if (m_TempData.TryGetComponent(error.m_TempEntity, out var componentData2))
					{
						Cancel(error, componentData2, iconMap, brushEntity, brushPosition, addCancelledError: false, ref totalCost);
					}
				}
			}
			if (addCancelledError && brushEntity != Entity.Null)
			{
				if (brushPosition.w != 0f)
				{
					brushPosition /= brushPosition.w;
				}
				error.m_TempEntity = brushEntity;
				error.m_Position = brushPosition.xyz;
				AddIcon(error, iconMap, cancelled: true);
			}
		}

		private void AddIcon(ErrorData error, NativeHashMap<IconKey, IconValue> iconMap, bool cancelled)
		{
			IconKey key = default(IconKey);
			key.m_Owner = error.m_TempEntity;
			key.m_Target = error.m_PermanentEntity;
			key.m_Prefab = m_ErrorPrefabs[(int)error.m_ErrorType];
			IconValue iconValue = default(IconValue);
			iconValue.m_Bounds = new Bounds3(error.m_Position, error.m_Position);
			iconValue.m_Cancelled = cancelled;
			switch (error.m_ErrorSeverity)
			{
			case ErrorSeverity.Warning:
				iconValue.m_Severity = IconPriority.Warning;
				break;
			case ErrorSeverity.Error:
				iconValue.m_Severity = IconPriority.Error;
				break;
			default:
				iconValue.m_Severity = IconPriority.Info;
				break;
			}
			if (iconMap.TryGetValue(key, out var item))
			{
				if (math.any(math.isnan(error.m_Position)))
				{
					iconValue.m_Bounds = item.m_Bounds;
				}
				else if (!math.any(math.isnan(item.m_Bounds.min)))
				{
					iconValue.m_Bounds |= item.m_Bounds;
				}
				iconValue.m_Severity = (IconPriority)math.max((int)iconValue.m_Severity, (int)item.m_Severity);
				iconMap[key] = iconValue;
			}
			else
			{
				iconMap.Add(key, iconValue);
			}
		}

		private bool AddError(Entity entity, ErrorSeverity severity)
		{
			if (m_ErrorMap.TryGetValue(entity, out var item))
			{
				if (severity > item)
				{
					m_ErrorMap[entity] = severity;
					return item < ErrorSeverity.Cancel;
				}
				return false;
			}
			m_ErrorMap.Add(entity, severity);
			return true;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Native> __Game_Common_Native_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ToolErrorData> __Game_Prefabs_ToolErrorData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Brush> __Game_Tools_Brush_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlayerMoney> __Game_City_PlayerMoney_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Native>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
			__Game_Prefabs_ToolErrorData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ToolErrorData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Tools_Brush_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Brush>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_City_PlayerMoney_RO_ComponentLookup = state.GetComponentLookup<PlayerMoney>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
		}
	}

	private ModificationEndBarrier m_ModificationBarrier;

	private ToolSystem m_ToolSystem;

	private Components m_Components;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private InstanceCountSystem m_InstanceCountSystem;

	private CitySystem m_CitySystem;

	private IconCommandSystem m_IconCommandSystem;

	private WaterSystem m_WaterSystem;

	private TerrainSystem m_TerrainSystem;

	private GroundWaterSystem m_GroundWaterSystem;

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_UpdatedAreaQuery;

	private EntityQuery m_ToolErrorPrefabQuery;

	private ChunkType m_ChunkType;

	private EntityData m_EntityData;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_Components = base.World.GetOrCreateSystemManaged<Components>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_InstanceCountSystem = base.World.GetOrCreateSystemManaged<InstanceCountSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_UpdatedQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Relative>(), ComponentType.Exclude<Moving>(), ComponentType.Exclude<Stopped>());
		m_UpdatedAreaQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<Area>(), ComponentType.Exclude<Deleted>());
		m_ToolErrorPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<NotificationIconData>(), ComponentType.ReadOnly<ToolErrorData>());
		m_ChunkType = new ChunkType(this);
		m_EntityData = new EntityData(this);
		RequireForUpdate(m_UpdatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeList<BoundsData> edgeList = new NativeList<BoundsData>(Allocator.TempJob);
		NativeList<BoundsData> objectList = new NativeList<BoundsData>(Allocator.TempJob);
		NativeQueue<ErrorData> errorQueue = new NativeQueue<ErrorData>(Allocator.TempJob);
		NativeQueue<ErrorData> errorQueue2 = new NativeQueue<ErrorData>(Allocator.TempJob);
		NativeArray<Entity> errorPrefabs = new NativeArray<Entity>(30, Allocator.TempJob);
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> nativeList = m_UpdatedQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		m_ChunkType.Update(this);
		m_EntityData.Update(this);
		BoundsListJob jobData = new BoundsListJob
		{
			m_Chunks = nativeList.AsDeferredJobArray(),
			m_ChunkType = m_ChunkType,
			m_EntityData = m_EntityData,
			m_EdgeList = edgeList,
			m_ObjectList = objectList
		};
		TerrainHeightData data = m_TerrainSystem.GetHeightData();
		Bounds3 editorCameraBounds = TerrainUtils.GetEditorCameraBounds(m_TerrainSystem, ref data);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		JobHandle deps;
		JobHandle dependencies5;
		ValidationJob jobData2 = new ValidationJob
		{
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_Chunks = nativeList.AsDeferredJobArray(),
			m_ChunkType = m_ChunkType,
			m_EntityData = m_EntityData,
			m_EdgeList = edgeList,
			m_ObjectList = objectList,
			m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
			m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies3),
			m_InstanceCounts = m_InstanceCountSystem.GetInstanceCounts(readOnly: true, out dependencies4),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_TerrainHeightData = data,
			m_worldBounds = editorCameraBounds,
			m_GroundWaterMap = m_GroundWaterSystem.GetMap(readOnly: true, out dependencies5),
			m_ErrorQueue = errorQueue.AsParallelWriter()
		};
		JobHandle job = JobUtils.CombineDependencies(dependencies, dependencies2, dependencies3, dependencies4, deps, dependencies5);
		JobHandle jobHandle = default(JobHandle);
		m_Components.m_ErrorMap = new NativeHashMap<Entity, ErrorSeverity>(32, Allocator.TempJob);
		if (!m_UpdatedAreaQuery.IsEmptyIgnoreFilter)
		{
			NativeList<AreaSearchItem> nativeList2 = new NativeList<AreaSearchItem>(Allocator.TempJob);
			CollectAreaTrianglesJob jobData3 = new CollectAreaTrianglesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NativeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Native_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_AreaTriangles = nativeList2
			};
			jobHandle = new ValidateAreaTrianglesJob
			{
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_AreaTriangles = nativeList2.AsDeferredJobArray(),
				m_EntityData = jobData2.m_EntityData,
				m_ObjectSearchTree = jobData2.m_ObjectSearchTree,
				m_NetSearchTree = jobData2.m_NetSearchTree,
				m_AreaSearchTree = jobData2.m_AreaSearchTree,
				m_WaterSurfaceData = jobData2.m_WaterSurfaceData,
				m_TerrainHeightData = jobData2.m_TerrainHeightData,
				m_ErrorQueue = errorQueue2.AsParallelWriter()
			}.Schedule(dependsOn: JobHandle.CombineDependencies(JobChunkExtensions.Schedule(jobData3, m_UpdatedAreaQuery, base.Dependency), job), list: nativeList2, innerloopBatchCount: 1);
			nativeList2.Dispose(jobHandle);
		}
		FillErrorPrefabsJob jobData4 = new FillErrorPrefabsJob
		{
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ToolErrorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ToolErrorData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ErrorPrefabs = errorPrefabs
		};
		ProcessValidationResultsJob jobData5 = new ProcessValidationResultsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BrushType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Brush_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlayerMoney = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_PlayerMoney_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_Chunks = nativeList,
			m_City = m_CitySystem.City,
			m_ErrorMap = m_Components.m_ErrorMap,
			m_ErrorPrefabs = errorPrefabs,
			m_ErrorQueue1 = errorQueue,
			m_ErrorQueue2 = errorQueue2,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		};
		base.Dependency = JobHandle.CombineDependencies(base.Dependency, outJobHandle);
		JobHandle job2 = IJobParallelForDeferExtensions.Schedule(dependsOn: JobHandle.CombineDependencies(IJobExtensions.Schedule(jobData, base.Dependency), job), jobData: jobData2, list: nativeList, innerloopBatchCount: 1);
		job2 = JobHandle.CombineDependencies(job2, jobHandle);
		JobHandle job3 = JobChunkExtensions.Schedule(jobData4, m_ToolErrorPrefabQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData5, JobHandle.CombineDependencies(job2, job3));
		edgeList.Dispose(job2);
		objectList.Dispose(job2);
		errorQueue.Dispose(jobHandle2);
		errorQueue2.Dispose(jobHandle2);
		nativeList.Dispose(jobHandle2);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(job2);
		m_NetSearchSystem.AddNetSearchTreeReader(job2);
		m_AreaSearchSystem.AddSearchTreeReader(job2);
		m_InstanceCountSystem.AddCountReader(job2);
		m_WaterSystem.AddSurfaceReader(job2);
		m_TerrainSystem.AddCPUHeightReader(job2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle2);
		m_Components.m_ErrorMapDeps = jobHandle2;
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
	public ValidationSystem()
	{
	}
}
