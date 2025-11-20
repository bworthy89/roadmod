using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Serialization;
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
public class SearchSystem : GameSystemBase, IPreDeserialize
{
	[BurstCompile]
	private struct UpdateNetSearchTreeJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> m_StartGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> m_EndGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<NodeGeometry> m_NodeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public ComponentTypeHandle<Orphan> m_OrphanType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Marker> m_MarkerType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<CullingInfo> m_CullingInfoType;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshRef> m_PrefabCompositionMeshRef;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshData> m_PrefabCompositionMeshData;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_Loaded;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_DeletedType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity item = nativeArray[i];
					m_SearchTree.TryRemove(item);
				}
				return;
			}
			if (m_Loaded || chunk.Has(ref m_CreatedType))
			{
				NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
				bool flag = chunk.Has(ref m_CullingInfoType);
				NativeArray<EdgeGeometry> nativeArray3 = chunk.GetNativeArray(ref m_EdgeGeometryType);
				if (nativeArray3.Length != 0)
				{
					NativeArray<StartNodeGeometry> nativeArray4 = chunk.GetNativeArray(ref m_StartGeometryType);
					NativeArray<EndNodeGeometry> nativeArray5 = chunk.GetNativeArray(ref m_EndGeometryType);
					NativeArray<Composition> nativeArray6 = chunk.GetNativeArray(ref m_CompositionType);
					bool flag2 = chunk.Has(ref m_MarkerType);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						Entity item2 = nativeArray2[j];
						EdgeGeometry edgeGeometry = nativeArray3[j];
						EdgeNodeGeometry geometry = nativeArray4[j].m_Geometry;
						EdgeNodeGeometry geometry2 = nativeArray5[j].m_Geometry;
						Bounds3 bounds = edgeGeometry.m_Bounds | geometry.m_Bounds | geometry2.m_Bounds;
						Composition composition = nativeArray6[j];
						NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
						NetCompositionData netCompositionData2 = m_PrefabCompositionData[composition.m_StartNode];
						NetCompositionData netCompositionData3 = m_PrefabCompositionData[composition.m_EndNode];
						int lod = math.min(netCompositionData.m_MinLod, math.min(netCompositionData2.m_MinLod, netCompositionData3.m_MinLod));
						BoundsMask boundsMask = BoundsMask.Debug;
						if (!flag2 || m_EditorMode)
						{
							if (math.any(edgeGeometry.m_Start.m_Length + edgeGeometry.m_End.m_Length > 0.1f))
							{
								NetCompositionMeshRef netCompositionMeshRef = m_PrefabCompositionMeshRef[composition.m_Edge];
								if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef.m_Mesh, out var componentData))
								{
									boundsMask |= ((componentData.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData.m_DefaultLayers));
								}
							}
							if (math.any(geometry.m_Left.m_Length > 0.05f) | math.any(geometry.m_Right.m_Length > 0.05f))
							{
								NetCompositionMeshRef netCompositionMeshRef2 = m_PrefabCompositionMeshRef[composition.m_StartNode];
								if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef2.m_Mesh, out var componentData2))
								{
									boundsMask |= ((componentData2.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData2.m_DefaultLayers));
								}
							}
							if (math.any(geometry2.m_Left.m_Length > 0.05f) | math.any(geometry2.m_Right.m_Length > 0.05f))
							{
								NetCompositionMeshRef netCompositionMeshRef3 = m_PrefabCompositionMeshRef[composition.m_EndNode];
								if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef3.m_Mesh, out var componentData3))
								{
									boundsMask |= ((componentData3.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData3.m_DefaultLayers));
								}
							}
						}
						if (!flag)
						{
							boundsMask &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
						}
						m_SearchTree.Add(item2, new QuadTreeBoundsXZ(bounds, boundsMask, lod));
					}
					return;
				}
				NativeArray<NodeGeometry> nativeArray7 = chunk.GetNativeArray(ref m_NodeGeometryType);
				if (nativeArray7.Length != 0)
				{
					NativeArray<Orphan> nativeArray8 = chunk.GetNativeArray(ref m_OrphanType);
					bool flag3 = chunk.Has(ref m_MarkerType);
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						Entity item3 = nativeArray2[k];
						Bounds3 bounds2 = nativeArray7[k].m_Bounds;
						BoundsMask boundsMask2 = BoundsMask.Debug;
						int lod2;
						if (nativeArray8.Length != 0)
						{
							Orphan orphan = nativeArray8[k];
							lod2 = m_PrefabCompositionData[orphan.m_Composition].m_MinLod;
							if (!flag3 || m_EditorMode)
							{
								NetCompositionMeshRef netCompositionMeshRef4 = m_PrefabCompositionMeshRef[orphan.m_Composition];
								if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef4.m_Mesh, out var componentData4))
								{
									boundsMask2 |= ((componentData4.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData4.m_DefaultLayers));
								}
							}
						}
						else
						{
							lod2 = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(2f)));
						}
						if (!flag)
						{
							boundsMask2 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
						}
						m_SearchTree.Add(item3, new QuadTreeBoundsXZ(bounds2, boundsMask2, lod2));
					}
					return;
				}
				NativeArray<Node> nativeArray9 = chunk.GetNativeArray(ref m_NodeType);
				if (nativeArray9.Length != 0)
				{
					BoundsMask boundsMask3 = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
					if (!flag)
					{
						boundsMask3 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
					}
					for (int l = 0; l < nativeArray2.Length; l++)
					{
						Entity item4 = nativeArray2[l];
						Node node = nativeArray9[l];
						Bounds3 bounds3 = new Bounds3(node.m_Position - 1f, node.m_Position + 1f);
						int lod3 = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(2f)));
						m_SearchTree.Add(item4, new QuadTreeBoundsXZ(bounds3, boundsMask3, lod3));
					}
					return;
				}
				NativeArray<Curve> nativeArray10 = chunk.GetNativeArray(ref m_CurveType);
				if (nativeArray10.Length != 0)
				{
					BoundsMask boundsMask4 = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
					if (!flag)
					{
						boundsMask4 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
					}
					for (int m = 0; m < nativeArray2.Length; m++)
					{
						Entity item5 = nativeArray2[m];
						Bounds3 bounds4 = MathUtils.Expand(MathUtils.Bounds(nativeArray10[m].m_Bezier), 0.5f);
						int lod4 = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(2f)));
						m_SearchTree.Add(item5, new QuadTreeBoundsXZ(bounds4, boundsMask4, lod4));
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray11 = chunk.GetNativeArray(m_EntityType);
			bool flag4 = chunk.Has(ref m_CullingInfoType);
			NativeArray<EdgeGeometry> nativeArray12 = chunk.GetNativeArray(ref m_EdgeGeometryType);
			if (nativeArray12.Length != 0)
			{
				NativeArray<StartNodeGeometry> nativeArray13 = chunk.GetNativeArray(ref m_StartGeometryType);
				NativeArray<EndNodeGeometry> nativeArray14 = chunk.GetNativeArray(ref m_EndGeometryType);
				NativeArray<Composition> nativeArray15 = chunk.GetNativeArray(ref m_CompositionType);
				bool flag5 = chunk.Has(ref m_MarkerType);
				for (int n = 0; n < nativeArray11.Length; n++)
				{
					Entity item6 = nativeArray11[n];
					EdgeGeometry edgeGeometry2 = nativeArray12[n];
					EdgeNodeGeometry geometry3 = nativeArray13[n].m_Geometry;
					EdgeNodeGeometry geometry4 = nativeArray14[n].m_Geometry;
					Bounds3 bounds5 = edgeGeometry2.m_Bounds | geometry3.m_Bounds | geometry4.m_Bounds;
					Composition composition2 = nativeArray15[n];
					NetCompositionData netCompositionData4 = m_PrefabCompositionData[composition2.m_Edge];
					NetCompositionData netCompositionData5 = m_PrefabCompositionData[composition2.m_StartNode];
					NetCompositionData netCompositionData6 = m_PrefabCompositionData[composition2.m_EndNode];
					int lod5 = math.min(netCompositionData4.m_MinLod, math.min(netCompositionData5.m_MinLod, netCompositionData6.m_MinLod));
					BoundsMask boundsMask5 = BoundsMask.Debug;
					if (!flag5 || m_EditorMode)
					{
						if (math.any(edgeGeometry2.m_Start.m_Length + edgeGeometry2.m_End.m_Length > 0.1f))
						{
							NetCompositionMeshRef netCompositionMeshRef5 = m_PrefabCompositionMeshRef[composition2.m_Edge];
							if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef5.m_Mesh, out var componentData5))
							{
								boundsMask5 |= ((componentData5.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData5.m_DefaultLayers));
							}
						}
						if (math.any(geometry3.m_Left.m_Length > 0.05f) | math.any(geometry3.m_Right.m_Length > 0.05f))
						{
							NetCompositionMeshRef netCompositionMeshRef6 = m_PrefabCompositionMeshRef[composition2.m_StartNode];
							if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef6.m_Mesh, out var componentData6))
							{
								boundsMask5 |= ((componentData6.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData6.m_DefaultLayers));
							}
						}
						if (math.any(geometry4.m_Left.m_Length > 0.05f) | math.any(geometry4.m_Right.m_Length > 0.05f))
						{
							NetCompositionMeshRef netCompositionMeshRef7 = m_PrefabCompositionMeshRef[composition2.m_EndNode];
							if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef7.m_Mesh, out var componentData7))
							{
								boundsMask5 |= ((componentData7.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData7.m_DefaultLayers));
							}
						}
					}
					if (!flag4)
					{
						boundsMask5 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
					}
					m_SearchTree.Update(item6, new QuadTreeBoundsXZ(bounds5, boundsMask5, lod5));
				}
				return;
			}
			NativeArray<NodeGeometry> nativeArray16 = chunk.GetNativeArray(ref m_NodeGeometryType);
			if (nativeArray16.Length != 0)
			{
				NativeArray<Orphan> nativeArray17 = chunk.GetNativeArray(ref m_OrphanType);
				bool flag6 = chunk.Has(ref m_MarkerType);
				for (int num = 0; num < nativeArray11.Length; num++)
				{
					Entity item7 = nativeArray11[num];
					Bounds3 bounds6 = nativeArray16[num].m_Bounds;
					BoundsMask boundsMask6 = BoundsMask.Debug;
					int lod6;
					if (nativeArray17.Length != 0)
					{
						Orphan orphan2 = nativeArray17[num];
						lod6 = m_PrefabCompositionData[orphan2.m_Composition].m_MinLod;
						if (!flag6 || m_EditorMode)
						{
							NetCompositionMeshRef netCompositionMeshRef8 = m_PrefabCompositionMeshRef[orphan2.m_Composition];
							if (m_PrefabCompositionMeshData.TryGetComponent(netCompositionMeshRef8.m_Mesh, out var componentData8))
							{
								boundsMask6 |= ((componentData8.m_DefaultLayers == (MeshLayer)0) ? BoundsMask.NormalLayers : CommonUtils.GetBoundsMask(componentData8.m_DefaultLayers));
							}
						}
					}
					else
					{
						lod6 = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(2f)));
					}
					if (!flag4)
					{
						boundsMask6 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
					}
					m_SearchTree.Update(item7, new QuadTreeBoundsXZ(bounds6, boundsMask6, lod6));
				}
				return;
			}
			NativeArray<Node> nativeArray18 = chunk.GetNativeArray(ref m_NodeType);
			if (nativeArray18.Length != 0)
			{
				BoundsMask boundsMask7 = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
				if (!flag4)
				{
					boundsMask7 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
				}
				for (int num2 = 0; num2 < nativeArray11.Length; num2++)
				{
					Entity item8 = nativeArray11[num2];
					Node node2 = nativeArray18[num2];
					Bounds3 bounds7 = new Bounds3(node2.m_Position - 1f, node2.m_Position + 1f);
					int lod7 = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(2f)));
					m_SearchTree.Update(item8, new QuadTreeBoundsXZ(bounds7, boundsMask7, lod7));
				}
				return;
			}
			NativeArray<Curve> nativeArray19 = chunk.GetNativeArray(ref m_CurveType);
			if (nativeArray19.Length != 0)
			{
				BoundsMask boundsMask8 = ((!m_EditorMode) ? BoundsMask.Debug : (BoundsMask.Debug | BoundsMask.NormalLayers));
				if (!flag4)
				{
					boundsMask8 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
				}
				for (int num3 = 0; num3 < nativeArray11.Length; num3++)
				{
					Entity item9 = nativeArray11[num3];
					Bounds3 bounds8 = MathUtils.Expand(MathUtils.Bounds(nativeArray19[num3].m_Bezier), 0.5f);
					int lod8 = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(2f)));
					m_SearchTree.Update(item9, new QuadTreeBoundsXZ(bounds8, boundsMask8, lod8));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	public struct UpdateLaneSearchTreeJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Overridden> m_OverriddenType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<UtilityLane> m_UtilityLaneType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> m_PrefabLaneGeometryData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_Loaded;

		[ReadOnly]
		public UtilityTypes m_DilatedUtilityTypes;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_DeletedType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity item = nativeArray[i];
					m_SearchTree.TryRemove(item);
				}
				return;
			}
			if (m_Loaded || chunk.Has(ref m_CreatedType))
			{
				NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
				NativeArray<Curve> nativeArray3 = chunk.GetNativeArray(ref m_CurveType);
				NativeArray<Owner> nativeArray4 = chunk.GetNativeArray(ref m_OwnerType);
				NativeArray<UtilityLane> nativeArray5 = chunk.GetNativeArray(ref m_UtilityLaneType);
				NativeArray<PrefabRef> nativeArray6 = chunk.GetNativeArray(ref m_PrefabRefType);
				bool flag = chunk.Has(ref m_OverriddenType);
				bool flag2 = chunk.Has<CullingInfo>();
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity item2 = nativeArray2[j];
					Curve curve = nativeArray3[j];
					PrefabRef prefabRef = nativeArray6[j];
					Bounds3 bounds = MathUtils.Bounds(curve.m_Bezier);
					if (m_PrefabLaneGeometryData.HasComponent(prefabRef.m_Prefab))
					{
						NetLaneGeometryData netLaneGeometryData = m_PrefabLaneGeometryData[prefabRef.m_Prefab];
						bounds = MathUtils.Expand(bounds, netLaneGeometryData.m_Size.xyx * 0.5f);
						BoundsMask boundsMask = BoundsMask.Debug;
						if (!flag)
						{
							boundsMask |= BoundsMask.NotOverridden;
							if (curve.m_Length > 0.1f)
							{
								MeshLayer defaultLayers = (m_EditorMode ? netLaneGeometryData.m_EditorLayers : netLaneGeometryData.m_GameLayers);
								CollectionUtils.TryGet(nativeArray4, j, out var value);
								CollectionUtils.TryGet(nativeArray5, j, out var value2);
								boundsMask |= CommonUtils.GetBoundsMask(GetLayers(value, value2, defaultLayers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData));
							}
						}
						int num = netLaneGeometryData.m_MinLod;
						if (m_PrefabUtilityLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_UtilityTypes & m_DilatedUtilityTypes) != UtilityTypes.None)
						{
							float renderingSize = RenderingUtils.GetRenderingSize(new float2(componentData.m_VisualCapacity));
							num = math.min(num, RenderingUtils.CalculateLodLimit(renderingSize));
						}
						if (!flag2)
						{
							boundsMask &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
						}
						m_SearchTree.Add(item2, new QuadTreeBoundsXZ(bounds, boundsMask, num));
					}
					else
					{
						bounds = MathUtils.Expand(bounds, 0.5f);
						int lod = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(1f)));
						BoundsMask boundsMask2 = BoundsMask.Debug;
						if (!flag2)
						{
							boundsMask2 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
						}
						m_SearchTree.Add(item2, new QuadTreeBoundsXZ(bounds, boundsMask2, lod));
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
			NativeArray<Curve> nativeArray8 = chunk.GetNativeArray(ref m_CurveType);
			NativeArray<Owner> nativeArray9 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<UtilityLane> nativeArray10 = chunk.GetNativeArray(ref m_UtilityLaneType);
			NativeArray<PrefabRef> nativeArray11 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool flag3 = chunk.Has(ref m_OverriddenType);
			bool flag4 = chunk.Has<CullingInfo>();
			for (int k = 0; k < nativeArray7.Length; k++)
			{
				Entity item3 = nativeArray7[k];
				Curve curve2 = nativeArray8[k];
				PrefabRef prefabRef2 = nativeArray11[k];
				Bounds3 bounds2 = MathUtils.Bounds(curve2.m_Bezier);
				if (m_PrefabLaneGeometryData.HasComponent(prefabRef2.m_Prefab))
				{
					NetLaneGeometryData netLaneGeometryData2 = m_PrefabLaneGeometryData[prefabRef2.m_Prefab];
					bounds2 = MathUtils.Expand(bounds2, netLaneGeometryData2.m_Size.xyx * 0.5f);
					BoundsMask boundsMask3 = BoundsMask.Debug;
					if (!flag3)
					{
						boundsMask3 |= BoundsMask.NotOverridden;
						if (curve2.m_Length > 0.1f)
						{
							MeshLayer defaultLayers2 = (m_EditorMode ? netLaneGeometryData2.m_EditorLayers : netLaneGeometryData2.m_GameLayers);
							CollectionUtils.TryGet(nativeArray9, k, out var value3);
							CollectionUtils.TryGet(nativeArray10, k, out var value4);
							boundsMask3 |= CommonUtils.GetBoundsMask(GetLayers(value3, value4, defaultLayers2, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData));
						}
					}
					int num2 = netLaneGeometryData2.m_MinLod;
					if (m_PrefabUtilityLaneData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2) && (componentData2.m_UtilityTypes & m_DilatedUtilityTypes) != UtilityTypes.None)
					{
						float renderingSize2 = RenderingUtils.GetRenderingSize(new float2(componentData2.m_VisualCapacity));
						num2 = math.min(num2, RenderingUtils.CalculateLodLimit(renderingSize2));
					}
					if (!flag4)
					{
						boundsMask3 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
					}
					m_SearchTree.Update(item3, new QuadTreeBoundsXZ(bounds2, boundsMask3, num2));
				}
				else
				{
					bounds2 = MathUtils.Expand(bounds2, 0.5f);
					int lod2 = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(1f)));
					BoundsMask boundsMask4 = BoundsMask.Debug;
					if (!flag4)
					{
						boundsMask4 &= ~(BoundsMask.AllLayers | BoundsMask.Debug);
					}
					m_SearchTree.Update(item3, new QuadTreeBoundsXZ(bounds2, boundsMask4, lod2));
				}
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
		public ComponentTypeHandle<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Orphan> __Game_Net_Orphan_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Marker> __Game_Net_Marker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshRef> __Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshData> __Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Overridden> __Game_Common_Overridden_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<UtilityLane> __Game_Net_UtilityLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> __Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_EdgeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_NodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NodeGeometry>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Orphan>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_Marker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Marker>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Rendering_CullingInfo_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CullingInfo>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup = state.GetComponentLookup<NetCompositionMeshRef>(isReadOnly: true);
			__Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionMeshData>(isReadOnly: true);
			__Game_Common_Overridden_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Overridden>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_UtilityLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<UtilityLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetLaneGeometryData>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private UndergroundViewSystem m_UndergroundViewSystem;

	private EntityQuery m_UpdatedNetsQuery;

	private EntityQuery m_UpdatedLanesQuery;

	private EntityQuery m_AllNetsQuery;

	private EntityQuery m_AllLanesQuery;

	private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

	private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_LaneSearchTree;

	private JobHandle m_NetReadDependencies;

	private JobHandle m_NetWriteDependencies;

	private JobHandle m_LaneReadDependencies;

	private JobHandle m_LaneWriteDependencies;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_UndergroundViewSystem = base.World.GetOrCreateSystemManaged<UndergroundViewSystem>();
		m_UpdatedNetsQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Edge>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Node>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UpdatedLanesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<LaneGeometry>(),
				ComponentType.ReadOnly<ParkingLane>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<LaneGeometry>(),
				ComponentType.ReadOnly<ParkingLane>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_AllNetsQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Node>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_AllLanesQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<LaneGeometry>(),
				ComponentType.ReadOnly<ParkingLane>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_NetSearchTree = new NativeQuadTree<Entity, QuadTreeBoundsXZ>(1f, Allocator.Persistent);
		m_LaneSearchTree = new NativeQuadTree<Entity, QuadTreeBoundsXZ>(1f, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_NetSearchTree.Dispose();
		m_LaneSearchTree.Dispose();
		base.OnDestroy();
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
		bool loaded = GetLoaded();
		EntityQuery query = (loaded ? m_AllNetsQuery : m_UpdatedNetsQuery);
		EntityQuery query2 = (loaded ? m_AllLanesQuery : m_UpdatedLanesQuery);
		bool flag = !query.IsEmptyIgnoreFilter;
		bool flag2 = !query2.IsEmptyIgnoreFilter;
		if (flag || flag2)
		{
			JobHandle jobHandle = default(JobHandle);
			if (flag)
			{
				JobHandle dependencies;
				JobHandle jobHandle2 = JobChunkExtensions.Schedule(new UpdateNetSearchTreeJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_StartGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_EndGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_NodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OrphanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_MarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Marker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabCompositionMeshRef = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabCompositionMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
					m_Loaded = loaded,
					m_SearchTree = GetNetSearchTree(readOnly: false, out dependencies)
				}, query, JobHandle.CombineDependencies(base.Dependency, dependencies));
				AddNetSearchTreeWriter(jobHandle2);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
			if (flag2)
			{
				JobHandle dependencies2;
				JobHandle jobHandle3 = JobChunkExtensions.Schedule(new UpdateLaneSearchTreeJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OverriddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Overridden_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_UtilityLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_UtilityLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabLaneGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
					m_Loaded = loaded,
					m_DilatedUtilityTypes = m_UndergroundViewSystem.utilityTypes,
					m_SearchTree = GetLaneSearchTree(readOnly: false, out dependencies2)
				}, query2, JobHandle.CombineDependencies(base.Dependency, dependencies2));
				AddLaneSearchTreeWriter(jobHandle3);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
			}
			base.Dependency = jobHandle;
		}
	}

	public NativeQuadTree<Entity, QuadTreeBoundsXZ> GetNetSearchTree(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_NetWriteDependencies : JobHandle.CombineDependencies(m_NetReadDependencies, m_NetWriteDependencies));
		return m_NetSearchTree;
	}

	public NativeQuadTree<Entity, QuadTreeBoundsXZ> GetLaneSearchTree(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_LaneWriteDependencies : JobHandle.CombineDependencies(m_LaneReadDependencies, m_LaneWriteDependencies));
		return m_LaneSearchTree;
	}

	public void AddNetSearchTreeReader(JobHandle jobHandle)
	{
		m_NetReadDependencies = JobHandle.CombineDependencies(m_NetReadDependencies, jobHandle);
	}

	public void AddNetSearchTreeWriter(JobHandle jobHandle)
	{
		m_NetWriteDependencies = jobHandle;
	}

	public void AddLaneSearchTreeReader(JobHandle jobHandle)
	{
		m_LaneReadDependencies = JobHandle.CombineDependencies(m_LaneReadDependencies, jobHandle);
	}

	public void AddLaneSearchTreeWriter(JobHandle jobHandle)
	{
		m_LaneWriteDependencies = jobHandle;
	}

	public void PreDeserialize(Context context)
	{
		JobHandle dependencies;
		NativeQuadTree<Entity, QuadTreeBoundsXZ> netSearchTree = GetNetSearchTree(readOnly: false, out dependencies);
		JobHandle dependencies2;
		NativeQuadTree<Entity, QuadTreeBoundsXZ> laneSearchTree = GetLaneSearchTree(readOnly: false, out dependencies2);
		dependencies.Complete();
		dependencies2.Complete();
		netSearchTree.Clear();
		laneSearchTree.Clear();
		m_Loaded = true;
	}

	public static MeshLayer GetLayers(Owner owner, UtilityLane utilityLane, MeshLayer defaultLayers, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<NetData> netDatas, ref ComponentLookup<NetGeometryData> netGeometryDatas)
	{
		if (defaultLayers == (MeshLayer.Pipeline | MeshLayer.SubPipeline))
		{
			if ((owner.m_Owner != Entity.Null && IsNetOwnerPipeline(owner, ref prefabRefs, ref netDatas, ref netGeometryDatas)) || (utilityLane.m_Flags & UtilityLaneFlags.PipelineConnection) != 0)
			{
				return MeshLayer.Pipeline;
			}
			return MeshLayer.SubPipeline;
		}
		return defaultLayers;
	}

	public static bool IsNetOwnerPipeline(Owner owner, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<NetData> netDatas, ref ComponentLookup<NetGeometryData> netGeometryDatas)
	{
		if (prefabRefs.TryGetComponent(owner.m_Owner, out var componentData) && netDatas.TryGetComponent(componentData.m_Prefab, out var componentData2) && netGeometryDatas.TryGetComponent(componentData.m_Prefab, out var componentData3))
		{
			if ((componentData2.m_RequiredLayers & (Layer.PowerlineLow | Layer.PowerlineHigh | Layer.WaterPipe | Layer.SewagePipe)) != Layer.None)
			{
				return (componentData3.m_Flags & GeometryFlags.Marker) == 0;
			}
			return false;
		}
		return false;
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
	public SearchSystem()
	{
	}
}
