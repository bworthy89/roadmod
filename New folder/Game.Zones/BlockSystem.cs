using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
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

namespace Game.Zones;

[CompilerGenerated]
public class BlockSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateBlocksJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.BuildOrder> m_BuildOrderType;

		[ReadOnly]
		public ComponentTypeHandle<Road> m_RoadType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public BufferTypeHandle<SubBlock> m_SubBlockType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Game.Net.BuildOrder> m_BuildOrderData;

		[ReadOnly]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<RoadComposition> m_RoadCompositionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<ZoneBlockData> m_ZoneBlockData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_DeletedType))
			{
				BufferAccessor<SubBlock> bufferAccessor = chunk.GetBufferAccessor(ref m_SubBlockType);
				for (int i = 0; i < bufferAccessor.Length; i++)
				{
					DynamicBuffer<SubBlock> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity subBlock = dynamicBuffer[j].m_SubBlock;
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, subBlock, default(Deleted));
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Edge> nativeArray3 = chunk.GetNativeArray(ref m_EdgeType);
			NativeArray<Curve> nativeArray4 = chunk.GetNativeArray(ref m_CurveType);
			NativeArray<Composition> nativeArray5 = chunk.GetNativeArray(ref m_CompositionType);
			NativeArray<Game.Net.BuildOrder> nativeArray6 = chunk.GetNativeArray(ref m_BuildOrderType);
			NativeArray<Road> nativeArray7 = chunk.GetNativeArray(ref m_RoadType);
			BufferAccessor<SubBlock> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubBlockType);
			NativeParallelHashMap<Block, Entity> oldBlockBuffer = new NativeParallelHashMap<Block, Entity>(32, Allocator.Temp);
			for (int k = 0; k < nativeArray.Length; k++)
			{
				Entity owner = nativeArray[k];
				Edge edge = nativeArray3[k];
				Curve curve = nativeArray4[k];
				Composition composition = nativeArray5[k];
				Game.Net.BuildOrder buildOrder = nativeArray6[k];
				Road road = nativeArray7[k];
				DynamicBuffer<SubBlock> blocks = bufferAccessor2[k];
				CollectionUtils.TryGet(nativeArray2, k, out var value);
				FillOldBlockBuffer(blocks, oldBlockBuffer);
				CreateBlocks(unfilteredChunkIndex, owner, oldBlockBuffer, value, composition, edge, curve, buildOrder, road);
				RemoveUnusedOldBlocks(unfilteredChunkIndex, blocks, oldBlockBuffer);
				oldBlockBuffer.Clear();
			}
			oldBlockBuffer.Dispose();
		}

		private void FillOldBlockBuffer(DynamicBuffer<SubBlock> blocks, NativeParallelHashMap<Block, Entity> oldBlockBuffer)
		{
			for (int i = 0; i < blocks.Length; i++)
			{
				Entity subBlock = blocks[i].m_SubBlock;
				Block key = m_BlockData[subBlock];
				oldBlockBuffer.TryAdd(key, subBlock);
			}
		}

		private void RemoveUnusedOldBlocks(int jobIndex, DynamicBuffer<SubBlock> blocks, NativeParallelHashMap<Block, Entity> oldBlockBuffer)
		{
			for (int i = 0; i < blocks.Length; i++)
			{
				Entity subBlock = blocks[i].m_SubBlock;
				Block key = m_BlockData[subBlock];
				if (oldBlockBuffer.TryGetValue(key, out var _))
				{
					m_CommandBuffer.AddComponent(jobIndex, subBlock, default(Deleted));
					oldBlockBuffer.Remove(key);
				}
			}
		}

		private void CreateBlocks(int jobIndex, Entity owner, NativeParallelHashMap<Block, Entity> oldBlockBuffer, Owner ownerOwner, Composition composition, Edge edge, Curve curve, Game.Net.BuildOrder buildOrder, Road road)
		{
			if (!m_RoadCompositionData.TryGetComponent(composition.m_Edge, out var componentData) || (componentData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) == 0)
			{
				return;
			}
			while (ownerOwner.m_Owner != Entity.Null)
			{
				if (m_BuildingData.HasComponent(ownerOwner.m_Owner))
				{
					return;
				}
				m_OwnerData.TryGetComponent(ownerOwner.m_Owner, out var componentData2);
				ownerOwner = componentData2;
			}
			NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
			if ((netCompositionData.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) != 0)
			{
				return;
			}
			bool flag = (netCompositionData.m_Flags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0;
			bool flag2 = (netCompositionData.m_Flags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0;
			if (!flag && !flag2)
			{
				return;
			}
			uint buildOrder2 = math.max(buildOrder.m_Start, buildOrder.m_End);
			bool flag3 = (road.m_Flags & Game.Net.RoadFlags.StartHalfAligned) != 0;
			bool flag4 = (road.m_Flags & Game.Net.RoadFlags.EndHalfAligned) != 0;
			if (flag)
			{
				int cellWidth = ZoneUtils.GetCellWidth(netCompositionData.m_Width - netCompositionData.m_MiddleOffset * 2f);
				CreateBlocks(jobIndex, owner, edge.m_Start, edge.m_End, oldBlockBuffer, componentData.m_ZoneBlockPrefab, curve.m_Bezier, cellWidth, buildOrder.m_Start, buildOrder.m_End, flag3, flag4, invert: false);
			}
			if (flag2)
			{
				int cellWidth2 = ZoneUtils.GetCellWidth(netCompositionData.m_Width + netCompositionData.m_MiddleOffset * 2f);
				CreateBlocks(jobIndex, owner, edge.m_End, edge.m_Start, oldBlockBuffer, componentData.m_ZoneBlockPrefab, MathUtils.Invert(curve.m_Bezier), cellWidth2, buildOrder.m_End, buildOrder.m_Start, flag4, flag3, invert: true);
			}
			if ((m_PrefabCompositionData[composition.m_StartNode].m_Flags.m_General & CompositionFlags.General.Roundabout) != 0)
			{
				if (flag)
				{
					CreateBlocks(jobIndex, owner, edge.m_Start, oldBlockBuffer, componentData.m_ZoneBlockPrefab, buildOrder2, start: true, right: false);
				}
				if (flag2)
				{
					CreateBlocks(jobIndex, owner, edge.m_Start, oldBlockBuffer, componentData.m_ZoneBlockPrefab, buildOrder2, start: true, right: true);
				}
			}
			if ((m_PrefabCompositionData[composition.m_EndNode].m_Flags.m_General & CompositionFlags.General.Roundabout) != 0)
			{
				if (flag2)
				{
					CreateBlocks(jobIndex, owner, edge.m_End, oldBlockBuffer, componentData.m_ZoneBlockPrefab, buildOrder2, start: false, right: false);
				}
				if (flag)
				{
					CreateBlocks(jobIndex, owner, edge.m_End, oldBlockBuffer, componentData.m_ZoneBlockPrefab, buildOrder2, start: false, right: true);
				}
			}
		}

		private void CreateBlocks(int jobIndex, Entity owner, Entity node, NativeParallelHashMap<Block, Entity> oldBlockBuffer, Entity blockPrefab, uint buildOrder, bool start, bool right)
		{
			EdgeNodeGeometry edgeNodeGeometry = ((!start) ? m_EndNodeGeometryData[owner].m_Geometry : m_StartNodeGeometryData[owner].m_Geometry);
			Bezier4x3 curve;
			Bezier4x3 curve2;
			Bezier4x3 curve3;
			Bezier4x3 curve4;
			if (right)
			{
				curve = edgeNodeGeometry.m_Left.m_Right;
				curve2 = edgeNodeGeometry.m_Right.m_Right;
				curve3 = edgeNodeGeometry.m_Right.m_Left;
				curve4 = edgeNodeGeometry.m_Left.m_Left;
			}
			else
			{
				curve = edgeNodeGeometry.m_Left.m_Right;
				curve2 = edgeNodeGeometry.m_Right.m_Right;
				curve3 = edgeNodeGeometry.m_Right.m_Left;
				curve4 = edgeNodeGeometry.m_Left.m_Left;
			}
			float num = float.MaxValue;
			Entity entity = owner;
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				EdgeNodeGeometry geometry;
				if (edge2.m_Start == node)
				{
					geometry = m_StartNodeGeometryData[edge].m_Geometry;
				}
				else
				{
					if (!(edge2.m_End == node))
					{
						continue;
					}
					geometry = m_EndNodeGeometryData[edge].m_Geometry;
				}
				float num2 = ((!right) ? math.distancesq(geometry.m_Right.m_Right.d, edgeNodeGeometry.m_Right.m_Left.d) : math.distancesq(geometry.m_Right.m_Left.d, edgeNodeGeometry.m_Right.m_Right.d));
				if (num2 < num)
				{
					if (right)
					{
						curve4 = geometry.m_Left.m_Left;
						curve3 = geometry.m_Right.m_Left;
					}
					else
					{
						curve = geometry.m_Left.m_Right;
						curve2 = geometry.m_Right.m_Right;
					}
					num = num2;
					entity = edge;
				}
			}
			if (m_BuildOrderData.TryGetComponent(entity, out var componentData))
			{
				buildOrder = math.max(buildOrder, math.max(componentData.m_Start, componentData.m_End));
			}
			bool flag = false;
			if (m_CompositionData.TryGetComponent(entity, out var componentData2))
			{
				flag = !m_RoadCompositionData.TryGetComponent(componentData2.m_Edge, out var componentData3) || (componentData3.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) == 0;
				NetCompositionData netCompositionData = m_PrefabCompositionData[componentData2.m_Edge];
				flag |= (netCompositionData.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) != 0;
				flag = ((!right) ? (flag | ((netCompositionData.m_Flags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) != 0)) : (flag | ((netCompositionData.m_Flags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) != 0)));
			}
			CutStart(ref curve, ref curve2, !right && flag);
			CutStart(ref curve4, ref curve3, right && flag);
			curve3 = MathUtils.Invert(curve3);
			curve4 = MathUtils.Invert(curve4);
			float4 @float = new float4(MathUtils.Length(curve.xz), MathUtils.Length(curve2.xz), MathUtils.Length(curve3.xz), MathUtils.Length(curve4.xz));
			float num3 = math.csum(@float);
			if (num3 < 8f)
			{
				return;
			}
			float2 value = MathUtils.StartTangent(curve2).xz;
			float2 value2 = MathUtils.EndTangent(curve3).xz;
			if (!MathUtils.TryNormalize(ref value) || !MathUtils.TryNormalize(ref value2))
			{
				return;
			}
			int num4 = (int)math.floor(num3 / 8f);
			int baseWidth = 0;
			int middleWidth = 0;
			int splitCount = 0;
			if (num4 <= 1)
			{
				return;
			}
			if (num4 <= 3)
			{
				middleWidth = num4;
				splitCount = 1;
			}
			else if (num4 <= 5)
			{
				baseWidth = 2;
				splitCount = 2;
			}
			else if (num4 <= 7)
			{
				baseWidth = 2;
				middleWidth = num4 - 4;
				splitCount = 3;
			}
			else
			{
				TryOption(ref baseWidth, ref middleWidth, ref splitCount, num4, 3, 3);
				TryOption(ref baseWidth, ref middleWidth, ref splitCount, num4, 3, 0);
				TryOption(ref baseWidth, ref middleWidth, ref splitCount, num4, 2, 2);
				TryOption(ref baseWidth, ref middleWidth, ref splitCount, num4, 2, 0);
				TryOption(ref baseWidth, ref middleWidth, ref splitCount, num4, 3, 2);
				TryOption(ref baseWidth, ref middleWidth, ref splitCount, num4, 2, 3);
			}
			int num5 = math.select(splitCount >> 1, 0, flag || right);
			int num6 = math.select(splitCount >> 1, splitCount, flag || !right);
			if (num5 >= num6)
			{
				return;
			}
			num4 = middleWidth + baseWidth * (splitCount & -2);
			float num7 = (num3 - (float)num4 * 8f) * 0.5f;
			if (num7 > 0f)
			{
				Bounds1 t = new Bounds1(0f, 1f);
				Bezier4x3 output;
				if (MathUtils.ClampLength(curve.xz, ref t, num7))
				{
					MathUtils.Divide(curve, out output, out curve, t.max);
					@float.x = math.max(0f, @float.x - num7);
				}
				else
				{
					float num8 = math.max(0f, num7 - @float.x);
					if (MathUtils.ClampLength(curve2.xz, ref t, num8))
					{
						MathUtils.Divide(curve2, out output, out curve2, t.max);
						@float.y = math.max(0f, @float.y - num8);
					}
					else
					{
						curve2.a = (curve2.b = (curve2.c = (curve2.d = curve3.a)));
						@float.y = 0f;
					}
					curve.a = (curve.b = (curve.c = (curve.d = curve2.a)));
					@float.x = 0f;
				}
				t = new Bounds1(0f, 1f);
				if (MathUtils.ClampLengthInverse(curve4.xz, ref t, num7))
				{
					MathUtils.Divide(curve4, out curve4, out output, t.min);
					@float.w = math.max(0f, @float.w - num7);
				}
				else
				{
					float num9 = math.max(0f, num7 - @float.w);
					if (MathUtils.ClampLengthInverse(curve3.xz, ref t, num9))
					{
						MathUtils.Divide(curve3, out curve3, out output, t.min);
						@float.z = math.max(0f, @float.z - num9);
					}
					else
					{
						curve3.a = (curve3.b = (curve3.c = (curve3.d = curve2.d)));
						@float.z = 0f;
					}
					curve4.a = (curve4.b = (curve4.c = (curve4.d = curve3.d)));
					@float.w = 0f;
				}
				num3 = math.csum(@float);
			}
			BuildOrder component = new BuildOrder
			{
				m_Order = buildOrder
			};
			CurvePosition component2 = new CurvePosition
			{
				m_CurvePosition = math.select(1f, 0f, start)
			};
			for (int j = num5; j < num6; j++)
			{
				int num10 = math.select(baseWidth, middleWidth, j == splitCount >> 1 && middleWidth > 0);
				int2 @int = new int2(j, j + 1);
				@int = @int * baseWidth + math.select(0, middleWidth - baseWidth, (@int > splitCount >> 1) & (middleWidth > 0));
				float2 float2 = new float2((float)@int.x / (float)num4, (float)@int.y / (float)num4) * num3;
				CutCurves(curve, curve2, curve3, curve4, @float, float2, out var curve1B, out var curve2B, out var curve3B, out var curve4B);
				value = ((!(math.distancesq(curve1B.a, curve1B.d) < 0.01f)) ? MathUtils.StartTangent(curve1B).xz : ((!(math.distancesq(curve2B.a, curve2B.d) < 0.01f)) ? MathUtils.StartTangent(curve2B).xz : ((!(math.distancesq(curve3B.a, curve3B.d) < 0.01f)) ? MathUtils.StartTangent(curve3B).xz : MathUtils.StartTangent(curve4B).xz)));
				value2 = ((!(math.distancesq(curve4B.a, curve4B.d) < 0.01f)) ? MathUtils.EndTangent(curve4B).xz : ((!(math.distancesq(curve3B.a, curve3B.d) < 0.01f)) ? MathUtils.EndTangent(curve3B).xz : ((!(math.distancesq(curve2B.a, curve2B.d) < 0.01f)) ? MathUtils.EndTangent(curve2B).xz : MathUtils.EndTangent(curve1B).xz)));
				if (!MathUtils.TryNormalize(ref value) || !MathUtils.TryNormalize(ref value2))
				{
					continue;
				}
				float2 float3 = MathUtils.Right(value);
				float2 float4 = MathUtils.Right(value2);
				float3 a = curve1B.a;
				float3 d = curve4B.d;
				float2 value3 = d.xz - a.xz;
				if (!MathUtils.TryNormalize(ref value3))
				{
					continue;
				}
				float2 float5 = MathUtils.Right(value3);
				float num11 = math.max(math.max(MathUtils.MaxDot(curve1B.xz, float5, out var t2), MathUtils.MaxDot(curve2B.xz, float5, out t2)), math.max(MathUtils.MaxDot(curve3B.xz, float5, out t2), MathUtils.MaxDot(curve4B.xz, float5, out t2)));
				num11 -= math.dot(a.xz, float5);
				float upperBound = math.distance(a.xz, d.xz);
				a.xz += float3 * math.clamp(num11 / math.dot(float3, float5), 0f, upperBound);
				d.xz += float4 * math.clamp(num11 / math.dot(float4, float5), 0f, upperBound);
				float3 float6 = math.lerp(a, d, 0.5f);
				float2 float7 = value3 * ((float)num10 * 4f);
				a = float6;
				a.xz -= float7;
				d = float6;
				d.xz += float7;
				float2 cutRange = float2;
				if (MathUtils.Intersect(curve1B.xz, new Line2.Segment(a.xz, a.xz - float5 * 48f), out var t3, 4))
				{
					cutRange.x = math.max(cutRange.x, t3.x * @float.x);
				}
				if (MathUtils.Intersect(curve2B.xz, new Line2.Segment(a.xz, a.xz - float5 * 48f), out t3, 4))
				{
					cutRange.x = math.max(cutRange.x, @float.x + t3.x * @float.y);
				}
				if (MathUtils.Intersect(curve3B.xz, new Line2.Segment(a.xz, a.xz - float5 * 48f), out t3, 4))
				{
					cutRange.x = math.max(cutRange.x, @float.x + @float.y + t3.x * @float.z);
				}
				if (MathUtils.Intersect(curve4B.xz, new Line2.Segment(a.xz, a.xz - float5 * 48f), out t3, 4))
				{
					cutRange.x = math.max(cutRange.x, @float.x + @float.y + @float.z + t3.x * @float.w);
				}
				if (MathUtils.Intersect(curve1B.xz, new Line2.Segment(d.xz, d.xz - float5 * 48f), out t3, 4))
				{
					cutRange.y = math.min(cutRange.y, t3.x * @float.x);
				}
				if (MathUtils.Intersect(curve2B.xz, new Line2.Segment(d.xz, d.xz - float5 * 48f), out t3, 4))
				{
					cutRange.y = math.min(cutRange.y, @float.x + t3.x * @float.y);
				}
				if (MathUtils.Intersect(curve3B.xz, new Line2.Segment(d.xz, d.xz - float5 * 48f), out t3, 4))
				{
					cutRange.y = math.min(cutRange.y, @float.x + @float.y + t3.x * @float.z);
				}
				if (MathUtils.Intersect(curve4B.xz, new Line2.Segment(d.xz, d.xz - float5 * 48f), out t3, 4))
				{
					cutRange.y = math.min(cutRange.y, @float.x + @float.y + @float.z + t3.x * @float.w);
				}
				cutRange.x = math.min(cutRange.x, num3);
				cutRange.y = math.max(cutRange.y, 0f);
				CutCurves(curve, curve2, curve3, curve4, @float, cutRange, out curve1B, out curve2B, out curve3B, out curve4B);
				num11 = math.max(math.max(MathUtils.MaxDot(curve1B.xz, float5, out t2), MathUtils.MaxDot(curve2B.xz, float5, out t2)), math.max(MathUtils.MaxDot(curve3B.xz, float5, out t2), MathUtils.MaxDot(curve4B.xz, float5, out t2)));
				num11 -= math.dot(a.xz, float5);
				a.xz += float5 * (num11 + 24f);
				int num12 = (num10 + 10 - 1) / 10;
				for (int k = 0; k < num12; k++)
				{
					int num13 = k * num10 / num12;
					int num14 = (k + 1) * num10 / num12;
					Block block = new Block
					{
						m_Position = a
					};
					block.m_Position.xz += value3 * ((float)(num13 + num14) * 4f);
					block.m_Direction = -float5;
					block.m_Size.x = (byte)(num14 - num13);
					block.m_Size.y = 6;
					if (oldBlockBuffer.TryGetValue(block, out var item))
					{
						oldBlockBuffer.Remove(block);
						m_CommandBuffer.SetComponent(jobIndex, item, new PrefabRef(blockPrefab));
						m_CommandBuffer.SetComponent(jobIndex, item, component2);
						m_CommandBuffer.SetComponent(jobIndex, item, component);
						m_CommandBuffer.AddComponent(jobIndex, item, default(Updated));
						continue;
					}
					ZoneBlockData zoneBlockData = m_ZoneBlockData[blockPrefab];
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, zoneBlockData.m_Archetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(blockPrefab));
					m_CommandBuffer.SetComponent(jobIndex, e, block);
					m_CommandBuffer.SetComponent(jobIndex, e, component2);
					m_CommandBuffer.SetComponent(jobIndex, e, component);
					DynamicBuffer<Cell> dynamicBuffer2 = m_CommandBuffer.SetBuffer<Cell>(jobIndex, e);
					int num15 = block.m_Size.x * block.m_Size.y;
					for (int l = 0; l < num15; l++)
					{
						dynamicBuffer2.Add(default(Cell));
					}
					m_CommandBuffer.AddComponent(jobIndex, e, new Owner
					{
						m_Owner = owner
					});
				}
			}
		}

		private void TryOption(ref int baseWidth, ref int middleWidth, ref int splitCount, int totalWidth, int newBaseWidth, int newMiddleWidth)
		{
			int num = math.min(1, newMiddleWidth);
			num += (math.max(0, totalWidth - newMiddleWidth) / math.max(1, newBaseWidth)) & -2;
			int num2 = middleWidth + baseWidth * (splitCount & -2);
			if (newMiddleWidth + newBaseWidth * (num & -2) > num2)
			{
				baseWidth = newBaseWidth;
				middleWidth = newMiddleWidth;
				splitCount = num;
			}
		}

		private void CutStart(ref Bezier4x3 curve1, ref Bezier4x3 curve2, bool cutFirst)
		{
			float num = 8f;
			if (cutFirst)
			{
				curve1.a = (curve1.b = (curve1.c = (curve1.d = curve2.a)));
				return;
			}
			Bezier4x3 output;
			if (FindCutPos(curve1, curve1, out var t))
			{
				if (t != 0f)
				{
					Bounds1 t2 = new Bounds1(0f, t);
					if (MathUtils.ClampLengthInverse(curve1.xz, ref t2, num))
					{
						MathUtils.Divide(curve1, out output, out curve1, t2.min);
					}
				}
				return;
			}
			FindCutPos(curve1, curve2, out t);
			if (t != 0f)
			{
				Bounds1 t3 = new Bounds1(0f, t);
				if (MathUtils.ClampLengthInverse(curve2.xz, ref t3, num))
				{
					MathUtils.Divide(curve2, out output, out curve2, t3.min);
					num = 0f;
				}
				else
				{
					num -= MathUtils.Length(curve2.xz, t3);
				}
			}
			if (num > 0f)
			{
				Bounds1 t4 = new Bounds1(0f, 1f);
				if (MathUtils.ClampLengthInverse(curve1.xz, ref t4, num))
				{
					MathUtils.Divide(curve1, out output, out curve1, t4.min);
				}
			}
			else
			{
				curve1.a = (curve1.b = (curve1.c = (curve1.d = curve2.a)));
			}
		}

		private bool FindCutPos(Bezier4x3 startCurve, Bezier4x3 curve, out float t)
		{
			Bezier4x3 curve2 = new Bezier4x3(curve.a - startCurve.a, curve.b - startCurve.a, curve.c - startCurve.a, curve.d - startCurve.a);
			float2 x = new float2(0f, 1f);
			float2 value = MathUtils.StartTangent(startCurve).xz;
			t = 0f;
			if (!MathUtils.TryNormalize(ref value))
			{
				return true;
			}
			for (int i = 0; i < 8; i++)
			{
				float num = math.csum(x) * 0.5f;
				float2 xz = MathUtils.Position(curve2, num).xz;
				float2 value2 = MathUtils.Tangent(curve2, num).xz;
				if (!MathUtils.TryNormalize(ref value2))
				{
					return true;
				}
				if (math.dot(value, xz - value2 * 8f) - math.abs(math.dot(value, MathUtils.Right(value2) * 16f)) < 0f)
				{
					x.x = num;
				}
				else
				{
					x.y = num;
				}
			}
			if (x.x != 0f)
			{
				t = x.y;
				return x.y != 1f;
			}
			return true;
		}

		private void CutCurves(Bezier4x3 curve1, Bezier4x3 curve2, Bezier4x3 curve3, Bezier4x3 curve4, float4 curveLengths, float2 cutRange, out Bezier4x3 curve1B, out Bezier4x3 curve2B, out Bezier4x3 curve3B, out Bezier4x3 curve4B)
		{
			float2 @float = new float2(0f, 1f);
			float2 float2 = @float;
			float2 float3 = @float;
			float2 float4 = @float;
			float2 float5 = @float;
			curve1B = curve1;
			curve2B = curve2;
			curve3B = curve3;
			curve4B = curve4;
			if (cutRange.x - curveLengths.x - curveLengths.y < curveLengths.z)
			{
				if (cutRange.x - curveLengths.x < curveLengths.y)
				{
					if (cutRange.x < curveLengths.x)
					{
						float2.x = cutRange.x / curveLengths.x;
					}
					else
					{
						float2.x = 2f;
						float3.x = math.saturate((cutRange.x - curveLengths.x) / curveLengths.y);
					}
				}
				else
				{
					float2.x = 2f;
					float3.x = 2f;
					float4.x = math.saturate((cutRange.x - curveLengths.x - curveLengths.y) / curveLengths.z);
				}
			}
			else
			{
				float2.x = 2f;
				float3.x = 2f;
				float4.x = 2f;
				float5.x = math.saturate((cutRange.x - curveLengths.x - curveLengths.y - curveLengths.z) / curveLengths.w);
			}
			if (cutRange.y > curveLengths.x)
			{
				if (cutRange.y - curveLengths.x > curveLengths.y)
				{
					if (cutRange.y - curveLengths.x - curveLengths.y > curveLengths.z)
					{
						float5.y = math.saturate((cutRange.y - curveLengths.x - curveLengths.y - curveLengths.z) / curveLengths.w);
					}
					else
					{
						float5.y = -1f;
						float4.y = math.saturate((cutRange.y - curveLengths.x - curveLengths.y) / curveLengths.z);
					}
				}
				else
				{
					float5.y = -1f;
					float4.y = -1f;
					float3.y = math.saturate((cutRange.y - curveLengths.x) / curveLengths.y);
				}
			}
			else
			{
				float5.y = -1f;
				float4.y = -1f;
				float3.y = -1f;
				float2.y = math.saturate(cutRange.y / curveLengths.x);
			}
			if (math.any(float2 != @float) && float2.x <= float2.y)
			{
				curve1B = MathUtils.Cut(curve1, float2);
			}
			if (math.any(float3 != @float) && float3.x <= float3.y)
			{
				curve2B = MathUtils.Cut(curve2, float3);
			}
			if (math.any(float4 != @float) && float4.x <= float4.y)
			{
				curve3B = MathUtils.Cut(curve3, float4);
			}
			if (math.any(float5 != @float) && float5.x <= float5.y)
			{
				curve4B = MathUtils.Cut(curve4, float5);
			}
			if (float4.x == 2f)
			{
				curve3B.a = (curve3B.b = (curve3B.c = (curve3B.d = curve4B.a)));
			}
			if (float3.x == 2f)
			{
				curve2B.a = (curve2B.b = (curve2B.c = (curve2B.d = curve3B.a)));
			}
			if (float2.x == 2f)
			{
				curve1B.a = (curve1B.b = (curve1B.c = (curve1B.d = curve2B.a)));
			}
			if (float3.y == -1f)
			{
				curve2B.a = (curve2B.b = (curve2B.c = (curve2B.d = curve1B.d)));
			}
			if (float4.y == -1f)
			{
				curve3B.a = (curve3B.b = (curve3B.c = (curve3B.d = curve2B.d)));
			}
			if (float5.y == -1f)
			{
				curve4B.a = (curve4B.b = (curve4B.c = (curve4B.d = curve3B.d)));
			}
		}

		private bool FindContinuousEdge(Entity node, Entity edge, float2 position, float2 tangent, int cellWidth, bool halfAligned, bool invert)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge2 = dynamicBuffer[i].m_Edge;
				if (edge2 == edge)
				{
					continue;
				}
				Edge edge3 = m_EdgeData[edge2];
				Curve curve = m_CurveData[edge2];
				float2 xz;
				float2 value;
				bool test;
				if (edge3.m_Start == node)
				{
					xz = curve.m_Bezier.a.xz;
					value = MathUtils.StartTangent(curve.m_Bezier).xz;
					test = invert;
				}
				else
				{
					if (!(edge3.m_End == node))
					{
						continue;
					}
					xz = curve.m_Bezier.d.xz;
					value = -MathUtils.EndTangent(curve.m_Bezier).xz;
					test = !invert;
				}
				if (!MathUtils.TryNormalize(ref value) || math.dot(tangent, value) > -0.99f || math.distance(position, xz) > 0.01f)
				{
					continue;
				}
				Entity edge4 = m_CompositionData[edge2].m_Edge;
				if (!m_RoadCompositionData.HasComponent(edge4) || (m_RoadCompositionData[edge4].m_Flags & Game.Prefabs.RoadFlags.EnableZoning) == 0)
				{
					continue;
				}
				NetCompositionData netCompositionData = m_PrefabCompositionData[edge4];
				if ((netCompositionData.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) != 0)
				{
					continue;
				}
				int cellWidth2 = ZoneUtils.GetCellWidth(netCompositionData.m_Width + netCompositionData.m_MiddleOffset * math.select(-2f, 2f, test));
				if (cellWidth != cellWidth2)
				{
					continue;
				}
				Road road = m_RoadData[edge2];
				if (edge3.m_Start == node)
				{
					if ((road.m_Flags & Game.Net.RoadFlags.StartHalfAligned) != 0 != halfAligned)
					{
						continue;
					}
				}
				else if ((road.m_Flags & Game.Net.RoadFlags.EndHalfAligned) != 0 != halfAligned)
				{
					continue;
				}
				return true;
			}
			return false;
		}

		private void CreateBlocks(int jobIndex, Entity owner, Entity startNode, Entity endNode, NativeParallelHashMap<Block, Entity> oldBlockBuffer, Entity blockPrefab, Bezier4x3 curve, int cellWidth, uint startOrder, uint endOrder, bool startHalf, bool endHalf, bool invert)
		{
			float2 value = MathUtils.StartTangent(curve).xz;
			float2 value2 = MathUtils.EndTangent(curve).xz;
			if (!MathUtils.TryNormalize(ref value) || !MathUtils.TryNormalize(ref value2))
			{
				return;
			}
			bool flag = FindContinuousEdge(startNode, owner, curve.a.xz, value, cellWidth, startHalf, !invert);
			bool flag2 = FindContinuousEdge(endNode, owner, curve.d.xz, -value2, cellWidth, endHalf, invert);
			float num = NetUtils.FindMiddleTangentPos(curve.xz, new float2(0f, 1f));
			MathUtils.Divide(curve, out var output, out var output2, num);
			float num2 = (float)cellWidth * 4f;
			output = NetUtils.OffsetCurveLeftSmooth(output, 0f - num2);
			output2 = NetUtils.OffsetCurveLeftSmooth(output2, 0f - num2);
			float num3 = MathUtils.Length(output.xz) + MathUtils.Length(output2.xz);
			if (num3 < 8f)
			{
				return;
			}
			value = MathUtils.StartTangent(output).xz;
			value2 = MathUtils.EndTangent(output2).xz;
			if (!MathUtils.TryNormalize(ref value) || !MathUtils.TryNormalize(ref value2))
			{
				return;
			}
			float num4 = math.degrees(math.acos(math.clamp(math.dot(value, value2), -1f, 1f)) * 8f / num3);
			float num5 = 2f / math.sqrt(math.clamp(num4 / 15f, 0.0001f, 1f)) * 8f;
			int num6 = math.max(1, (int)(num3 / num5));
			BuildOrder component = new BuildOrder
			{
				m_Order = startOrder
			};
			Bezier4x3 bezier4x = default(Bezier4x3);
			Bezier4x3 bezier4x2 = default(Bezier4x3);
			for (int i = 0; i < num6; i++)
			{
				float2 @float = new float2((float)i / (float)num6, (float)(i + 1) / (float)num6);
				float2 t = math.min(1f, @float / num);
				float2 t2 = math.max(0f, (@float - num) / (1f - num));
				if (t.x < 1f)
				{
					value = MathUtils.Tangent(output, t.x).xz;
					bezier4x = MathUtils.Cut(output, t);
				}
				else
				{
					value = MathUtils.Tangent(output2, t2.x).xz;
					bezier4x.a = (bezier4x.b = (bezier4x.c = (bezier4x.d = MathUtils.Position(output2, t2.x))));
				}
				if (t.y < 1f)
				{
					value2 = MathUtils.Tangent(output, t.y).xz;
					bezier4x2.a = (bezier4x2.b = (bezier4x2.c = (bezier4x2.d = MathUtils.Position(output, t.y))));
				}
				else
				{
					value2 = MathUtils.Tangent(output2, t2.y).xz;
					bezier4x2 = MathUtils.Cut(output2, t2);
				}
				if (!MathUtils.TryNormalize(ref value) || !MathUtils.TryNormalize(ref value2))
				{
					continue;
				}
				float2 float2 = MathUtils.Right(value);
				float2 float3 = MathUtils.Right(value2);
				float3 a = bezier4x.a;
				float3 d = bezier4x2.d;
				float2 value3 = d.xz - a.xz;
				if (!MathUtils.TryNormalize(ref value3))
				{
					continue;
				}
				if (i == 0)
				{
					if (flag)
					{
						a.xz -= value3 * math.select(0f, 4f, ((cellWidth & 1) != 0) ^ startHalf);
					}
					else
					{
						float num7 = num2 - math.select(0f, 8f, cellWidth > 1);
						num7 += math.select(0f, math.select(4f, -4f, (cellWidth & 1) != 0), startHalf);
						a.xz -= value3 * num7;
					}
				}
				if (i == num6 - 1)
				{
					if (flag2)
					{
						d.xz += value3 * math.select(0f, 4f, ((cellWidth & 1) != 0) ^ endHalf);
					}
					else
					{
						float num8 = num2 - math.select(0f, 8f, cellWidth > 1);
						num8 += math.select(0f, math.select(4f, -4f, (cellWidth & 1) != 0), endHalf);
						d.xz += value3 * num8;
					}
				}
				float2 float4 = MathUtils.Right(value3);
				float num9 = math.max(MathUtils.MaxDot(bezier4x.xz, float4, out var t3), MathUtils.MaxDot(bezier4x2.xz, float4, out t3));
				num9 -= math.dot(a.xz, float4);
				float upperBound = math.distance(a.xz, d.xz);
				a.xz += float2 * math.clamp(num9 / math.dot(float2, float4), 0f, upperBound);
				d.xz += float3 * math.clamp(num9 / math.dot(float3, float4), 0f, upperBound);
				int num10 = (int)math.floor((math.length((d - a).xz) + 0.1f) / 8f);
				if (num10 < 2)
				{
					continue;
				}
				float3 float5 = math.lerp(a, d, 0.5f);
				float2 float6 = value3 * ((float)num10 * 4f);
				a = float5;
				a.xz -= float6;
				d = float5;
				d.xz += float6;
				float2 float7 = @float;
				if (MathUtils.Intersect(bezier4x.xz, new Line2.Segment(a.xz, a.xz - float4 * 48f), out var t4, 4))
				{
					float7.x = math.max(float7.x, math.lerp(@float.x, @float.y, t4.x * num));
				}
				if (MathUtils.Intersect(bezier4x2.xz, new Line2.Segment(a.xz, a.xz - float4 * 48f), out t4, 4))
				{
					float7.x = math.max(float7.x, math.lerp(@float.x, @float.y, t4.x * (1f - num) + num));
				}
				if (MathUtils.Intersect(bezier4x.xz, new Line2.Segment(d.xz, d.xz - float4 * 48f), out t4, 4))
				{
					float7.y = math.min(float7.y, math.lerp(@float.x, @float.y, t4.x * num));
				}
				if (MathUtils.Intersect(bezier4x2.xz, new Line2.Segment(d.xz, d.xz - float4 * 48f), out t4, 4))
				{
					float7.y = math.min(float7.y, math.lerp(@float.x, @float.y, t4.x * (1f - num) + num));
				}
				t = math.min(1f, float7 / num);
				t2 = math.max(0f, (float7 - num) / (1f - num));
				if (t.x < 1f)
				{
					bezier4x = MathUtils.Cut(output, t);
				}
				else
				{
					bezier4x.a = (bezier4x.b = (bezier4x.c = (bezier4x.d = MathUtils.Position(output2, t2.x))));
				}
				if (t.y < 1f)
				{
					bezier4x2.a = (bezier4x2.b = (bezier4x2.c = (bezier4x2.d = MathUtils.Position(output, t.y))));
				}
				else
				{
					bezier4x2 = MathUtils.Cut(output2, t2);
				}
				num9 = math.max(MathUtils.MaxDot(bezier4x.xz, float4, out t3), MathUtils.MaxDot(bezier4x2.xz, float4, out t3));
				num9 -= math.dot(a.xz, float4);
				a.xz += float4 * (num9 + 24f);
				int num11 = (num10 + 10 - 1) / 10;
				for (int j = 0; j < num11; j++)
				{
					int num12 = j * num10 / num11;
					int num13 = (j + 1) * num10 / num11;
					Block block = new Block
					{
						m_Position = a
					};
					block.m_Position.xz += value3 * ((float)(num12 + num13) * 4f);
					block.m_Direction = -float4;
					block.m_Size.x = (byte)(num13 - num12);
					block.m_Size.y = 6;
					CurvePosition component2 = default(CurvePosition);
					component2.m_CurvePosition = math.lerp(@float.x, @float.y, new float2(num13, num12) / num10);
					component2.m_CurvePosition = math.select(component2.m_CurvePosition, 1f - component2.m_CurvePosition, invert);
					if (endOrder > startOrder)
					{
						component.m_Order++;
					}
					else if (endOrder < startOrder)
					{
						component.m_Order--;
					}
					if (oldBlockBuffer.TryGetValue(block, out var item))
					{
						oldBlockBuffer.Remove(block);
						m_CommandBuffer.SetComponent(jobIndex, item, new PrefabRef(blockPrefab));
						m_CommandBuffer.SetComponent(jobIndex, item, component2);
						m_CommandBuffer.SetComponent(jobIndex, item, component);
						m_CommandBuffer.AddComponent(jobIndex, item, default(Updated));
						continue;
					}
					ZoneBlockData zoneBlockData = m_ZoneBlockData[blockPrefab];
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, zoneBlockData.m_Archetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(blockPrefab));
					m_CommandBuffer.SetComponent(jobIndex, e, block);
					m_CommandBuffer.SetComponent(jobIndex, e, component2);
					m_CommandBuffer.SetComponent(jobIndex, e, component);
					DynamicBuffer<Cell> dynamicBuffer = m_CommandBuffer.SetBuffer<Cell>(jobIndex, e);
					int num14 = block.m_Size.x * block.m_Size.y;
					for (int k = 0; k < num14; k++)
					{
						dynamicBuffer.Add(default(Cell));
					}
					m_CommandBuffer.AddComponent(jobIndex, e, new Owner
					{
						m_Owner = owner
					});
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
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.BuildOrder> __Game_Net_BuildOrder_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Road> __Game_Net_Road_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubBlock> __Game_Zones_SubBlock_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.BuildOrder> __Game_Net_BuildOrder_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadComposition> __Game_Prefabs_RoadComposition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneBlockData> __Game_Prefabs_ZoneBlockData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Net_BuildOrder_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.BuildOrder>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Road>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Zones_SubBlock_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubBlock>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_BuildOrder_RO_ComponentLookup = state.GetComponentLookup<Game.Net.BuildOrder>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_RoadComposition_RO_ComponentLookup = state.GetComponentLookup<RoadComposition>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_ZoneBlockData_RO_ComponentLookup = state.GetComponentLookup<ZoneBlockData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdatedEdgesQuery;

	private ModificationBarrier4 m_ModificationBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_UpdatedEdgesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<SubBlock>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		RequireForUpdate(m_UpdatedEdgesQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateBlocksJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildOrderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_BuildOrder_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubBlockType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_SubBlock_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildOrderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_BuildOrder_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadComposition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneBlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneBlockData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_UpdatedEdgesQuery, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public BlockSystem()
	{
	}
}
