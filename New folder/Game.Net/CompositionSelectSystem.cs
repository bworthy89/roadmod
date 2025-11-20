using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Common;
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

namespace Game.Net;

[CompilerGenerated]
public class CompositionSelectSystem : GameSystemBase
{
	private struct CompositionCreateInfo
	{
		public Entity m_Entity;

		public Entity m_Prefab;

		public CompositionFlags m_EdgeFlags;

		public CompositionFlags m_StartFlags;

		public CompositionFlags m_EndFlags;

		public CompositionFlags m_ObsoleteEdgeFlags;

		public CompositionFlags m_ObsoleteStartFlags;

		public CompositionFlags m_ObsoleteEndFlags;

		public Composition m_CompositionData;
	}

	[BurstCompile]
	private struct SelectCompositionJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Upgraded> m_UpgradedType;

		[ReadOnly]
		public ComponentTypeHandle<Fixed> m_FixedType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Composition> m_CompositionType;

		public ComponentTypeHandle<Orphan> m_OrphanType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<Fixed> m_FixedData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetObjectData> m_PrefabNetObjectData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<RoadData> m_PrefabRoadData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<NetGeometryComposition> m_PrefabGeometryCompositions;

		[ReadOnly]
		public BufferLookup<NetGeometryEdgeState> m_PrefabGeometryEdgeStates;

		[ReadOnly]
		public BufferLookup<NetGeometryNodeState> m_PrefabGeometryNodeStates;

		[ReadOnly]
		public BufferLookup<FixedNetElement> m_PrefabFixedNetElements;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		public NativeQueue<CompositionCreateInfo>.ParallelWriter m_CompositionCreateQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Edge> nativeArray2 = chunk.GetNativeArray(ref m_EdgeType);
			if (nativeArray2.Length != 0)
			{
				NativeArray<Curve> nativeArray3 = chunk.GetNativeArray(ref m_CurveType);
				NativeArray<Upgraded> nativeArray4 = chunk.GetNativeArray(ref m_UpgradedType);
				NativeArray<Fixed> nativeArray5 = chunk.GetNativeArray(ref m_FixedType);
				NativeArray<PrefabRef> nativeArray6 = chunk.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Composition> nativeArray7 = chunk.GetNativeArray(ref m_CompositionType);
				NativeArray<Owner> nativeArray8 = chunk.GetNativeArray(ref m_OwnerType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					Edge edge = nativeArray2[i];
					Curve curve = nativeArray3[i];
					CollectionUtils.TryGet(nativeArray8, i, out var value);
					CompositionCreateInfo value2 = new CompositionCreateInfo
					{
						m_Entity = entity,
						m_Prefab = nativeArray6[i].m_Prefab
					};
					NetData prefabNetData = m_PrefabNetData[value2.m_Prefab];
					NetGeometryData prefabGeometryData = m_PrefabGeometryData[value2.m_Prefab];
					DynamicBuffer<NetGeometryComposition> geometryCompositions = m_PrefabGeometryCompositions[value2.m_Prefab];
					CompositionFlags upgradeFlags = default(CompositionFlags);
					CompositionFlags subObjectFlags = GetSubObjectFlags(entity, curve, prefabGeometryData);
					CompositionFlags elevationFlags = GetElevationFlags(entity, edge.m_Start, edge.m_End, prefabGeometryData);
					if (nativeArray4.Length != 0)
					{
						upgradeFlags = nativeArray4[i].m_Flags;
					}
					if (nativeArray5.Length != 0 && m_PrefabFixedNetElements.HasBuffer(value2.m_Prefab))
					{
						Fixed obj = nativeArray5[i];
						DynamicBuffer<FixedNetElement> dynamicBuffer = m_PrefabFixedNetElements[value2.m_Prefab];
						if (obj.m_Index >= 0 && obj.m_Index < dynamicBuffer.Length)
						{
							FixedNetElement fixedNetElement = dynamicBuffer[obj.m_Index];
							upgradeFlags |= fixedNetElement.m_SetState;
							elevationFlags &= ~fixedNetElement.m_UnsetState;
						}
					}
					value2.m_EdgeFlags = GetEdgeFlags(value2.m_Entity, value2.m_Prefab, prefabGeometryData, upgradeFlags, subObjectFlags, elevationFlags, out value2.m_ObsoleteEdgeFlags);
					value2.m_StartFlags = GetNodeFlags(value2.m_Entity, edge.m_Start, value.m_Owner, value2.m_Prefab, prefabNetData, prefabGeometryData, value2.m_EdgeFlags, curve, isStart: true, out value2.m_ObsoleteStartFlags, ref value2.m_ObsoleteEdgeFlags);
					value2.m_EndFlags = GetNodeFlags(value2.m_Entity, edge.m_End, value.m_Owner, value2.m_Prefab, prefabNetData, prefabGeometryData, value2.m_EdgeFlags, curve, isStart: false, out value2.m_ObsoleteEndFlags, ref value2.m_ObsoleteEdgeFlags);
					value2.m_EdgeFlags.m_General |= (value2.m_StartFlags.m_General | value2.m_EndFlags.m_General) & CompositionFlags.General.FixedNodeSize;
					value2.m_CompositionData.m_Edge = FindComposition(geometryCompositions, value2.m_EdgeFlags);
					value2.m_CompositionData.m_StartNode = FindComposition(geometryCompositions, value2.m_StartFlags);
					value2.m_CompositionData.m_EndNode = FindComposition(geometryCompositions, value2.m_EndFlags);
					if (value2.m_CompositionData.m_Edge == Entity.Null || value2.m_ObsoleteEdgeFlags != default(CompositionFlags) || value2.m_CompositionData.m_StartNode == Entity.Null || value2.m_ObsoleteStartFlags != default(CompositionFlags) || value2.m_CompositionData.m_EndNode == Entity.Null || value2.m_ObsoleteEndFlags != default(CompositionFlags))
					{
						m_CompositionCreateQueue.Enqueue(value2);
					}
					else
					{
						nativeArray7[i] = value2.m_CompositionData;
					}
				}
				return;
			}
			NativeArray<PrefabRef> nativeArray9 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Orphan> nativeArray10 = chunk.GetNativeArray(ref m_OrphanType);
			for (int j = 0; j < nativeArray10.Length; j++)
			{
				Entity entity2 = nativeArray[j];
				CompositionCreateInfo value3 = new CompositionCreateInfo
				{
					m_Entity = entity2,
					m_Prefab = nativeArray9[j].m_Prefab
				};
				NetData prefabNetData2 = m_PrefabNetData[value3.m_Prefab];
				NetGeometryData prefabGeometryData2 = m_PrefabGeometryData[value3.m_Prefab];
				DynamicBuffer<NetGeometryComposition> geometryCompositions2 = m_PrefabGeometryCompositions[value3.m_Prefab];
				CompositionFlags elevationFlags2 = GetElevationFlags(Entity.Null, entity2, entity2, prefabGeometryData2);
				CompositionFlags obsoleteEdgeFlags = default(CompositionFlags);
				value3.m_EdgeFlags = GetNodeFlags(Entity.Null, value3.m_Entity, Entity.Null, value3.m_Prefab, prefabNetData2, prefabGeometryData2, elevationFlags2, default(Curve), isStart: true, out value3.m_ObsoleteEdgeFlags, ref obsoleteEdgeFlags);
				value3.m_CompositionData.m_Edge = FindComposition(geometryCompositions2, value3.m_EdgeFlags);
				if (value3.m_CompositionData.m_Edge == Entity.Null || value3.m_ObsoleteEdgeFlags != default(CompositionFlags))
				{
					m_CompositionCreateQueue.Enqueue(value3);
					continue;
				}
				nativeArray10[j] = new Orphan
				{
					m_Composition = value3.m_CompositionData.m_Edge
				};
			}
		}

		private Entity FindComposition(DynamicBuffer<NetGeometryComposition> geometryCompositions, CompositionFlags flags)
		{
			for (int i = 0; i < geometryCompositions.Length; i++)
			{
				NetGeometryComposition netGeometryComposition = geometryCompositions[i];
				if (netGeometryComposition.m_Mask == flags)
				{
					return netGeometryComposition.m_Composition;
				}
			}
			return Entity.Null;
		}

		private CompositionFlags GetSubObjectFlags(Entity entity, Curve curve, NetGeometryData prefabGeometryData)
		{
			CompositionFlags result = default(CompositionFlags);
			if (m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity subObject = bufferData[i].m_SubObject;
					PrefabRef prefabRef = m_PrefabRefData[subObject];
					if (!m_PrefabNetObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData) || !m_PrefabPlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
					{
						continue;
					}
					componentData.m_CompositionFlags &= ~CompositionFlags.nodeMask;
					if ((componentData2.m_Flags & Game.Objects.PlacementFlags.Attached) != Game.Objects.PlacementFlags.None && m_AttachedData.TryGetComponent(subObject, out var componentData3))
					{
						if (componentData3.m_Parent != entity)
						{
							continue;
						}
						Transform transform = m_TransformData[subObject];
						float3 @float = MathUtils.Position(curve.m_Bezier, componentData3.m_CurvePosition);
						float num = math.dot(MathUtils.Left(math.normalizesafe(MathUtils.Tangent(curve.m_Bezier, componentData3.m_CurvePosition)).xz), transform.m_Position.xz - @float.xz);
						if (num > 0f)
						{
							componentData.m_CompositionFlags = NetCompositionHelpers.InvertCompositionFlags(componentData.m_CompositionFlags);
						}
						if ((componentData.m_CompositionFlags & ~CompositionFlags.optionMask) == default(CompositionFlags) && componentData.m_CompositionFlags.m_General != 0 && (componentData.m_CompositionFlags.m_Left != 0 || componentData.m_CompositionFlags.m_Right != 0))
						{
							if (math.abs(num) < prefabGeometryData.m_DefaultWidth * (1f / 6f))
							{
								componentData.m_CompositionFlags.m_Left = (CompositionFlags.Side)0u;
								componentData.m_CompositionFlags.m_Right = (CompositionFlags.Side)0u;
							}
							else
							{
								componentData.m_CompositionFlags.m_General = (CompositionFlags.General)0u;
							}
						}
					}
					result |= componentData.m_CompositionFlags;
				}
			}
			return result;
		}

		private CompositionFlags GetSubObjectFlags(Entity entity, NetData prefabNetData)
		{
			CompositionFlags result = default(CompositionFlags);
			if (m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity subObject = bufferData[i].m_SubObject;
					PrefabRef prefabRef = m_PrefabRefData[subObject];
					if (m_PrefabNetObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && m_PrefabPlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && ((componentData2.m_Flags & Game.Objects.PlacementFlags.Attached) == 0 || !m_AttachedData.TryGetComponent(subObject, out var componentData3) || !(componentData3.m_Parent != entity)))
					{
						result |= componentData.m_CompositionFlags & CompositionFlags.nodeMask;
					}
				}
				if ((result.m_General & CompositionFlags.General.FixedNodeSize) != 0)
				{
					PrefabRef prefabRef2 = m_PrefabRefData[entity];
					NetData netData = m_PrefabNetData[prefabRef2.m_Prefab];
					if ((prefabNetData.m_RequiredLayers & netData.m_RequiredLayers) == 0)
					{
						result.m_General &= ~CompositionFlags.General.FixedNodeSize;
					}
				}
			}
			return result;
		}

		private CompositionFlags GetElevationFlags(Entity edge, Entity startNode, Entity endNode, NetGeometryData prefabGeometryData)
		{
			m_ElevationData.TryGetComponent(startNode, out var componentData);
			m_ElevationData.TryGetComponent(endNode, out var componentData2);
			Elevation componentData3;
			if (edge == Entity.Null)
			{
				componentData3 = componentData;
			}
			else
			{
				m_ElevationData.TryGetComponent(edge, out componentData3);
			}
			return NetCompositionHelpers.GetElevationFlags(componentData, componentData3, componentData2, prefabGeometryData);
		}

		private CompositionFlags GetEdgeFlags(Entity edge, Entity prefab, NetGeometryData prefabGeometryData, CompositionFlags upgradeFlags, CompositionFlags subObjectFlags, CompositionFlags elevationFlags, out CompositionFlags obsoleteEdgeFlags)
		{
			CompositionFlags compositionFlags = upgradeFlags | subObjectFlags | elevationFlags;
			obsoleteEdgeFlags = default(CompositionFlags);
			compositionFlags.m_General |= CompositionFlags.General.Edge;
			compositionFlags |= GetHandednessFlags(prefabGeometryData);
			compositionFlags |= GetEdgeStates(prefab, compositionFlags);
			bool num = (compositionFlags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) != 0;
			bool flag = (compositionFlags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) != 0;
			bool flag2 = (compositionFlags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) != 0;
			if ((prefabGeometryData.m_Flags & (GeometryFlags.RequireElevated | GeometryFlags.ElevatedIsRaised)) == GeometryFlags.ElevatedIsRaised)
			{
				flag = false;
				flag2 = false;
			}
			if (num)
			{
				CompositionFlags.General general = upgradeFlags.m_General & (CompositionFlags.General.PrimaryMiddleBeautification | CompositionFlags.General.SecondaryMiddleBeautification);
				compositionFlags.m_General &= ~general;
				obsoleteEdgeFlags.m_General |= general;
			}
			if (num || flag)
			{
				CompositionFlags.Side side = upgradeFlags.m_Left & (CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.SecondaryBeautification | CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.SoundBarrier);
				compositionFlags.m_Left &= ~side;
				obsoleteEdgeFlags.m_Left |= side;
			}
			if (num || flag2)
			{
				CompositionFlags.Side side2 = upgradeFlags.m_Right & (CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.SecondaryBeautification | CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.SoundBarrier);
				compositionFlags.m_Right &= ~side2;
				obsoleteEdgeFlags.m_Right |= side2;
			}
			return compositionFlags;
		}

		private CompositionFlags GetEdgeStates(Entity prefab, CompositionFlags edgeFlags)
		{
			CompositionFlags result = default(CompositionFlags);
			if (m_PrefabGeometryEdgeStates.TryGetBuffer(prefab, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					NetGeometryEdgeState edgeState = bufferData[i];
					if (NetCompositionHelpers.TestEdgeFlags(edgeState, edgeFlags))
					{
						result |= edgeState.m_State;
					}
				}
			}
			return result;
		}

		private CompositionFlags GetNodeStates(DynamicBuffer<NetGeometryNodeState> nodeStates, CompositionFlags edgeFlags1, CompositionFlags edgeFlags2, bool isLeft, bool isRight)
		{
			CompositionFlags result = default(CompositionFlags);
			for (int i = 0; i < nodeStates.Length; i++)
			{
				NetGeometryNodeState netGeometryNodeState = nodeStates[i];
				NetGeometryNodeState nodeState = netGeometryNodeState;
				CompositionFlags compositionFlags = edgeFlags1;
				CompositionFlags compositionFlags2 = edgeFlags2;
				if (!isLeft)
				{
					nodeState.m_CompositionNone.m_Left = (CompositionFlags.Side)0u;
					nodeState.m_State.m_Left = (CompositionFlags.Side)0u;
					compositionFlags2.m_Right = (CompositionFlags.Side)0u;
					if (nodeState.m_MatchType == NetEdgeMatchType.Exclusive)
					{
						compositionFlags.m_Left = (CompositionFlags.Side)0u;
					}
				}
				if (!isRight)
				{
					nodeState.m_CompositionNone.m_Right = (CompositionFlags.Side)0u;
					nodeState.m_State.m_Right = (CompositionFlags.Side)0u;
					compositionFlags2.m_Left = (CompositionFlags.Side)0u;
					if (nodeState.m_MatchType == NetEdgeMatchType.Exclusive)
					{
						compositionFlags.m_Right = (CompositionFlags.Side)0u;
					}
				}
				if (NetCompositionHelpers.TestEdgeMatch(match: new bool2(NetCompositionHelpers.TestEdgeFlags(netGeometryNodeState, compositionFlags), NetCompositionHelpers.TestEdgeFlags(nodeState, compositionFlags2)), nodeState: nodeState))
				{
					result |= nodeState.m_State;
				}
			}
			return result;
		}

		private CompositionFlags GetEdgeFlags(Entity edge, bool invert, Entity prefab, NetGeometryData prefabGeometryData, CompositionFlags elevationFlags)
		{
			Curve curve = m_CurveData[edge];
			CompositionFlags compositionFlags = default(CompositionFlags);
			CompositionFlags subObjectFlags = GetSubObjectFlags(edge, curve, prefabGeometryData);
			if (m_UpgradedData.HasComponent(edge))
			{
				compositionFlags = m_UpgradedData[edge].m_Flags;
			}
			if (m_FixedData.HasComponent(edge) && m_PrefabFixedNetElements.HasBuffer(prefab))
			{
				Fixed obj = m_FixedData[edge];
				DynamicBuffer<FixedNetElement> dynamicBuffer = m_PrefabFixedNetElements[prefab];
				if (obj.m_Index >= 0 && obj.m_Index < dynamicBuffer.Length)
				{
					FixedNetElement fixedNetElement = dynamicBuffer[obj.m_Index];
					compositionFlags |= fixedNetElement.m_SetState;
					elevationFlags &= ~fixedNetElement.m_UnsetState;
				}
			}
			CompositionFlags compositionFlags2 = compositionFlags | subObjectFlags | elevationFlags;
			compositionFlags2.m_General |= CompositionFlags.General.Edge;
			compositionFlags2 |= GetHandednessFlags(prefabGeometryData);
			compositionFlags2 |= GetEdgeStates(prefab, compositionFlags2);
			if (invert)
			{
				compositionFlags2 = NetCompositionHelpers.InvertCompositionFlags(compositionFlags2);
			}
			return compositionFlags2;
		}

		private CompositionFlags GetHandednessFlags(NetGeometryData prefabGeometryData)
		{
			CompositionFlags result = default(CompositionFlags);
			if ((prefabGeometryData.m_Flags & GeometryFlags.IsLefthanded) != 0 != m_LeftHandTraffic)
			{
				if ((prefabGeometryData.m_Flags & GeometryFlags.InvertCompositionHandedness) != 0)
				{
					result.m_General |= CompositionFlags.General.Invert;
				}
				if ((prefabGeometryData.m_Flags & GeometryFlags.FlipCompositionHandedness) != 0)
				{
					result.m_General |= CompositionFlags.General.Flip;
				}
			}
			return result;
		}

		private CompositionFlags GetNodeFlags(Entity edge, Entity node, Entity owner, Entity prefab, NetData prefabNetData, NetGeometryData prefabGeometryData, CompositionFlags edgeFlags, Curve curve, bool isStart, out CompositionFlags obsoleteNodeFlags, ref CompositionFlags obsoleteEdgeFlags)
		{
			CompositionFlags compositionFlags = edgeFlags;
			edgeFlags.m_General &= CompositionFlags.General.Lighting | CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel | CompositionFlags.General.MiddlePlatform | CompositionFlags.General.WideMedian | CompositionFlags.General.PrimaryMiddleBeautification | CompositionFlags.General.SecondaryMiddleBeautification;
			edgeFlags.m_Left &= CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered | CompositionFlags.Side.PrimaryTrack | CompositionFlags.Side.SecondaryTrack | CompositionFlags.Side.TertiaryTrack | CompositionFlags.Side.QuaternaryTrack | CompositionFlags.Side.PrimaryStop | CompositionFlags.Side.SecondaryStop | CompositionFlags.Side.TertiaryStop | CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.SecondaryBeautification | CompositionFlags.Side.AbruptEnd | CompositionFlags.Side.Gate | CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.ParkingSpaces | CompositionFlags.Side.SoundBarrier | CompositionFlags.Side.SecondaryLane | CompositionFlags.Side.ForbidLeftTurn | CompositionFlags.Side.ForbidRightTurn | CompositionFlags.Side.ForbidStraight | CompositionFlags.Side.ForbidSecondary;
			edgeFlags.m_Right &= CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered | CompositionFlags.Side.PrimaryTrack | CompositionFlags.Side.SecondaryTrack | CompositionFlags.Side.TertiaryTrack | CompositionFlags.Side.QuaternaryTrack | CompositionFlags.Side.PrimaryStop | CompositionFlags.Side.SecondaryStop | CompositionFlags.Side.TertiaryStop | CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.SecondaryBeautification | CompositionFlags.Side.AbruptEnd | CompositionFlags.Side.Gate | CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.ParkingSpaces | CompositionFlags.Side.SoundBarrier | CompositionFlags.Side.SecondaryLane | CompositionFlags.Side.ForbidLeftTurn | CompositionFlags.Side.ForbidRightTurn | CompositionFlags.Side.ForbidStraight | CompositionFlags.Side.ForbidSecondary;
			CompositionFlags handednessFlags = GetHandednessFlags(prefabGeometryData);
			obsoleteNodeFlags = default(CompositionFlags);
			handednessFlags.m_General |= CompositionFlags.General.Node;
			if (isStart)
			{
				handednessFlags.m_General ^= CompositionFlags.General.Invert;
				edgeFlags = NetCompositionHelpers.InvertCompositionFlags(edgeFlags);
			}
			handednessFlags |= GetSubObjectFlags(node, prefabNetData);
			handednessFlags |= edgeFlags;
			if (m_EdgeData.TryGetComponent(owner, out var componentData))
			{
				Curve curve2 = m_CurveData[owner];
				PrefabRef prefabRef = m_PrefabRefData[owner];
				NetData prefabNetData2 = m_PrefabNetData[prefabRef.m_Prefab];
				NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				float3 x = (isStart ? curve.m_Bezier.a : curve.m_Bezier.d);
				float num = math.distancesq(x, curve2.m_Bezier.a);
				float num2 = math.distancesq(x, curve2.m_Bezier.d);
				Entity entity = ((num <= num2) ? componentData.m_Start : componentData.m_End);
				handednessFlags |= GetSubObjectFlags(entity, prefabNetData2) & new CompositionFlags(CompositionFlags.General.FixedNodeSize, (CompositionFlags.Side)0u, (CompositionFlags.Side)0u);
				EdgeIterator edgeIterator = new EdgeIterator(owner, entity, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[value.m_Edge];
					NetGeometryData netGeometryData2 = m_PrefabGeometryData[prefabRef2.m_Prefab];
					if ((netGeometryData2.m_MergeLayers & netGeometryData.m_MergeLayers) == 0)
					{
						Layer layer = netGeometryData2.m_MergeLayers | netGeometryData.m_MergeLayers;
						if (((layer & (Layer.Road | Layer.Pathway | Layer.MarkerPathway | Layer.PublicTransportRoad)) != Layer.None && (layer & (Layer.TrainTrack | Layer.SubwayTrack)) != Layer.None) || ((layer & (Layer.Road | Layer.TrainTrack | Layer.Pathway | Layer.TramTrack | Layer.SubwayTrack | Layer.MarkerPathway | Layer.PublicTransportRoad)) != Layer.None && (layer & Layer.Waterway) != Layer.None))
						{
							handednessFlags.m_General |= CompositionFlags.General.LevelCrossing;
						}
					}
				}
			}
			if ((prefabGeometryData.m_Flags & GeometryFlags.SupportRoundabout) == 0)
			{
				handednessFlags.m_General &= ~CompositionFlags.General.Roundabout;
			}
			int num3 = 0;
			int3 @int = default(int3);
			bool flag = true;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			bool flag6 = false;
			bool flag7 = false;
			bool flag8 = false;
			m_PrefabGeometryNodeStates.TryGetBuffer(prefab, out var bufferData);
			if (!isStart)
			{
				curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
			}
			FindFriendEdges(edge, node, prefabGeometryData.m_MergeLayers, curve, out var leftEdge, out var rightEdge);
			bool includeMiddleConnections = (prefabGeometryData.m_MergeLayers & (Layer.Pathway | Layer.MarkerPathway)) != 0;
			EdgeIterator edgeIterator2 = new EdgeIterator(edge, node, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData, includeMiddleConnections);
			EdgeIteratorValue value2;
			while (edgeIterator2.GetNext(out value2))
			{
				PrefabRef prefabRef3 = m_PrefabRefData[value2.m_Edge];
				NetGeometryData prefabGeometryData2 = m_PrefabGeometryData[prefabRef3.m_Prefab];
				m_PrefabGeometryNodeStates.TryGetBuffer(prefabRef3.m_Prefab, out var bufferData2);
				if (m_PrefabRoadData.HasComponent(prefabRef3.m_Prefab))
				{
					RoadData roadData = m_PrefabRoadData[prefabRef3.m_Prefab];
					flag3 |= (roadData.m_Flags & Game.Prefabs.RoadFlags.PreferTrafficLights) != 0;
					if ((roadData.m_Flags & (Game.Prefabs.RoadFlags.DefaultIsForward | Game.Prefabs.RoadFlags.DefaultIsBackward)) != 0)
					{
						@int += math.select(new int3(0, 1, 1), new int3(1, 0, 1), value2.m_End == ((roadData.m_Flags & Game.Prefabs.RoadFlags.DefaultIsForward) != 0));
					}
					else
					{
						++@int;
					}
				}
				if ((prefabGeometryData2.m_MergeLayers & prefabGeometryData.m_MergeLayers) == 0)
				{
					Layer layer2 = prefabGeometryData2.m_MergeLayers | prefabGeometryData.m_MergeLayers;
					if (((layer2 & (Layer.Road | Layer.Pathway | Layer.MarkerPathway | Layer.PublicTransportRoad)) != Layer.None && (layer2 & (Layer.TrainTrack | Layer.SubwayTrack)) != Layer.None) || ((layer2 & (Layer.Road | Layer.TrainTrack | Layer.Pathway | Layer.TramTrack | Layer.SubwayTrack | Layer.MarkerPathway | Layer.PublicTransportRoad)) != Layer.None && (layer2 & Layer.Waterway) != Layer.None))
					{
						handednessFlags.m_General |= CompositionFlags.General.LevelCrossing;
					}
					if ((layer2 & Layer.Road) != Layer.None && (layer2 & (Layer.Pathway | Layer.MarkerPathway)) != Layer.None)
					{
						flag2 = true;
					}
				}
				else
				{
					if (value2.m_Middle)
					{
						continue;
					}
					bool flag9 = value2.m_Edge == leftEdge;
					bool flag10 = value2.m_Edge == rightEdge;
					Edge edge2 = m_EdgeData[value2.m_Edge];
					CompositionFlags elevationFlags = GetElevationFlags(value2.m_Edge, edge2.m_Start, edge2.m_End, prefabGeometryData2);
					CompositionFlags flags = elevationFlags;
					if (value2.m_End)
					{
						flags = NetCompositionHelpers.InvertCompositionFlags(flags);
					}
					CompositionFlags edgeFlags2 = GetEdgeFlags(value2.m_Edge, value2.m_End, prefabRef3.m_Prefab, prefabGeometryData2, elevationFlags);
					if ((edgeFlags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) != 0 || (edgeFlags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered | CompositionFlags.Side.SoundBarrier)) != 0 || (edgeFlags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered | CompositionFlags.Side.SoundBarrier)) != 0)
					{
						if ((edgeFlags.m_General & CompositionFlags.General.Elevated) != 0)
						{
							if ((flags.m_General & CompositionFlags.General.Elevated) == 0)
							{
								if (flag9)
								{
									if ((flags.m_Left & CompositionFlags.Side.Raised) != 0)
									{
										handednessFlags.m_Left |= CompositionFlags.Side.LowTransition;
									}
									else
									{
										handednessFlags.m_Left |= CompositionFlags.Side.HighTransition;
									}
								}
								if (flag10)
								{
									if ((flags.m_Right & CompositionFlags.Side.Raised) != 0)
									{
										handednessFlags.m_Right |= CompositionFlags.Side.LowTransition;
									}
									else
									{
										handednessFlags.m_Right |= CompositionFlags.Side.HighTransition;
									}
								}
							}
						}
						else if ((edgeFlags.m_General & CompositionFlags.General.Tunnel) != 0)
						{
							if ((flags.m_General & CompositionFlags.General.Tunnel) == 0)
							{
								if (flag9)
								{
									if ((flags.m_Left & CompositionFlags.Side.Lowered) != 0)
									{
										handednessFlags.m_Left |= CompositionFlags.Side.LowTransition;
									}
									else
									{
										handednessFlags.m_Left |= CompositionFlags.Side.HighTransition;
									}
								}
								if (flag10)
								{
									if ((flags.m_Right & CompositionFlags.Side.Lowered) != 0)
									{
										handednessFlags.m_Right |= CompositionFlags.Side.LowTransition;
									}
									else
									{
										handednessFlags.m_Right |= CompositionFlags.Side.HighTransition;
									}
								}
							}
						}
						else
						{
							if (flag9)
							{
								if ((edgeFlags.m_Left & CompositionFlags.Side.Raised) != 0)
								{
									if ((flags.m_Left & CompositionFlags.Side.Raised) == 0 && (flags.m_General & CompositionFlags.General.Elevated) == 0)
									{
										handednessFlags.m_Left |= CompositionFlags.Side.LowTransition;
									}
								}
								else if ((edgeFlags.m_Left & CompositionFlags.Side.Lowered) != 0)
								{
									if ((flags.m_Left & CompositionFlags.Side.Lowered) == 0 && (flags.m_General & CompositionFlags.General.Tunnel) == 0)
									{
										handednessFlags.m_Left |= CompositionFlags.Side.LowTransition;
									}
								}
								else if ((edgeFlags.m_Left & CompositionFlags.Side.SoundBarrier) != 0 && (edgeFlags2.m_Left & CompositionFlags.Side.SoundBarrier) == 0)
								{
									handednessFlags.m_Left |= CompositionFlags.Side.LowTransition;
								}
							}
							if (flag10)
							{
								if ((edgeFlags.m_Right & CompositionFlags.Side.Raised) != 0)
								{
									if ((flags.m_Right & CompositionFlags.Side.Raised) == 0 && (flags.m_General & CompositionFlags.General.Elevated) == 0)
									{
										handednessFlags.m_Right |= CompositionFlags.Side.LowTransition;
									}
								}
								else if ((edgeFlags.m_Right & CompositionFlags.Side.Lowered) != 0)
								{
									if ((flags.m_Right & CompositionFlags.Side.Lowered) == 0 && (flags.m_General & CompositionFlags.General.Tunnel) == 0)
									{
										handednessFlags.m_Right |= CompositionFlags.Side.LowTransition;
									}
								}
								else if ((edgeFlags.m_Right & CompositionFlags.Side.SoundBarrier) != 0 && (edgeFlags2.m_Right & CompositionFlags.Side.SoundBarrier) == 0)
								{
									handednessFlags.m_Right |= CompositionFlags.Side.LowTransition;
								}
							}
						}
					}
					if ((edgeFlags2.m_Left & CompositionFlags.Side.RemoveCrosswalk) != 0)
					{
						flag7 = true;
					}
					else
					{
						if (((edgeFlags2.m_Left | edgeFlags2.m_Right) & CompositionFlags.Side.Sidewalk) != 0 && (edgeFlags2.m_General & CompositionFlags.General.MiddlePlatform) != 0)
						{
							flag5 = true;
						}
						if ((edgeFlags2.m_Left & CompositionFlags.Side.AddCrosswalk) != 0)
						{
							flag5 = true;
							flag8 = true;
						}
						flag6 = true;
					}
					if (((edgeFlags2.m_Left | edgeFlags2.m_Right) & CompositionFlags.Side.ParkingSpaces) != 0)
					{
						flag4 = true;
					}
					if (prefabGeometryData.m_StyleType != prefabGeometryData2.m_StyleType)
					{
						handednessFlags.m_General |= CompositionFlags.General.StyleBreak;
					}
					if (bufferData.IsCreated && bufferData.Length != 0)
					{
						handednessFlags |= GetNodeStates(bufferData, compositionFlags, edgeFlags2, flag9, flag10);
					}
					if (bufferData2.IsCreated && bufferData2.Length != 0)
					{
						handednessFlags |= NetCompositionHelpers.InvertCompositionFlags(GetNodeStates(bufferData2, edgeFlags2, compositionFlags, flag10, flag9));
					}
					if (value2.m_Edge != edge)
					{
						if (prefabRef3.m_Prefab != prefab)
						{
							flag = false;
						}
						num3++;
					}
				}
			}
			if ((handednessFlags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) != 0)
			{
				bool num4 = (handednessFlags.m_Left & (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) != 0;
				bool flag11 = (handednessFlags.m_Right & (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) != 0;
				if (num4 && !flag11)
				{
					handednessFlags.m_Right |= CompositionFlags.Side.LowTransition;
				}
				if (!num4 && flag11)
				{
					handednessFlags.m_Left |= CompositionFlags.Side.LowTransition;
				}
			}
			if (flag2 && (prefabGeometryData.m_MergeLayers & (Layer.Pathway | Layer.MarkerPathway)) != Layer.None)
			{
				return handednessFlags;
			}
			flag = flag && num3 == 1;
			flag2 = flag2 && num3 >= 1;
			flag6 = flag6 && flag2;
			if (num3 >= 2)
			{
				handednessFlags.m_General |= CompositionFlags.General.Intersection;
				if ((handednessFlags.m_Left & handednessFlags.m_Right & CompositionFlags.Side.Sidewalk) != 0 || flag2)
				{
					handednessFlags.m_General |= CompositionFlags.General.Crosswalk;
				}
			}
			else
			{
				switch (num3)
				{
				case 1:
					if (flag)
					{
						if (flag4 && ((compositionFlags.m_Left | compositionFlags.m_Right) & CompositionFlags.Side.ParkingSpaces) != 0 && (handednessFlags.m_General & CompositionFlags.General.LevelCrossing) == 0)
						{
							handednessFlags.m_General |= CompositionFlags.General.Crosswalk;
						}
					}
					else
					{
						handednessFlags.m_General |= CompositionFlags.General.Intersection;
					}
					if (flag2)
					{
						handednessFlags.m_General |= CompositionFlags.General.Crosswalk;
					}
					break;
				case 0:
					handednessFlags.m_General |= CompositionFlags.General.DeadEnd;
					flag5 = false;
					if ((handednessFlags.m_General & CompositionFlags.General.Invert) != 0)
					{
						obsoleteEdgeFlags.m_Left |= compositionFlags.m_Left & (CompositionFlags.Side.AddCrosswalk | CompositionFlags.Side.RemoveCrosswalk);
					}
					else
					{
						obsoleteEdgeFlags.m_Right |= compositionFlags.m_Right & (CompositionFlags.Side.AddCrosswalk | CompositionFlags.Side.RemoveCrosswalk);
					}
					break;
				}
			}
			if (num3 != 0)
			{
				if (((compositionFlags.m_Left | compositionFlags.m_Right) & CompositionFlags.Side.Sidewalk) != 0 && (compositionFlags.m_General & CompositionFlags.General.MiddlePlatform) != 0)
				{
					handednessFlags.m_General |= CompositionFlags.General.Crosswalk;
				}
				if ((handednessFlags.m_General & (CompositionFlags.General.Intersection | CompositionFlags.General.MedianBreak)) == 0)
				{
					if ((handednessFlags.m_General & CompositionFlags.General.Crosswalk) != 0)
					{
						if ((handednessFlags.m_General & CompositionFlags.General.Invert) != 0)
						{
							if ((compositionFlags.m_Left & CompositionFlags.Side.AddCrosswalk) != 0 || flag8)
							{
								obsoleteEdgeFlags.m_Left |= compositionFlags.m_Left & (CompositionFlags.Side.AddCrosswalk | CompositionFlags.Side.RemoveCrosswalk);
							}
							else if ((compositionFlags.m_Left & CompositionFlags.Side.RemoveCrosswalk) != 0 || flag7)
							{
								handednessFlags.m_General &= ~CompositionFlags.General.Crosswalk;
								flag6 = false;
							}
						}
						else if ((compositionFlags.m_Right & CompositionFlags.Side.AddCrosswalk) != 0 || flag8)
						{
							obsoleteEdgeFlags.m_Right |= compositionFlags.m_Right & (CompositionFlags.Side.AddCrosswalk | CompositionFlags.Side.RemoveCrosswalk);
						}
						else if ((compositionFlags.m_Right & CompositionFlags.Side.RemoveCrosswalk) != 0 || flag7)
						{
							handednessFlags.m_General &= ~CompositionFlags.General.Crosswalk;
							flag6 = false;
						}
					}
					else if ((handednessFlags.m_General & CompositionFlags.General.Invert) != 0)
					{
						if ((compositionFlags.m_Left & CompositionFlags.Side.RemoveCrosswalk) != 0 || flag7)
						{
							obsoleteEdgeFlags.m_Left |= compositionFlags.m_Left & (CompositionFlags.Side.AddCrosswalk | CompositionFlags.Side.RemoveCrosswalk);
						}
						else if ((compositionFlags.m_Left & CompositionFlags.Side.AddCrosswalk) != 0 || flag8)
						{
							handednessFlags.m_General |= CompositionFlags.General.Crosswalk;
						}
					}
					else if ((compositionFlags.m_Right & CompositionFlags.Side.RemoveCrosswalk) != 0 || flag7)
					{
						obsoleteEdgeFlags.m_Right |= compositionFlags.m_Right & (CompositionFlags.Side.AddCrosswalk | CompositionFlags.Side.RemoveCrosswalk);
					}
					else if ((compositionFlags.m_Right & CompositionFlags.Side.AddCrosswalk) != 0 || flag8)
					{
						handednessFlags.m_General |= CompositionFlags.General.Crosswalk;
					}
				}
				else if ((handednessFlags.m_General & CompositionFlags.General.Crosswalk) != 0)
				{
					if ((handednessFlags.m_General & CompositionFlags.General.Invert) != 0)
					{
						if ((compositionFlags.m_Left & CompositionFlags.Side.AddCrosswalk) != 0)
						{
							obsoleteEdgeFlags.m_Left |= CompositionFlags.Side.AddCrosswalk;
						}
						if ((compositionFlags.m_Left & CompositionFlags.Side.RemoveCrosswalk) != 0)
						{
							handednessFlags.m_General &= ~CompositionFlags.General.Crosswalk;
						}
					}
					else
					{
						if ((compositionFlags.m_Right & CompositionFlags.Side.AddCrosswalk) != 0)
						{
							obsoleteEdgeFlags.m_Right |= CompositionFlags.Side.AddCrosswalk;
						}
						if ((compositionFlags.m_Right & CompositionFlags.Side.RemoveCrosswalk) != 0)
						{
							handednessFlags.m_General &= ~CompositionFlags.General.Crosswalk;
						}
					}
				}
				else if ((handednessFlags.m_General & CompositionFlags.General.Invert) != 0)
				{
					if ((compositionFlags.m_Left & CompositionFlags.Side.RemoveCrosswalk) != 0)
					{
						obsoleteEdgeFlags.m_Left |= CompositionFlags.Side.RemoveCrosswalk;
					}
					if ((compositionFlags.m_Left & CompositionFlags.Side.AddCrosswalk) != 0)
					{
						handednessFlags.m_General |= CompositionFlags.General.Crosswalk;
					}
				}
				else
				{
					if ((compositionFlags.m_Right & CompositionFlags.Side.RemoveCrosswalk) != 0)
					{
						obsoleteEdgeFlags.m_Right |= CompositionFlags.Side.RemoveCrosswalk;
					}
					if ((compositionFlags.m_Right & CompositionFlags.Side.AddCrosswalk) != 0)
					{
						handednessFlags.m_General |= CompositionFlags.General.Crosswalk;
					}
				}
				if ((handednessFlags.m_General & CompositionFlags.General.MedianBreak) != 0 && ((handednessFlags.m_General & CompositionFlags.General.Crosswalk) != 0 || flag5))
				{
					handednessFlags.m_General |= CompositionFlags.General.Intersection;
				}
			}
			CompositionFlags compositionFlags2 = default(CompositionFlags);
			if (m_UpgradedData.HasComponent(node))
			{
				Upgraded upgraded = m_UpgradedData[node];
				compositionFlags2 = upgraded.m_Flags;
				upgraded.m_Flags.m_General &= ~CompositionFlags.General.RemoveTrafficLights;
				handednessFlags |= upgraded.m_Flags;
			}
			if ((handednessFlags.m_General & (CompositionFlags.General.Roundabout | CompositionFlags.General.LevelCrossing)) == 0 && ((handednessFlags.m_General & (CompositionFlags.General.Intersection | CompositionFlags.General.Crosswalk)) != 0 || flag2 || flag5))
			{
				if (flag3 && math.all(@int >= new int3(1, 1, 2)) && (math.all(@int >= new int3(2, 1, 3)) || (handednessFlags.m_General & CompositionFlags.General.Crosswalk) != 0 || flag5 || flag6))
				{
					if ((compositionFlags2.m_General & (CompositionFlags.General.RemoveTrafficLights | CompositionFlags.General.AllWayStop)) != 0)
					{
						handednessFlags.m_General &= ~CompositionFlags.General.TrafficLights;
					}
					else
					{
						handednessFlags.m_General |= CompositionFlags.General.TrafficLights;
					}
					obsoleteNodeFlags.m_General |= compositionFlags2.m_General & CompositionFlags.General.TrafficLights;
				}
				else
				{
					obsoleteNodeFlags.m_General |= compositionFlags2.m_General & CompositionFlags.General.RemoveTrafficLights;
				}
			}
			else
			{
				handednessFlags.m_General &= ~(CompositionFlags.General.TrafficLights | CompositionFlags.General.AllWayStop);
				obsoleteNodeFlags.m_General |= compositionFlags2.m_General & (CompositionFlags.General.TrafficLights | CompositionFlags.General.RemoveTrafficLights | CompositionFlags.General.AllWayStop);
			}
			if ((handednessFlags.m_General & (CompositionFlags.General.Intersection | CompositionFlags.General.Roundabout)) != CompositionFlags.General.Intersection)
			{
				handednessFlags.m_Left &= ~(CompositionFlags.Side.ForbidLeftTurn | CompositionFlags.Side.ForbidRightTurn | CompositionFlags.Side.ForbidStraight);
				handednessFlags.m_Right &= ~(CompositionFlags.Side.ForbidLeftTurn | CompositionFlags.Side.ForbidRightTurn | CompositionFlags.Side.ForbidStraight);
				if ((handednessFlags.m_General & CompositionFlags.General.Invert) != 0)
				{
					obsoleteEdgeFlags.m_Left |= compositionFlags.m_Left & (CompositionFlags.Side.ForbidLeftTurn | CompositionFlags.Side.ForbidRightTurn | CompositionFlags.Side.ForbidStraight);
				}
				else
				{
					obsoleteEdgeFlags.m_Right |= compositionFlags.m_Right & (CompositionFlags.Side.ForbidLeftTurn | CompositionFlags.Side.ForbidRightTurn | CompositionFlags.Side.ForbidStraight);
				}
			}
			return handednessFlags;
		}

		private void FindFriendEdges(Entity edge, Entity node, Layer mergeLayers, Curve curve, out Entity leftEdge, out Entity rightEdge)
		{
			float2 @float = 0f;
			int num = 0;
			leftEdge = edge;
			rightEdge = edge;
			EdgeIterator edgeIterator = new EdgeIterator(edge, node, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				if (!(value.m_Edge == edge))
				{
					PrefabRef prefabRef = m_PrefabRefData[value.m_Edge];
					if ((m_PrefabGeometryData[prefabRef.m_Prefab].m_MergeLayers & mergeLayers) != Layer.None)
					{
						Curve curve2 = m_CurveData[value.m_Edge];
						@float += math.select(curve2.m_Bezier.a.xz, curve2.m_Bezier.d.xz, value.m_End);
						num++;
						leftEdge = value.m_Edge;
						rightEdge = value.m_Edge;
					}
				}
			}
			if (num <= 1)
			{
				return;
			}
			@float /= (float)num;
			float2 float2 = math.normalizesafe(MathUtils.Position(curve.m_Bezier, 0.5f).xz - @float);
			float2 x = MathUtils.Right(float2);
			float num2 = -2f;
			float num3 = 2f;
			edgeIterator = new EdgeIterator(edge, node, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData);
			EdgeIteratorValue value2;
			while (edgeIterator.GetNext(out value2))
			{
				if (value2.m_Edge == edge)
				{
					continue;
				}
				PrefabRef prefabRef2 = m_PrefabRefData[value2.m_Edge];
				if ((m_PrefabGeometryData[prefabRef2.m_Prefab].m_MergeLayers & mergeLayers) != Layer.None)
				{
					Curve curve3 = m_CurveData[value2.m_Edge];
					if (value2.m_End)
					{
						curve3.m_Bezier = MathUtils.Invert(curve3.m_Bezier);
					}
					float2 y = math.normalizesafe(MathUtils.Position(curve3.m_Bezier, 0.5f).xz - @float);
					float num4;
					if (math.dot(float2, y) < 0f)
					{
						num4 = math.dot(x, y) * 0.5f;
					}
					else
					{
						float num5 = math.dot(x, y);
						num4 = math.select(-1f, 1f, num5 >= 0f) - num5 * 0.5f;
					}
					if (num4 > num2)
					{
						num2 = num4;
						leftEdge = value2.m_Edge;
					}
					if (num4 < num3)
					{
						num3 = num4;
						rightEdge = value2.m_Edge;
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct CreatedCompositionKey : IEquatable<CreatedCompositionKey>
	{
		public Entity m_Prefab;

		public CompositionFlags m_Flags;

		public bool Equals(CreatedCompositionKey other)
		{
			if (m_Prefab.Equals(other.m_Prefab))
			{
				return m_Flags == other.m_Flags;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return m_Prefab.GetHashCode() * 31 + m_Flags.GetHashCode();
		}
	}

	[BurstCompile]
	private struct CreateCompositionJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetTerrainData> m_PrefabTerrainData;

		[ReadOnly]
		public BufferLookup<SubReplacement> m_SubReplacements;

		[ReadOnly]
		public BufferLookup<NetGeometrySection> m_PrefabGeometrySections;

		[ReadOnly]
		public BufferLookup<NetSubSection> m_PrefabSubSections;

		[ReadOnly]
		public BufferLookup<NetSectionPiece> m_PrefabSectionPieces;

		public ComponentLookup<Upgraded> m_UpgradedData;

		public ComponentLookup<Temp> m_TempData;

		public NativeQueue<CompositionCreateInfo> m_CompositionCreateQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int count = m_CompositionCreateQueue.Count;
			if (count == 0)
			{
				return;
			}
			NativeParallelHashMap<CreatedCompositionKey, Entity> createdCompositions = default(NativeParallelHashMap<CreatedCompositionKey, Entity>);
			for (int i = 0; i < count; i++)
			{
				CompositionCreateInfo compositionCreateInfo = m_CompositionCreateQueue.Dequeue();
				if (compositionCreateInfo.m_CompositionData.m_Edge == Entity.Null)
				{
					compositionCreateInfo.m_CompositionData.m_Edge = GetOrCreateComposition(compositionCreateInfo.m_Prefab, compositionCreateInfo.m_EdgeFlags, ref createdCompositions);
				}
				if (m_EdgeData.HasComponent(compositionCreateInfo.m_Entity))
				{
					Edge edge = m_EdgeData[compositionCreateInfo.m_Entity];
					if (compositionCreateInfo.m_CompositionData.m_StartNode == Entity.Null)
					{
						compositionCreateInfo.m_CompositionData.m_StartNode = GetOrCreateComposition(compositionCreateInfo.m_Prefab, compositionCreateInfo.m_StartFlags, ref createdCompositions);
					}
					if (compositionCreateInfo.m_CompositionData.m_EndNode == Entity.Null)
					{
						compositionCreateInfo.m_CompositionData.m_EndNode = GetOrCreateComposition(compositionCreateInfo.m_Prefab, compositionCreateInfo.m_EndFlags, ref createdCompositions);
					}
					m_CommandBuffer.SetComponent(compositionCreateInfo.m_Entity, compositionCreateInfo.m_CompositionData);
					if (compositionCreateInfo.m_ObsoleteStartFlags != default(CompositionFlags))
					{
						Upgraded value = m_UpgradedData[edge.m_Start];
						if (value.m_Flags != default(CompositionFlags))
						{
							value.m_Flags &= ~compositionCreateInfo.m_ObsoleteStartFlags;
							m_UpgradedData[edge.m_Start] = value;
							if (value.m_Flags == default(CompositionFlags))
							{
								m_CommandBuffer.RemoveComponent<Upgraded>(edge.m_Start);
							}
							if (m_TempData.TryGetComponent(edge.m_Start, out var componentData) && componentData.m_Original != Entity.Null && (componentData.m_Flags & (TempFlags.Upgrade | TempFlags.Parent)) == (TempFlags.Upgrade | TempFlags.Parent))
							{
								m_UpgradedData.TryGetComponent(componentData.m_Original, out var componentData2);
								if (value.m_Flags == componentData2.m_Flags)
								{
									componentData.m_Flags &= ~(TempFlags.Upgrade | TempFlags.Parent);
									m_TempData[edge.m_Start] = componentData;
								}
							}
						}
					}
					if (compositionCreateInfo.m_ObsoleteEndFlags != default(CompositionFlags))
					{
						Upgraded value2 = m_UpgradedData[edge.m_End];
						if (value2.m_Flags != default(CompositionFlags))
						{
							value2.m_Flags &= ~compositionCreateInfo.m_ObsoleteEndFlags;
							m_UpgradedData[edge.m_End] = value2;
							if (value2.m_Flags == default(CompositionFlags))
							{
								m_CommandBuffer.RemoveComponent<Upgraded>(edge.m_End);
							}
							if (m_TempData.TryGetComponent(edge.m_End, out var componentData3) && componentData3.m_Original != Entity.Null && (componentData3.m_Flags & (TempFlags.Upgrade | TempFlags.Parent)) == (TempFlags.Upgrade | TempFlags.Parent))
							{
								m_UpgradedData.TryGetComponent(componentData3.m_Original, out var componentData4);
								if (value2.m_Flags == componentData4.m_Flags)
								{
									componentData3.m_Flags &= ~(TempFlags.Upgrade | TempFlags.Parent);
									m_TempData[edge.m_End] = componentData3;
								}
							}
						}
					}
				}
				else
				{
					m_CommandBuffer.SetComponent(compositionCreateInfo.m_Entity, new Orphan
					{
						m_Composition = compositionCreateInfo.m_CompositionData.m_Edge
					});
				}
				if (!(compositionCreateInfo.m_ObsoleteEdgeFlags != default(CompositionFlags)))
				{
					continue;
				}
				Upgraded value3 = m_UpgradedData[compositionCreateInfo.m_Entity];
				if (!(value3.m_Flags != default(CompositionFlags)))
				{
					continue;
				}
				value3.m_Flags &= ~compositionCreateInfo.m_ObsoleteEdgeFlags;
				m_UpgradedData[compositionCreateInfo.m_Entity] = value3;
				if (value3.m_Flags == default(CompositionFlags))
				{
					m_CommandBuffer.RemoveComponent<Upgraded>(compositionCreateInfo.m_Entity);
				}
				if (m_TempData.TryGetComponent(compositionCreateInfo.m_Entity, out var componentData5) && componentData5.m_Original != Entity.Null && (componentData5.m_Flags & (TempFlags.Upgrade | TempFlags.Parent)) == (TempFlags.Upgrade | TempFlags.Parent))
				{
					m_UpgradedData.TryGetComponent(componentData5.m_Original, out var componentData6);
					if (value3.m_Flags == componentData6.m_Flags && EqualSubReplacements(compositionCreateInfo.m_Entity, componentData5.m_Original))
					{
						componentData5.m_Flags &= ~(TempFlags.Upgrade | TempFlags.Parent);
						m_TempData[compositionCreateInfo.m_Entity] = componentData5;
					}
				}
			}
			if (createdCompositions.IsCreated)
			{
				createdCompositions.Dispose();
			}
		}

		private bool EqualSubReplacements(Entity entity1, Entity entity2)
		{
			DynamicBuffer<SubReplacement> bufferData;
			bool flag = m_SubReplacements.TryGetBuffer(entity1, out bufferData);
			DynamicBuffer<SubReplacement> bufferData2;
			bool flag2 = m_SubReplacements.TryGetBuffer(entity2, out bufferData2);
			if (flag != flag2)
			{
				return false;
			}
			if (!flag)
			{
				return true;
			}
			if (bufferData.Length != bufferData2.Length)
			{
				return false;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				SubReplacement subReplacement = bufferData[i];
				bool flag3 = false;
				for (int j = 0; j < bufferData2.Length; j++)
				{
					SubReplacement other = bufferData2[j];
					if (subReplacement.Equals(other))
					{
						flag3 = true;
						break;
					}
				}
				if (!flag3)
				{
					return false;
				}
			}
			return true;
		}

		private Entity GetOrCreateComposition(Entity prefab, CompositionFlags flags, ref NativeParallelHashMap<CreatedCompositionKey, Entity> createdCompositions)
		{
			CreatedCompositionKey key = new CreatedCompositionKey
			{
				m_Prefab = prefab,
				m_Flags = flags
			};
			if (createdCompositions.IsCreated)
			{
				if (createdCompositions.TryGetValue(key, out var item))
				{
					return item;
				}
			}
			else
			{
				createdCompositions = new NativeParallelHashMap<CreatedCompositionKey, Entity>(50, Allocator.Temp);
			}
			Entity entity = CreateComposition(prefab, flags);
			createdCompositions.Add(key, entity);
			return entity;
		}

		private Entity CreateComposition(Entity prefab, CompositionFlags mask)
		{
			NetGeometryData netGeometryData = m_PrefabGeometryData[prefab];
			DynamicBuffer<NetGeometrySection> dynamicBuffer = m_PrefabGeometrySections[prefab];
			Entity entity = (((mask.m_General & CompositionFlags.General.Node) == 0) ? m_CommandBuffer.CreateEntity(netGeometryData.m_EdgeCompositionArchetype) : m_CommandBuffer.CreateEntity(netGeometryData.m_NodeCompositionArchetype));
			m_CommandBuffer.AppendToBuffer(prefab, new NetGeometryComposition
			{
				m_Composition = entity,
				m_Mask = mask
			});
			m_CommandBuffer.SetComponent(entity, new PrefabRef(prefab));
			m_CommandBuffer.SetComponent(entity, new NetCompositionData
			{
				m_Flags = mask
			});
			DynamicBuffer<NetCompositionPiece> dynamicBuffer2 = m_CommandBuffer.SetBuffer<NetCompositionPiece>(entity);
			NativeList<NetCompositionPiece> resultBuffer = new NativeList<NetCompositionPiece>(32, Allocator.Temp);
			NetCompositionHelpers.GetCompositionPieces(resultBuffer, dynamicBuffer.AsNativeArray(), mask, m_PrefabSubSections, m_PrefabSectionPieces);
			dynamicBuffer2.CopyFrom(resultBuffer.AsArray());
			for (int i = 0; i < resultBuffer.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = resultBuffer[i];
				if (m_PrefabTerrainData.HasComponent(netCompositionPiece.m_Piece))
				{
					m_CommandBuffer.AddComponent(entity, default(TerrainComposition));
					break;
				}
			}
			resultBuffer.Dispose();
			return entity;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Upgraded> __Game_Net_Upgraded_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Fixed> __Game_Net_Fixed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Composition> __Game_Net_Composition_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Orphan> __Game_Net_Orphan_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Fixed> __Game_Net_Fixed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetObjectData> __Game_Prefabs_NetObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadData> __Game_Prefabs_RoadData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetGeometryComposition> __Game_Prefabs_NetGeometryComposition_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetGeometryEdgeState> __Game_Prefabs_NetGeometryEdgeState_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetGeometryNodeState> __Game_Prefabs_NetGeometryNodeState_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<FixedNetElement> __Game_Prefabs_FixedNetElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<NetTerrainData> __Game_Prefabs_NetTerrainData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubReplacement> __Game_Net_SubReplacement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetGeometrySection> __Game_Prefabs_NetGeometrySection_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetSubSection> __Game_Prefabs_NetSubSection_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetSectionPiece> __Game_Prefabs_NetSectionPiece_RO_BufferLookup;

		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RW_ComponentLookup;

		public ComponentLookup<Temp> __Game_Tools_Temp_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Upgraded>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Fixed>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_Composition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>();
			__Game_Net_Orphan_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Orphan>();
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentLookup = state.GetComponentLookup<Fixed>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetObjectData_RO_ComponentLookup = state.GetComponentLookup<NetObjectData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_RoadData_RO_ComponentLookup = state.GetComponentLookup<RoadData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Prefabs_NetGeometryComposition_RO_BufferLookup = state.GetBufferLookup<NetGeometryComposition>(isReadOnly: true);
			__Game_Prefabs_NetGeometryEdgeState_RO_BufferLookup = state.GetBufferLookup<NetGeometryEdgeState>(isReadOnly: true);
			__Game_Prefabs_NetGeometryNodeState_RO_BufferLookup = state.GetBufferLookup<NetGeometryNodeState>(isReadOnly: true);
			__Game_Prefabs_FixedNetElement_RO_BufferLookup = state.GetBufferLookup<FixedNetElement>(isReadOnly: true);
			__Game_Prefabs_NetTerrainData_RO_ComponentLookup = state.GetComponentLookup<NetTerrainData>(isReadOnly: true);
			__Game_Net_SubReplacement_RO_BufferLookup = state.GetBufferLookup<SubReplacement>(isReadOnly: true);
			__Game_Prefabs_NetGeometrySection_RO_BufferLookup = state.GetBufferLookup<NetGeometrySection>(isReadOnly: true);
			__Game_Prefabs_NetSubSection_RO_BufferLookup = state.GetBufferLookup<NetSubSection>(isReadOnly: true);
			__Game_Prefabs_NetSectionPiece_RO_BufferLookup = state.GetBufferLookup<NetSectionPiece>(isReadOnly: true);
			__Game_Net_Upgraded_RW_ComponentLookup = state.GetComponentLookup<Upgraded>();
			__Game_Tools_Temp_RW_ComponentLookup = state.GetComponentLookup<Temp>();
		}
	}

	private NetCompositionSystem m_NetCompositionSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ModificationBarrier3 m_ModificationBarrier;

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_AllQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NetCompositionSystem = base.World.GetOrCreateSystemManaged<NetCompositionSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier3>();
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadWrite<Composition>(),
				ComponentType.ReadWrite<Orphan>()
			}
		});
		m_AllQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadWrite<Composition>(),
				ComponentType.ReadWrite<Orphan>()
			}
		});
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery query = (GetLoaded() ? m_AllQuery : m_UpdatedQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			NativeQueue<CompositionCreateInfo> compositionCreateQueue = new NativeQueue<CompositionCreateInfo>(Allocator.TempJob);
			SelectCompositionJob jobData = new SelectCompositionJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UpgradedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FixedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OrphanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Orphan_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FixedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabGeometryCompositions = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryComposition_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabGeometryEdgeStates = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryEdgeState_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabGeometryNodeStates = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryNodeState_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabFixedNetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_FixedNetElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
				m_CompositionCreateQueue = compositionCreateQueue.AsParallelWriter()
			};
			CreateCompositionJob jobData2 = new CreateCompositionJob
			{
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTerrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetTerrainData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubReplacements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubReplacement_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabGeometrySections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetGeometrySection_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubSections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetSubSection_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSectionPieces = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetSectionPiece_RO_BufferLookup, ref base.CheckedStateRef),
				m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RW_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RW_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionCreateQueue = compositionCreateQueue,
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
			};
			JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(jobData, query, base.Dependency);
			JobHandle jobHandle = IJobExtensions.Schedule(jobData2, dependsOn);
			compositionCreateQueue.Dispose(jobHandle);
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
	public CompositionSelectSystem()
	{
	}
}
