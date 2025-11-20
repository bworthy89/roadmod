using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Rendering;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class BatchInstanceSystem : GameSystemBase
{
	[CompilerGenerated]
	public class Groups : GameSystemBase
	{
		private struct TypeHandle
		{
			public BufferLookup<MeshBatch> __Game_Rendering_MeshBatch_RW_BufferLookup;

			public BufferLookup<FadeBatch> __Game_Rendering_FadeBatch_RW_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				__Game_Rendering_MeshBatch_RW_BufferLookup = state.GetBufferLookup<MeshBatch>();
				__Game_Rendering_FadeBatch_RW_BufferLookup = state.GetBufferLookup<FadeBatch>();
			}
		}

		private PreCullingSystem m_PreCullingSystem;

		private BatchManagerSystem m_BatchManagerSystem;

		public NativeParallelQueue<GroupActionData> m_GroupActionQueue;

		public NativeQueue<VelocityData> m_VelocityQueue;

		public NativeQueue<FadeData> m_FadeQueue;

		public JobHandle m_Dependency;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
			m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		}

		[Preserve]
		protected override void OnUpdate()
		{
			JobHandle dependencies;
			NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: false, out dependencies);
			JobHandle dependencies2;
			NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> nativeSubBatches = m_BatchManagerSystem.GetNativeSubBatches(readOnly: false, out dependencies2);
			NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.InstanceUpdater instanceUpdater = nativeBatchInstances.BeginInstanceUpdate(Allocator.TempJob);
			JobHandle dependsOn = JobHandle.CombineDependencies(base.Dependency, m_Dependency, dependencies);
			DequeueFadesJob jobData = new DequeueFadesJob
			{
				m_FadeContainer = m_PreCullingSystem.GetFadeContainer(),
				m_BatchInstances = nativeBatchInstances,
				m_VelocityQueue = m_VelocityQueue,
				m_FadeQueue = m_FadeQueue,
				m_MeshBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshBatch_RW_BufferLookup, ref base.CheckedStateRef),
				m_FadeBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_FadeBatch_RW_BufferLookup, ref base.CheckedStateRef)
			};
			GroupActionJob jobData2 = new GroupActionJob
			{
				m_MeshBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshBatch_RW_BufferLookup, ref base.CheckedStateRef),
				m_GroupActions = m_GroupActionQueue.AsReader(),
				m_BatchInstanceUpdater = instanceUpdater.AsParallel(int.MaxValue)
			};
			JobHandle jobHandle = IJobExtensions.Schedule(jobData, dependsOn);
			JobHandle jobHandle2 = IJobParallelForExtensions.Schedule(jobData2, m_GroupActionQueue.HashRange, 1, jobHandle);
			JobHandle jobHandle3 = nativeBatchInstances.EndInstanceUpdate(instanceUpdater, JobHandle.CombineDependencies(jobHandle2, dependencies2), nativeSubBatches);
			m_GroupActionQueue.Dispose(jobHandle2);
			m_VelocityQueue.Dispose(jobHandle);
			m_FadeQueue.Dispose(jobHandle);
			m_BatchManagerSystem.AddNativeBatchInstancesWriter(jobHandle3);
			m_BatchManagerSystem.AddNativeSubBatchesWriter(jobHandle3);
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
		public Groups()
		{
		}
	}

	[BurstCompile]
	private struct BatchInstanceJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[ReadOnly]
		public ComponentLookup<CullingInfo> m_CullingInfoData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Error> m_ErrorData;

		[ReadOnly]
		public ComponentLookup<Warning> m_WarningData;

		[ReadOnly]
		public ComponentLookup<Override> m_OverrideData;

		[ReadOnly]
		public ComponentLookup<Highlighted> m_HighlightedData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[NativeDisableParallelForRestriction]
		public BufferLookup<MeshBatch> m_MeshBatches;

		[NativeDisableParallelForRestriction]
		public BufferLookup<FadeBatch> m_FadeBatches;

		[ReadOnly]
		public ComponentLookup<Stopped> m_StoppedData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<Stack> m_StackData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.NetObject> m_NetObjectData;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ObjectElevationData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Marker> m_ObjectMarkerData;

		[ReadOnly]
		public ComponentLookup<Relative> m_RelativeData;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> m_UnderConstructionData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Game.Net.OutsideConnection> m_NetOutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> m_TrackLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.UtilityLane> m_UtilityLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Marker> m_NetMarkerData;

		[ReadOnly]
		public BufferLookup<CutRange> m_CutRanges;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public ComponentLookup<Block> m_ZoneBlockData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<GrowthScaleData> m_PrefabGrowthScaleData;

		[ReadOnly]
		public ComponentLookup<StackData> m_PrefabStackData;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> m_PrefabQuantityObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_PrefabMeshData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshData> m_PrefabCompositionMeshData;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshRef> m_PrefabCompositionMeshRef;

		[ReadOnly]
		public BufferLookup<SubMesh> m_PrefabSubMeshes;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_PrefabSubMeshGroups;

		[ReadOnly]
		public BufferLookup<BatchGroup> m_PrefabBatchGroups;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_MarkersVisible;

		[ReadOnly]
		public bool m_UnspawnedVisible;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public bool m_UseLodFade;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public float m_FrameTime;

		[ReadOnly]
		public UtilityTypes m_DilatedUtilityTypes;

		[ReadOnly]
		public BoundsMask m_VisibleMask;

		[ReadOnly]
		public BoundsMask m_BecameVisible;

		[ReadOnly]
		public BoundsMask m_BecameHidden;

		[ReadOnly]
		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> m_BatchInstances;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public NativeParallelQueue<GroupActionData>.Writer m_GroupActionQueue;

		public NativeQueue<FadeData>.ParallelWriter m_FadeQueue;

		public NativeQueue<VelocityData>.ParallelWriter m_VelocityQueue;

		public void Execute(int index)
		{
			PreCullingData cullingData = m_CullingData[index];
			if ((cullingData.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated | PreCullingFlags.BatchesUpdated | PreCullingFlags.FadeContainer)) != 0)
			{
				if ((cullingData.m_Flags & PreCullingFlags.NearCamera) == 0)
				{
					RemoveInstances(cullingData);
				}
				else if ((cullingData.m_Flags & PreCullingFlags.Object) != 0)
				{
					UpdateObjectInstances(cullingData);
				}
				else if ((cullingData.m_Flags & PreCullingFlags.Net) != 0)
				{
					UpdateNetInstances(cullingData);
				}
				else if ((cullingData.m_Flags & PreCullingFlags.Lane) != 0)
				{
					UpdateLaneInstances(cullingData);
				}
				else if ((cullingData.m_Flags & PreCullingFlags.Zone) != 0)
				{
					UpdateZoneInstances(cullingData);
				}
				else if ((cullingData.m_Flags & PreCullingFlags.FadeContainer) != 0)
				{
					UpdateFadeInstances(cullingData);
				}
			}
		}

		private void RemoveInstances(PreCullingData cullingData)
		{
			if (!m_MeshBatches.TryGetBuffer(cullingData.m_Entity, out var bufferData))
			{
				return;
			}
			bool flag = m_UseLodFade && (cullingData.m_Flags & (PreCullingFlags.Temp | PreCullingFlags.Zone)) == 0 && (((cullingData.m_Flags & PreCullingFlags.Deleted) != 0) ? ((cullingData.m_Flags & PreCullingFlags.Applied) == 0) : ((!m_UnspawnedVisible && m_UnspawnedData.HasComponent(cullingData.m_Entity)) || (m_CullingInfoData[cullingData.m_Entity].m_Mask & (m_VisibleMask | m_BecameHidden)) == 0));
			Entity entity = Entity.Null;
			if (flag)
			{
				if (m_TransformFrames.TryGetBuffer(cullingData.m_Entity, out var bufferData2))
				{
					entity = cullingData.m_Entity;
					UpdateFrame updateFrame = new UpdateFrame((uint)cullingData.m_UpdateFrame);
					ObjectInterpolateSystem.CalculateUpdateFrames(m_FrameIndex, m_FrameTime, updateFrame.m_Index, out var updateFrame2, out var updateFrame3, out var framePosition);
					TransformFrame transformFrame = bufferData2[(int)updateFrame2];
					TransformFrame transformFrame2 = bufferData2[(int)updateFrame3];
					float3 velocity = math.lerp(transformFrame.m_Velocity, transformFrame2.m_Velocity, framePosition);
					m_VelocityQueue.Enqueue(new VelocityData
					{
						m_Source = entity,
						m_Velocity = velocity
					});
				}
				else if (m_RelativeData.HasComponent(cullingData.m_Entity))
				{
					Owner componentData2;
					if (m_CurrentVehicleData.TryGetComponent(cullingData.m_Entity, out var componentData))
					{
						entity = componentData.m_Vehicle;
					}
					else if (m_OwnerData.TryGetComponent(cullingData.m_Entity, out componentData2))
					{
						entity = componentData2.m_Owner;
						while (m_RelativeData.HasComponent(entity))
						{
							entity = m_OwnerData[entity].m_Owner;
						}
					}
				}
			}
			RemoveInstances(bufferData, flag, entity);
			bufferData.Clear();
		}

		private unsafe void UpdateObjectInstances(PreCullingData cullingData)
		{
			if (!m_MeshBatches.TryGetBuffer(cullingData.m_Entity, out var bufferData))
			{
				return;
			}
			MeshLayer meshLayer = MeshLayer.Default;
			bool flag = (cullingData.m_Flags & PreCullingFlags.Temp) == 0 && (cullingData.m_Flags & (PreCullingFlags.Created | PreCullingFlags.Applied)) != PreCullingFlags.Applied;
			bool flag2 = m_InterpolatedTransformData.HasComponent(cullingData.m_Entity) || m_StoppedData.HasComponent(cullingData.m_Entity);
			bool flag3 = m_ObjectMarkerData.HasComponent(cullingData.m_Entity);
			bool flag4 = false;
			SubMeshFlags subMeshFlags = SubMeshFlags.DefaultMissingMesh | SubMeshFlags.HasTransform;
			subMeshFlags = (SubMeshFlags)((uint)subMeshFlags | (uint)(m_LeftHandTraffic ? 65536 : 131072));
			if (m_ErrorData.HasComponent(cullingData.m_Entity) || m_WarningData.HasComponent(cullingData.m_Entity) || m_OverrideData.HasComponent(cullingData.m_Entity) || m_HighlightedData.HasComponent(cullingData.m_Entity))
			{
				meshLayer |= MeshLayer.Outline;
				subMeshFlags |= SubMeshFlags.OutlineOnly;
				flag4 = true;
			}
			int oldBatchCount = bufferData.Length;
			MeshBatch* ptr = stackalloc MeshBatch[oldBatchCount];
			UnsafeUtility.MemCpy(ptr, bufferData.GetUnsafeReadOnlyPtr(), oldBatchCount * UnsafeUtility.SizeOf<MeshBatch>());
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			bufferData.Clear();
			bool hasMeshMatches = false;
			if (flag && m_BecameVisible != 0)
			{
				flag = (m_CullingInfoData[cullingData.m_Entity].m_Mask & m_VisibleMask) != m_BecameVisible;
			}
			if (m_PrefabSubMeshes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData2))
			{
				MeshLayer meshLayer2 = meshLayer;
				SubMeshFlags subMeshFlags2 = subMeshFlags;
				int3 tileCounts = 0;
				if ((cullingData.m_Flags & PreCullingFlags.Temp) != 0)
				{
					Temp temp = m_TempData[cullingData.m_Entity];
					if ((temp.m_Flags & TempFlags.Hidden) != 0)
					{
						goto IL_0627;
					}
					if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent | TempFlags.SubDetail)) != 0)
					{
						meshLayer2 |= MeshLayer.Outline;
						subMeshFlags2 |= SubMeshFlags.OutlineOnly;
					}
					flag4 = (temp.m_Flags & TempFlags.Essential) != 0;
				}
				Game.Objects.Elevation componentData;
				if (flag3)
				{
					meshLayer2 &= ~MeshLayer.Default;
					meshLayer2 |= MeshLayer.Marker;
				}
				else if (flag2)
				{
					meshLayer2 &= ~MeshLayer.Default;
					meshLayer2 |= MeshLayer.Moving;
				}
				else if (m_ObjectElevationData.TryGetComponent(cullingData.m_Entity, out componentData) && componentData.m_Elevation < 0f)
				{
					meshLayer2 &= ~MeshLayer.Default;
					meshLayer2 |= MeshLayer.Tunnel;
				}
				float3 scale;
				if (m_TreeData.TryGetComponent(cullingData.m_Entity, out var componentData2))
				{
					subMeshFlags2 = ((!m_PrefabGrowthScaleData.TryGetComponent(prefabRef.m_Prefab, out var componentData3)) ? (subMeshFlags2 | SubMeshFlags.RequireAdult) : (subMeshFlags2 | BatchDataHelpers.CalculateTreeSubMeshData(componentData2, componentData3, out scale)));
				}
				if (m_StackData.TryGetComponent(cullingData.m_Entity, out var componentData4) && m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out var componentData5))
				{
					subMeshFlags2 |= BatchDataHelpers.CalculateStackSubMeshData(componentData4, componentData5, out tileCounts, out scale, out var _);
				}
				if (m_NetObjectData.TryGetComponent(cullingData.m_Entity, out var componentData6))
				{
					subMeshFlags2 |= BatchDataHelpers.CalculateNetObjectSubMeshData(componentData6);
				}
				if (m_QuantityData.TryGetComponent(cullingData.m_Entity, out var componentData7))
				{
					subMeshFlags2 = ((!m_PrefabQuantityObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData8)) ? (subMeshFlags2 | BatchDataHelpers.CalculateQuantitySubMeshData(componentData7, default(QuantityObjectData), m_EditorMode)) : (subMeshFlags2 | BatchDataHelpers.CalculateQuantitySubMeshData(componentData7, componentData8, m_EditorMode)));
				}
				if (!m_UnderConstructionData.TryGetComponent(cullingData.m_Entity, out var componentData9) || !(componentData9.m_NewPrefab == Entity.Null))
				{
					if (m_DestroyedData.TryGetComponent(cullingData.m_Entity, out var componentData10) && m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData11) && (componentData11.m_Flags & (Game.Objects.GeometryFlags.Physical | Game.Objects.GeometryFlags.HasLot)) == (Game.Objects.GeometryFlags.Physical | Game.Objects.GeometryFlags.HasLot))
					{
						if (componentData10.m_Cleared >= 0f)
						{
							goto IL_0627;
						}
						meshLayer2 &= ~MeshLayer.Outline;
					}
					DynamicBuffer<MeshGroup> bufferData3 = default(DynamicBuffer<MeshGroup>);
					int num = 1;
					if (m_PrefabSubMeshGroups.TryGetBuffer(prefabRef.m_Prefab, out var bufferData4) && m_MeshGroups.TryGetBuffer(cullingData.m_Entity, out bufferData3))
					{
						num = bufferData3.Length;
					}
					SubMeshGroup subMeshGroup = default(SubMeshGroup);
					for (int i = 0; i < num; i++)
					{
						if (bufferData4.IsCreated)
						{
							CollectionUtils.TryGet(bufferData3, i, out var value);
							subMeshGroup = bufferData4[value.m_SubMeshGroup];
						}
						else
						{
							subMeshGroup.m_SubMeshRange = new int2(0, bufferData2.Length);
						}
						for (int j = subMeshGroup.m_SubMeshRange.x; j < subMeshGroup.m_SubMeshRange.y; j++)
						{
							SubMesh subMesh = bufferData2[j];
							if ((subMesh.m_Flags & subMeshFlags2) == subMesh.m_Flags)
							{
								MeshData meshData = m_PrefabMeshData[subMesh.m_SubMesh];
								DynamicBuffer<BatchGroup> batchGroups = m_PrefabBatchGroups[subMesh.m_SubMesh];
								MeshLayer meshLayer3 = meshLayer2;
								if ((meshData.m_DefaultLayers != 0 && (meshLayer2 & (MeshLayer.Moving | MeshLayer.Marker)) == 0) || (meshData.m_DefaultLayers & (MeshLayer.Pipeline | MeshLayer.SubPipeline)) != 0)
								{
									meshLayer3 &= ~(MeshLayer.Default | MeshLayer.Moving | MeshLayer.Tunnel | MeshLayer.Marker);
									m_OwnerData.TryGetComponent(cullingData.m_Entity, out var componentData12);
									meshLayer3 |= Game.Net.SearchSystem.GetLayers(componentData12, default(Game.Net.UtilityLane), meshData.m_DefaultLayers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData);
								}
								if ((meshLayer3 & MeshLayer.Outline) != 0 && (meshData.m_State & MeshFlags.Decal) != 0 && !flag4)
								{
									meshLayer3 &= ~MeshLayer.Outline;
								}
								if ((subMesh.m_Flags & SubMeshFlags.OutlineOnly) != 0)
								{
									meshLayer3 &= MeshLayer.Outline;
								}
								int falseValue = 1;
								falseValue = math.select(falseValue, tileCounts.x, (subMesh.m_Flags & SubMeshFlags.IsStackStart) != 0);
								falseValue = math.select(falseValue, tileCounts.y, (subMesh.m_Flags & SubMeshFlags.IsStackMiddle) != 0);
								falseValue = math.select(falseValue, tileCounts.z, (subMesh.m_Flags & SubMeshFlags.IsStackEnd) != 0);
								if (falseValue >= 1)
								{
									AddInstance(ptr, ref oldBatchCount, bufferData, batchGroups, meshLayer3, MeshType.Object, 0, flag, cullingData.m_Entity, i, j - subMeshGroup.m_SubMeshRange.x, falseValue, ref hasMeshMatches);
								}
							}
						}
					}
				}
			}
			goto IL_0627;
			IL_0627:
			bufferData.TrimExcess();
			RemoveInstances(ptr, oldBatchCount, bufferData, flag, hasMeshMatches, cullingData.m_Entity);
		}

		private unsafe void UpdateNetInstances(PreCullingData cullingData)
		{
			if (!m_MeshBatches.TryGetBuffer(cullingData.m_Entity, out var bufferData))
			{
				return;
			}
			MeshLayer meshLayer = MeshLayer.Default;
			bool flag = (cullingData.m_Flags & PreCullingFlags.Temp) == 0 && (cullingData.m_Flags & (PreCullingFlags.Created | PreCullingFlags.Applied)) != PreCullingFlags.Applied;
			bool flag2 = m_NetOutsideConnectionData.HasComponent(cullingData.m_Entity);
			bool flag3 = m_NetMarkerData.HasComponent(cullingData.m_Entity);
			if (m_ErrorData.HasComponent(cullingData.m_Entity) || m_WarningData.HasComponent(cullingData.m_Entity) || m_HighlightedData.HasComponent(cullingData.m_Entity))
			{
				meshLayer |= MeshLayer.Outline;
			}
			int oldBatchCount = bufferData.Length;
			MeshBatch* ptr = stackalloc MeshBatch[oldBatchCount];
			UnsafeUtility.MemCpy(ptr, bufferData.GetUnsafeReadOnlyPtr(), oldBatchCount * UnsafeUtility.SizeOf<MeshBatch>());
			bufferData.Clear();
			MeshLayer meshLayer2 = meshLayer;
			bool flag4 = false;
			bool hasMeshMatches = false;
			if (flag && m_BecameVisible != 0)
			{
				flag = (m_CullingInfoData[cullingData.m_Entity].m_Mask & m_VisibleMask) != m_BecameVisible;
			}
			if ((cullingData.m_Flags & PreCullingFlags.Temp) != 0)
			{
				Temp temp = m_TempData[cullingData.m_Entity];
				if ((temp.m_Flags & TempFlags.Hidden) != 0)
				{
					goto IL_0483;
				}
				if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent | TempFlags.SubDetail)) != 0)
				{
					if ((temp.m_Flags & TempFlags.SubDetail) != 0)
					{
						flag4 = true;
					}
					else
					{
						meshLayer2 |= MeshLayer.Outline;
					}
				}
			}
			if (flag3)
			{
				meshLayer2 &= ~MeshLayer.Default;
				meshLayer2 |= MeshLayer.Marker;
			}
			Orphan componentData4;
			if (m_CompositionData.TryGetComponent(cullingData.m_Entity, out var componentData))
			{
				Edge edge = m_EdgeData[cullingData.m_Entity];
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[cullingData.m_Entity];
				StartNodeGeometry startNodeGeometry = m_StartNodeGeometryData[cullingData.m_Entity];
				EndNodeGeometry endNodeGeometry = m_EndNodeGeometryData[cullingData.m_Entity];
				PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
				NetGeometryData netGeometryData = m_PrefabNetGeometryData[prefabRef.m_Prefab];
				if (math.any(edgeGeometry.m_Start.m_Length + edgeGeometry.m_End.m_Length > 0.1f))
				{
					UpdateNetInstances(ptr, ref oldBatchCount, bufferData, cullingData.m_Entity, componentData.m_Edge, NetSubMesh.Edge, meshLayer2, flag, ref hasMeshMatches);
				}
				if (math.any(startNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(startNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[edge.m_Start];
					NetGeometryData netGeometryData2 = m_PrefabNetGeometryData[prefabRef2.m_Prefab];
					NetSubMesh subMesh = (((netGeometryData.m_MergeLayers & netGeometryData2.m_MergeLayers) != Layer.None) ? NetSubMesh.StartNode : NetSubMesh.SubStartNode);
					MeshLayer meshLayer3 = meshLayer2;
					if (flag4 && m_TempData.TryGetComponent(edge.m_Start, out var componentData2) && ((componentData2.m_Flags & (TempFlags.Upgrade | TempFlags.Parent)) == (TempFlags.Upgrade | TempFlags.Parent) || (componentData2.m_Flags & TempFlags.Select) != 0))
					{
						meshLayer3 |= MeshLayer.Outline;
					}
					UpdateNetInstances(ptr, ref oldBatchCount, bufferData, cullingData.m_Entity, componentData.m_StartNode, subMesh, meshLayer3, flag, ref hasMeshMatches);
				}
				if (math.any(endNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(endNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
				{
					PrefabRef prefabRef3 = m_PrefabRefData[edge.m_End];
					NetGeometryData netGeometryData3 = m_PrefabNetGeometryData[prefabRef3.m_Prefab];
					NetSubMesh subMesh2 = (((netGeometryData.m_MergeLayers & netGeometryData3.m_MergeLayers) != Layer.None) ? NetSubMesh.EndNode : NetSubMesh.SubEndNode);
					MeshLayer meshLayer4 = meshLayer2;
					if (flag4 && m_TempData.TryGetComponent(edge.m_End, out var componentData3) && ((componentData3.m_Flags & (TempFlags.Upgrade | TempFlags.Parent)) == (TempFlags.Upgrade | TempFlags.Parent) || (componentData3.m_Flags & TempFlags.Select) != 0))
					{
						meshLayer4 |= MeshLayer.Outline;
					}
					UpdateNetInstances(ptr, ref oldBatchCount, bufferData, cullingData.m_Entity, componentData.m_EndNode, subMesh2, meshLayer4, flag, ref hasMeshMatches);
				}
			}
			else if (!flag2 && m_OrphanData.TryGetComponent(cullingData.m_Entity, out componentData4))
			{
				UpdateNetInstances(ptr, ref oldBatchCount, bufferData, cullingData.m_Entity, componentData4.m_Composition, NetSubMesh.Orphan1, meshLayer2, flag, ref hasMeshMatches);
				UpdateNetInstances(ptr, ref oldBatchCount, bufferData, cullingData.m_Entity, componentData4.m_Composition, NetSubMesh.Orphan2, meshLayer2, flag, ref hasMeshMatches);
			}
			goto IL_0483;
			IL_0483:
			bufferData.TrimExcess();
			RemoveInstances(ptr, oldBatchCount, bufferData, flag, hasMeshMatches, cullingData.m_Entity);
		}

		private unsafe void UpdateNetInstances(MeshBatch* oldBatches, ref int oldBatchCount, DynamicBuffer<MeshBatch> meshBatches, Entity entity, Entity composition, NetSubMesh subMesh, MeshLayer layers, bool fadeIn, ref bool hasMeshMatches)
		{
			NetCompositionMeshRef netCompositionMeshRef = m_PrefabCompositionMeshRef[composition];
			if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef.m_Mesh, out var componentData))
			{
				DynamicBuffer<BatchGroup> batchGroups = m_PrefabBatchGroups[netCompositionMeshRef.m_Mesh];
				MeshLayer meshLayer = layers;
				if (componentData.m_DefaultLayers != 0 && (layers & MeshLayer.Marker) == 0)
				{
					meshLayer &= ~MeshLayer.Default;
					meshLayer |= componentData.m_DefaultLayers;
				}
				subMesh = (netCompositionMeshRef.m_Rotate ? NetSubMesh.RotatedEdge : subMesh);
				AddInstance(oldBatches, ref oldBatchCount, meshBatches, batchGroups, meshLayer, MeshType.Net, 0, fadeIn, entity, 0, (int)subMesh, 1, ref hasMeshMatches);
			}
		}

		private unsafe void UpdateLaneInstances(PreCullingData cullingData)
		{
			if (!m_MeshBatches.TryGetBuffer(cullingData.m_Entity, out var bufferData))
			{
				return;
			}
			MeshLayer meshLayer = MeshLayer.Default;
			bool flag = (cullingData.m_Flags & PreCullingFlags.Temp) == 0 && (cullingData.m_Flags & (PreCullingFlags.Created | PreCullingFlags.Applied)) != PreCullingFlags.Applied;
			bool flag2 = false;
			if (m_ErrorData.HasComponent(cullingData.m_Entity) || m_WarningData.HasComponent(cullingData.m_Entity) || m_HighlightedData.HasComponent(cullingData.m_Entity))
			{
				meshLayer |= MeshLayer.Outline;
				flag2 = true;
			}
			SubMeshFlags subMeshFlags = ((!m_EditorMode && !m_MarkersVisible) ? SubMeshFlags.RequireEditor : ((SubMeshFlags)0u));
			subMeshFlags = (SubMeshFlags)((uint)subMeshFlags | (uint)(m_LeftHandTraffic ? 131072 : 65536));
			int oldBatchCount = bufferData.Length;
			MeshBatch* ptr = stackalloc MeshBatch[oldBatchCount];
			UnsafeUtility.MemCpy(ptr, bufferData.GetUnsafeReadOnlyPtr(), oldBatchCount * UnsafeUtility.SizeOf<MeshBatch>());
			Curve curve = m_CurveData[cullingData.m_Entity];
			PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
			bufferData.Clear();
			bool hasMeshMatches = false;
			if (flag && m_BecameVisible != 0)
			{
				flag = (m_CullingInfoData[cullingData.m_Entity].m_Mask & m_VisibleMask) != m_BecameVisible;
			}
			if (curve.m_Length > 0.1f && m_PrefabSubMeshes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData2))
			{
				MeshLayer meshLayer2 = meshLayer;
				SubMeshFlags subMeshFlags2 = subMeshFlags;
				if ((cullingData.m_Flags & PreCullingFlags.Temp) != 0)
				{
					Temp temp = m_TempData[cullingData.m_Entity];
					if ((temp.m_Flags & TempFlags.Hidden) != 0)
					{
						goto IL_052b;
					}
					if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent | TempFlags.SubDetail)) != 0)
					{
						meshLayer2 |= MeshLayer.Outline;
					}
					flag2 = (temp.m_Flags & TempFlags.Essential) != 0;
				}
				if (m_OwnerData.TryGetComponent(cullingData.m_Entity, out var componentData) && IsNetOwnerTunnel(componentData))
				{
					meshLayer2 &= ~MeshLayer.Default;
					meshLayer2 |= MeshLayer.Tunnel;
				}
				if (m_PedestrianLaneData.TryGetComponent(cullingData.m_Entity, out var componentData2) && (componentData2.m_Flags & PedestrianLaneFlags.Unsafe) != 0)
				{
					subMeshFlags2 |= SubMeshFlags.RequireSafe;
				}
				if (m_CarLaneData.TryGetComponent(cullingData.m_Entity, out var componentData3))
				{
					if ((componentData3.m_Flags & CarLaneFlags.Unsafe) != 0)
					{
						subMeshFlags2 |= SubMeshFlags.RequireSafe;
					}
					if ((componentData3.m_Flags & CarLaneFlags.LevelCrossing) == 0)
					{
						subMeshFlags2 |= SubMeshFlags.RequireLevelCrossing;
					}
				}
				if (m_TrackLaneData.TryGetComponent(cullingData.m_Entity, out var componentData4))
				{
					if ((componentData4.m_Flags & TrackLaneFlags.LevelCrossing) == 0)
					{
						subMeshFlags2 |= SubMeshFlags.RequireLevelCrossing;
					}
					subMeshFlags2 = (SubMeshFlags)((uint)subMeshFlags2 | (uint)(((componentData4.m_Flags & (TrackLaneFlags.Switch | TrackLaneFlags.DiamondCrossing)) == 0) ? 4096 : 2048));
				}
				int x = 256;
				if (m_UtilityLaneData.TryGetComponent(cullingData.m_Entity, out var componentData5) && m_DilatedUtilityTypes != UtilityTypes.None && m_PrefabUtilityLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData6) && (componentData6.m_UtilityTypes & m_DilatedUtilityTypes) != UtilityTypes.None)
				{
					x = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(componentData6.m_VisualCapacity)));
				}
				for (int i = 0; i < bufferData2.Length; i++)
				{
					SubMesh subMesh = bufferData2[i];
					if ((subMesh.m_Flags & subMeshFlags2) != 0)
					{
						continue;
					}
					MeshData meshData = m_PrefabMeshData[subMesh.m_SubMesh];
					DynamicBuffer<BatchGroup> batchGroups = m_PrefabBatchGroups[subMesh.m_SubMesh];
					MeshLayer meshLayer3 = meshLayer2;
					if ((subMesh.m_Flags & SubMeshFlags.RequireEditor) != 0)
					{
						meshLayer3 &= ~(MeshLayer.Default | MeshLayer.Tunnel);
						meshLayer3 |= MeshLayer.Marker;
					}
					if ((meshData.m_DefaultLayers != 0 && (meshLayer3 & MeshLayer.Marker) == 0) || (meshData.m_DefaultLayers & (MeshLayer.Pipeline | MeshLayer.SubPipeline)) != 0)
					{
						meshLayer3 &= ~(MeshLayer.Default | MeshLayer.Tunnel | MeshLayer.Marker);
						meshLayer3 |= Game.Net.SearchSystem.GetLayers(componentData, componentData5, meshData.m_DefaultLayers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData);
					}
					float length = MathUtils.Size(meshData.m_Bounds.z);
					bool geometryTiling = (meshData.m_State & MeshFlags.Tiling) != 0;
					int num = 0;
					int clipCount;
					if (m_CutRanges.TryGetBuffer(cullingData.m_Entity, out var bufferData3))
					{
						float num2 = 0f;
						for (int j = 0; j <= bufferData3.Length; j++)
						{
							float num3;
							float num4;
							if (j < bufferData3.Length)
							{
								CutRange cutRange = bufferData3[j];
								num3 = cutRange.m_CurveDelta.min;
								num4 = cutRange.m_CurveDelta.max;
							}
							else
							{
								num3 = 1f;
								num4 = 1f;
							}
							if (num3 >= num2)
							{
								Curve curve2 = new Curve
								{
									m_Length = curve.m_Length * (num3 - num2)
								};
								if (curve2.m_Length > 0.1f)
								{
									num += BatchDataHelpers.GetTileCount(curve2, length, meshData.m_TilingCount, geometryTiling, out clipCount);
								}
							}
							num2 = num4;
						}
					}
					else
					{
						num = BatchDataHelpers.GetTileCount(curve, length, meshData.m_TilingCount, geometryTiling, out clipCount);
					}
					if (num >= 1)
					{
						if ((meshLayer3 & MeshLayer.Outline) != 0 && (meshData.m_State & MeshFlags.Decal) != 0 && !flag2)
						{
							meshLayer3 &= ~MeshLayer.Outline;
						}
						int num5 = math.min(x, meshData.m_MinLod);
						AddInstance(ptr, ref oldBatchCount, bufferData, batchGroups, meshLayer3, MeshType.Lane, (ushort)num5, flag, cullingData.m_Entity, 0, i, num, ref hasMeshMatches);
					}
				}
			}
			goto IL_052b;
			IL_052b:
			bufferData.TrimExcess();
			RemoveInstances(ptr, oldBatchCount, bufferData, flag, hasMeshMatches, cullingData.m_Entity);
		}

		private unsafe void UpdateZoneInstances(PreCullingData cullingData)
		{
			if (m_MeshBatches.TryGetBuffer(cullingData.m_Entity, out var bufferData))
			{
				int oldBatchCount = bufferData.Length;
				MeshBatch* ptr = stackalloc MeshBatch[oldBatchCount];
				UnsafeUtility.MemCpy(ptr, bufferData.GetUnsafeReadOnlyPtr(), oldBatchCount * UnsafeUtility.SizeOf<MeshBatch>());
				Block block = m_ZoneBlockData[cullingData.m_Entity];
				PrefabRef prefabRef = m_PrefabRefData[cullingData.m_Entity];
				bufferData.Clear();
				bool hasMeshMatches = false;
				if ((cullingData.m_Flags & PreCullingFlags.Temp) == 0 || (m_TempData[cullingData.m_Entity].m_Flags & TempFlags.Hidden) == 0)
				{
					ushort partition = (ushort)math.clamp(block.m_Size.x * block.m_Size.y - 1 >> 4, 0, 3);
					DynamicBuffer<BatchGroup> batchGroups = m_PrefabBatchGroups[prefabRef.m_Prefab];
					AddInstance(ptr, ref oldBatchCount, bufferData, batchGroups, MeshLayer.Default, MeshType.Zone, partition, fadeIn: false, cullingData.m_Entity, 0, 0, 1, ref hasMeshMatches);
				}
				bufferData.TrimExcess();
				RemoveInstances(ptr, oldBatchCount, bufferData, fadeOut: false, hasMeshMatches, cullingData.m_Entity);
			}
		}

		private void UpdateFadeInstances(PreCullingData cullingData)
		{
			DynamicBuffer<MeshBatch> dynamicBuffer = m_MeshBatches[cullingData.m_Entity];
			DynamicBuffer<FadeBatch> dynamicBuffer2 = m_FadeBatches[cullingData.m_Entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				MeshBatch meshBatch = dynamicBuffer[i];
				if (m_BatchInstances.GetCullingData(meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex).lodFade.z == 0 || !m_UseLodFade)
				{
					m_GroupActionQueue.Enqueue(new GroupActionData
					{
						m_GroupIndex = meshBatch.m_GroupIndex,
						m_RemoveInstanceIndex = meshBatch.m_InstanceIndex
					});
					dynamicBuffer.RemoveAtSwapBack(i);
					dynamicBuffer2.RemoveAtSwapBack(i);
					i--;
				}
			}
		}

		private bool IsNetOwnerTunnel(Owner owner)
		{
			if (m_NetElevationData.TryGetComponent(owner.m_Owner, out var componentData) && math.cmin(componentData.m_Elevation) < 0f)
			{
				return true;
			}
			if (m_ConnectedEdges.TryGetBuffer(owner.m_Owner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					ConnectedEdge connectedEdge = bufferData[i];
					if (m_NetElevationData.TryGetComponent(connectedEdge.m_Edge, out componentData) && math.cmin(componentData.m_Elevation) < 0f)
					{
						return true;
					}
				}
			}
			return false;
		}

		private unsafe void AddInstance(MeshBatch* oldBatches, ref int oldBatchCount, DynamicBuffer<MeshBatch> meshBatches, DynamicBuffer<BatchGroup> batchGroups, MeshLayer layers, MeshType type, ushort partition, bool fadeIn, Entity entity, int meshGroupIndex, int meshIndex, int tileCount, ref bool hasMeshMatches)
		{
			for (int i = 0; i < batchGroups.Length; i++)
			{
				BatchGroup batchGroup = batchGroups[i];
				if ((batchGroup.m_Layer & layers) == 0 || batchGroup.m_Type != type || batchGroup.m_Partition != partition)
				{
					continue;
				}
				for (int j = 0; j < tileCount; j++)
				{
					bool flag = fadeIn;
					int num = 0;
					while (true)
					{
						if (num < oldBatchCount)
						{
							MeshBatch elem = oldBatches[num];
							bool flag2 = elem.m_MeshGroup == meshGroupIndex && elem.m_MeshIndex == meshIndex && elem.m_TileIndex == j;
							flag = flag && !flag2;
							if (flag2 && elem.m_GroupIndex == batchGroup.m_GroupIndex)
							{
								meshBatches.Add(elem);
								oldBatches[num] = oldBatches[--oldBatchCount];
								break;
							}
							hasMeshMatches |= flag2;
							num++;
							continue;
						}
						if (m_BecameVisible != 0)
						{
							flag &= (m_BecameVisible & CommonUtils.GetBoundsMask(batchGroup.m_Layer)) == 0;
						}
						InstanceData addInstanceData = new InstanceData
						{
							m_Entity = entity,
							m_MeshGroup = (byte)meshGroupIndex,
							m_MeshIndex = (byte)meshIndex,
							m_TileIndex = (byte)j
						};
						m_GroupActionQueue.Enqueue(new GroupActionData
						{
							m_GroupIndex = batchGroup.m_GroupIndex,
							m_RemoveInstanceIndex = int.MaxValue,
							m_MergeIndex = batchGroup.m_MergeIndex,
							m_AddInstanceData = addInstanceData,
							m_FadeIn = flag
						});
						meshBatches.Add(new MeshBatch
						{
							m_GroupIndex = batchGroup.m_GroupIndex,
							m_InstanceIndex = -1,
							m_MeshGroup = addInstanceData.m_MeshGroup,
							m_MeshIndex = addInstanceData.m_MeshIndex,
							m_TileIndex = addInstanceData.m_TileIndex
						});
						break;
					}
				}
			}
		}

		private unsafe void RemoveInstances(MeshBatch* oldBatches, int oldBatchCount, DynamicBuffer<MeshBatch> meshBatches, bool fadeOut, bool hasMeshMatches, Entity entity)
		{
			for (int i = 0; i < oldBatchCount; i++)
			{
				MeshBatch meshBatch = oldBatches[i];
				if (meshBatch.m_GroupIndex == -1)
				{
					continue;
				}
				bool flag = fadeOut && m_UseLodFade;
				if (flag && hasMeshMatches)
				{
					for (int j = 0; j < meshBatches.Length; j++)
					{
						MeshBatch meshBatch2 = meshBatches[j];
						if (meshBatch.m_MeshGroup == meshBatch2.m_MeshGroup && meshBatch.m_MeshIndex == meshBatch2.m_MeshIndex && meshBatch.m_TileIndex == meshBatch2.m_TileIndex)
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					m_FadeQueue.Enqueue(new FadeData
					{
						m_Source = Entity.Null,
						m_GroupIndex = meshBatch.m_GroupIndex,
						m_InstanceIndex = meshBatch.m_InstanceIndex
					});
				}
				else
				{
					m_GroupActionQueue.Enqueue(new GroupActionData
					{
						m_GroupIndex = meshBatch.m_GroupIndex,
						m_RemoveInstanceIndex = meshBatch.m_InstanceIndex
					});
				}
			}
		}

		private void RemoveInstances(DynamicBuffer<MeshBatch> meshBatches, bool fadeOut, Entity entity)
		{
			for (int i = 0; i < meshBatches.Length; i++)
			{
				MeshBatch meshBatch = meshBatches[i];
				if (meshBatch.m_GroupIndex != -1)
				{
					if (fadeOut)
					{
						m_FadeQueue.Enqueue(new FadeData
						{
							m_Source = entity,
							m_GroupIndex = meshBatch.m_GroupIndex,
							m_InstanceIndex = meshBatch.m_InstanceIndex
						});
					}
					else
					{
						m_GroupActionQueue.Enqueue(new GroupActionData
						{
							m_GroupIndex = meshBatch.m_GroupIndex,
							m_RemoveInstanceIndex = meshBatch.m_InstanceIndex
						});
					}
				}
			}
		}
	}

	public struct GroupActionData : IComparable<GroupActionData>
	{
		public int m_GroupIndex;

		public int m_RemoveInstanceIndex;

		public int m_MergeIndex;

		public InstanceData m_AddInstanceData;

		public bool m_FadeIn;

		public int CompareTo(GroupActionData other)
		{
			return math.select(other.m_RemoveInstanceIndex - m_RemoveInstanceIndex, m_GroupIndex - other.m_GroupIndex, m_GroupIndex != other.m_GroupIndex);
		}

		public override int GetHashCode()
		{
			return m_GroupIndex;
		}
	}

	public struct VelocityData
	{
		public Entity m_Source;

		public float3 m_Velocity;
	}

	public struct FadeData
	{
		public Entity m_Source;

		public int m_GroupIndex;

		public int m_InstanceIndex;
	}

	[BurstCompile]
	private struct DequeueFadesJob : IJob
	{
		[ReadOnly]
		public Entity m_FadeContainer;

		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> m_BatchInstances;

		public NativeQueue<VelocityData> m_VelocityQueue;

		public NativeQueue<FadeData> m_FadeQueue;

		public BufferLookup<MeshBatch> m_MeshBatches;

		public BufferLookup<FadeBatch> m_FadeBatches;

		public void Execute()
		{
			NativeArray<VelocityData> nativeArray = default(NativeArray<VelocityData>);
			if (!m_VelocityQueue.IsEmpty())
			{
				nativeArray = m_VelocityQueue.ToArray(Allocator.Temp);
			}
			FadeData item;
			while (m_FadeQueue.TryDequeue(out item))
			{
				ref InstanceData reference = ref m_BatchInstances.AccessInstanceData(item.m_GroupIndex, item.m_InstanceIndex);
				ref CullingData reference2 = ref m_BatchInstances.AccessCullingData(item.m_GroupIndex, item.m_InstanceIndex);
				reference.m_Entity = m_FadeContainer;
				reference2.isFading = true;
				DynamicBuffer<MeshBatch> dynamicBuffer = m_MeshBatches[m_FadeContainer];
				DynamicBuffer<FadeBatch> dynamicBuffer2 = m_FadeBatches[m_FadeContainer];
				float3 velocity = default(float3);
				if (item.m_Source != Entity.Null && nativeArray.IsCreated)
				{
					for (int i = 0; i < nativeArray.Length; i++)
					{
						VelocityData velocityData = nativeArray[i];
						if (velocityData.m_Source == item.m_Source)
						{
							velocity = velocityData.m_Velocity;
							break;
						}
					}
				}
				dynamicBuffer.Add(new MeshBatch
				{
					m_GroupIndex = item.m_GroupIndex,
					m_InstanceIndex = item.m_InstanceIndex,
					m_MeshGroup = byte.MaxValue,
					m_MeshIndex = byte.MaxValue,
					m_TileIndex = byte.MaxValue
				});
				dynamicBuffer2.Add(new FadeBatch
				{
					m_Source = item.m_Source,
					m_Velocity = velocity
				});
			}
			if (nativeArray.IsCreated)
			{
				nativeArray.Dispose();
			}
		}
	}

	[BurstCompile]
	private struct GroupActionJob : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public BufferLookup<MeshBatch> m_MeshBatches;

		public NativeParallelQueue<GroupActionData>.Reader m_GroupActions;

		[NativeDisableParallelForRestriction]
		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.ParallelInstanceUpdater m_BatchInstanceUpdater;

		public void Execute(int index)
		{
			NativeArray<GroupActionData> array = m_GroupActions.ToArray(index, Allocator.Temp);
			if (array.Length >= 2)
			{
				array.Sort();
			}
			int num = -1;
			GroupInstanceUpdater<CullingData, GroupData, BatchData, InstanceData> groupInstanceUpdater = default(GroupInstanceUpdater<CullingData, GroupData, BatchData, InstanceData>);
			for (int i = 0; i < array.Length; i++)
			{
				GroupActionData groupActionData = array[i];
				if (groupActionData.m_GroupIndex != num)
				{
					if (num != -1)
					{
						m_BatchInstanceUpdater.EndGroup(groupInstanceUpdater);
					}
					groupInstanceUpdater = m_BatchInstanceUpdater.BeginGroup(groupActionData.m_GroupIndex);
					num = groupActionData.m_GroupIndex;
				}
				if (groupActionData.m_RemoveInstanceIndex != int.MaxValue)
				{
					InstanceData instanceData = groupInstanceUpdater.RemoveInstance(groupActionData.m_RemoveInstanceIndex);
					if (!(instanceData.m_Entity != Entity.Null))
					{
						continue;
					}
					DynamicBuffer<MeshBatch> dynamicBuffer = m_MeshBatches[instanceData.m_Entity];
					int instanceCount = groupInstanceUpdater.GetInstanceCount();
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						MeshBatch value = dynamicBuffer[j];
						if (value.m_GroupIndex == groupActionData.m_GroupIndex && value.m_InstanceIndex == instanceCount)
						{
							value.m_InstanceIndex = groupActionData.m_RemoveInstanceIndex;
							dynamicBuffer[j] = value;
							break;
						}
					}
					continue;
				}
				DynamicBuffer<MeshBatch> dynamicBuffer2 = m_MeshBatches[groupActionData.m_AddInstanceData.m_Entity];
				for (int k = 0; k < dynamicBuffer2.Length; k++)
				{
					MeshBatch value2 = dynamicBuffer2[k];
					if (value2.m_GroupIndex == groupActionData.m_GroupIndex && value2.m_InstanceIndex == -1 && value2.m_MeshGroup == groupActionData.m_AddInstanceData.m_MeshGroup && value2.m_MeshIndex == groupActionData.m_AddInstanceData.m_MeshIndex && value2.m_TileIndex == groupActionData.m_AddInstanceData.m_TileIndex)
					{
						int num2 = math.select(255, 0, groupActionData.m_FadeIn);
						value2.m_InstanceIndex = groupInstanceUpdater.AddInstance(cullingData: new CullingData
						{
							lodFade = num2
						}, instanceData: groupActionData.m_AddInstanceData, mergeIndex: groupActionData.m_MergeIndex);
						dynamicBuffer2[k] = value2;
						break;
					}
				}
			}
			if (num != -1)
			{
				m_BatchInstanceUpdater.EndGroup(groupInstanceUpdater);
			}
			array.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Error> __Game_Tools_Error_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Warning> __Game_Tools_Warning_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Override> __Game_Tools_Override_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Highlighted> __Game_Tools_Highlighted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferLookup;

		public BufferLookup<MeshBatch> __Game_Rendering_MeshBatch_RW_BufferLookup;

		public BufferLookup<FadeBatch> __Game_Rendering_FadeBatch_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Stopped> __Game_Objects_Stopped_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Stack> __Game_Objects_Stack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.NetObject> __Game_Objects_NetObject_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Marker> __Game_Objects_Marker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Relative> __Game_Objects_Relative_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.OutsideConnection> __Game_Net_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.UtilityLane> __Game_Net_UtilityLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Marker> __Game_Net_Marker_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CutRange> __Game_Net_CutRange_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GrowthScaleData> __Game_Prefabs_GrowthScaleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> __Game_Prefabs_QuantityObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshData> __Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshRef> __Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<BatchGroup> __Game_Prefabs_BatchGroup_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>(isReadOnly: true);
			__Game_Rendering_CullingInfo_RO_ComponentLookup = state.GetComponentLookup<CullingInfo>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Tools_Error_RO_ComponentLookup = state.GetComponentLookup<Error>(isReadOnly: true);
			__Game_Tools_Warning_RO_ComponentLookup = state.GetComponentLookup<Warning>(isReadOnly: true);
			__Game_Tools_Override_RO_ComponentLookup = state.GetComponentLookup<Override>(isReadOnly: true);
			__Game_Tools_Highlighted_RO_ComponentLookup = state.GetComponentLookup<Highlighted>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferLookup = state.GetBufferLookup<MeshGroup>(isReadOnly: true);
			__Game_Rendering_MeshBatch_RW_BufferLookup = state.GetBufferLookup<MeshBatch>();
			__Game_Rendering_FadeBatch_RW_BufferLookup = state.GetBufferLookup<FadeBatch>();
			__Game_Objects_Stopped_RO_ComponentLookup = state.GetComponentLookup<Stopped>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_Stack_RO_ComponentLookup = state.GetComponentLookup<Stack>(isReadOnly: true);
			__Game_Objects_NetObject_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.NetObject>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Marker_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Marker>(isReadOnly: true);
			__Game_Objects_Relative_RO_ComponentLookup = state.GetComponentLookup<Relative>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentLookup = state.GetComponentLookup<UnderConstruction>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferLookup = state.GetBufferLookup<TransformFrame>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Net.OutsideConnection>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_UtilityLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.UtilityLane>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Net_Marker_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Marker>(isReadOnly: true);
			__Game_Net_CutRange_RO_BufferLookup = state.GetBufferLookup<CutRange>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_GrowthScaleData_RO_ComponentLookup = state.GetComponentLookup<GrowthScaleData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Prefabs_QuantityObjectData_RO_ComponentLookup = state.GetComponentLookup<QuantityObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionMeshData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup = state.GetComponentLookup<NetCompositionMeshRef>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_BatchGroup_RO_BufferLookup = state.GetBufferLookup<BatchGroup>(isReadOnly: true);
		}
	}

	private RenderingSystem m_RenderingSystem;

	private BatchManagerSystem m_BatchManagerSystem;

	private PreCullingSystem m_PreCullingSystem;

	private UndergroundViewSystem m_UndergroundViewSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ToolSystem m_ToolSystem;

	private Groups m_Groups;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
		m_UndergroundViewSystem = base.World.GetOrCreateSystemManaged<UndergroundViewSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_Groups = base.World.GetOrCreateSystemManaged<Groups>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: true, out dependencies);
		m_Groups.m_GroupActionQueue = new NativeParallelQueue<GroupActionData>(Allocator.TempJob);
		m_Groups.m_VelocityQueue = new NativeQueue<VelocityData>(Allocator.TempJob);
		m_Groups.m_FadeQueue = new NativeQueue<FadeData>(Allocator.TempJob);
		JobHandle dependencies2;
		BatchInstanceJob jobData = new BatchInstanceJob
		{
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ErrorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Error_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WarningData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Warning_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OverrideData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Override_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HighlightedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Highlighted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshBatch_RW_BufferLookup, ref base.CheckedStateRef),
			m_FadeBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_FadeBatch_RW_BufferLookup, ref base.CheckedStateRef),
			m_StoppedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_NetObject_RO_ComponentLookup, ref base.CheckedStateRef),
			m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectMarkerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Marker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RelativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnderConstructionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetOutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_UtilityLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetMarkerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Marker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CutRanges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_CutRange_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_ZoneBlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGrowthScaleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GrowthScaleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabQuantityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_QuantityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionMeshRef = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabBatchGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_BatchGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_MarkersVisible = m_RenderingSystem.markersVisible,
			m_UnspawnedVisible = m_RenderingSystem.unspawnedVisible,
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_UseLodFade = m_RenderingSystem.lodCrossFade,
			m_FrameIndex = m_RenderingSystem.frameIndex,
			m_FrameTime = m_RenderingSystem.frameTime,
			m_DilatedUtilityTypes = m_UndergroundViewSystem.utilityTypes,
			m_VisibleMask = m_PreCullingSystem.visibleMask,
			m_BecameVisible = m_PreCullingSystem.becameVisible,
			m_BecameHidden = m_PreCullingSystem.becameHidden,
			m_BatchInstances = nativeBatchInstances,
			m_CullingData = m_PreCullingSystem.GetUpdatedData(readOnly: true, out dependencies2),
			m_GroupActionQueue = m_Groups.m_GroupActionQueue.AsWriter(),
			m_VelocityQueue = m_Groups.m_VelocityQueue.AsParallelWriter(),
			m_FadeQueue = m_Groups.m_FadeQueue.AsParallelWriter()
		};
		JobHandle jobHandle = jobData.Schedule(jobData.m_CullingData, 4, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
		m_BatchManagerSystem.AddNativeBatchInstancesReader(jobHandle);
		m_PreCullingSystem.AddCullingDataReader(jobHandle);
		m_Groups.m_Dependency = jobHandle;
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
	public BatchInstanceSystem()
	{
	}
}
