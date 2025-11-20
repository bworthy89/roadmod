using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Mathematics;
using Game.Common;
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
public class NodeAlignSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateNodeRotationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Standalone> m_StandaloneType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Node> nativeArray2 = chunk.GetNativeArray(ref m_NodeType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool isStandalone = chunk.Has(ref m_StandaloneType);
			NativeList<float> angleBuffer = new NativeList<float>(32, Allocator.Temp);
			NativeList<Line2.Segment> lineBuffer = new NativeList<Line2.Segment>(16, Allocator.Temp);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Node node = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				AlignNode(entity, isStandalone, angleBuffer, lineBuffer, prefabRef, ref node);
				nativeArray2[i] = node;
				angleBuffer.Clear();
				lineBuffer.Clear();
			}
			angleBuffer.Dispose();
			lineBuffer.Dispose();
		}

		private void AlignNode(Entity entity, bool isStandalone, NativeList<float> angleBuffer, NativeList<Line2.Segment> lineBuffer, PrefabRef prefabRef, ref Node node)
		{
			float2 y = default(float2);
			float3 @float = default(float3);
			float3 position = node.m_Position;
			int num = 0;
			m_PrefabNetData.TryGetComponent(prefabRef.m_Prefab, out var componentData);
			EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, entity, m_Edges, m_EdgeData, m_TempData, m_HiddenData);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				Curve curve = m_CurveData[value.m_Edge];
				PrefabRef prefabRef2 = m_PrefabRefData[value.m_Edge];
				m_PrefabNetData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2);
				if ((componentData.m_RequiredLayers & componentData2.m_RequiredLayers) != Layer.None)
				{
					float3 float2;
					float2 value2;
					if (value.m_End)
					{
						float2 = curve.m_Bezier.d - position;
						value2 = -MathUtils.EndTangent(curve.m_Bezier).xz;
						y -= value2;
					}
					else
					{
						float2 = curve.m_Bezier.a - position;
						value2 = MathUtils.StartTangent(curve.m_Bezier).xz;
						y += value2;
					}
					@float += float2;
					num++;
					if (MathUtils.TryNormalize(ref value2))
					{
						float num2 = math.atan2(value2.x, 0f - value2.y) * (1f / (2f * MathF.PI)) + 1.25f;
						angleBuffer.Add(num2 - math.floor(num2));
						num2 += 0.5f;
						angleBuffer.Add(num2 - math.floor(num2));
						lineBuffer.Add(new Line2.Segment(float2.xz, value2));
					}
				}
			}
			if (!isStandalone)
			{
				if (num > 0)
				{
					node.m_Position = position + @float / num;
					@float = default(float3);
					num = 0;
				}
				if (lineBuffer.Length >= 2)
				{
					lineBuffer.Sort(default(LineComparer));
					for (int i = 1; i < lineBuffer.Length; i++)
					{
						Line2.Segment segment = lineBuffer[i];
						for (int j = 0; j < i; j++)
						{
							Line2.Segment segment2 = lineBuffer[j];
							float x = math.dot(segment.b, segment2.b);
							float3 float3 = default(float3);
							if (math.abs(x) > 0.999f)
							{
								float3.xy = segment.a + segment2.a;
								float3.z = 2f;
							}
							else
							{
								float2 float4 = math.distance(segment.a, segment2.a) * new float2(math.abs(x) - 1f, 1f - math.abs(x));
								Line2.Segment segment3 = new Line2.Segment(segment.a - segment.b * float4.x, segment.a - segment.b * float4.y);
								Line2.Segment segment4 = new Line2.Segment(segment2.a - segment2.b * float4.x, segment2.a - segment2.b * float4.y);
								MathUtils.Distance(segment3, segment4, out var t);
								float3.xy = MathUtils.Position(segment3, t.x) + MathUtils.Position(segment4, t.y);
								float3.z = 2f;
							}
							float num3 = 1.01f - math.abs(x);
							@float += float3 * num3;
							num++;
						}
					}
					if (num > 0)
					{
						node.m_Position.xz = position.xz + @float.xy / @float.z;
					}
				}
			}
			if (angleBuffer.Length == 0)
			{
				if (m_OutsideConnectionData.HasComponent(entity))
				{
					float2 float5 = math.abs(node.m_Position.xz);
					if (float5.x > float5.y)
					{
						node.m_Rotation = quaternion.LookRotation(new float3(0f - math.sign(node.m_Position.x), 0f, 0f), math.up());
					}
					else if (float5.y > float5.x)
					{
						node.m_Rotation = quaternion.LookRotation(new float3(0f, 0f, 0f - math.sign(node.m_Position.z)), math.up());
					}
				}
				return;
			}
			float num4;
			if (angleBuffer.Length == 2)
			{
				num4 = angleBuffer[0] + 0.75f;
			}
			else
			{
				angleBuffer.Sort();
				float num5 = angleBuffer[angleBuffer.Length - 1];
				float num6 = angleBuffer[0];
				float num7 = num6 + 1f - num5;
				num4 = (num5 + num6) * 0.5f;
				for (int k = 1; k < angleBuffer.Length; k++)
				{
					num5 = angleBuffer[k - 1];
					num6 = angleBuffer[k];
					float num8 = num6 - num5;
					if (num8 > num7)
					{
						num7 = num8;
						num4 = (num5 + num6) * 0.5f;
					}
				}
			}
			node.m_Rotation = quaternion.RotateY(num4 * (MathF.PI * -2f));
			if (math.dot(math.rotate(node.m_Rotation, new float3(0f, 0f, 1f)).xz, y) < 0f)
			{
				node.m_Rotation = quaternion.RotateY(MathF.PI - num4 * (MathF.PI * 2f));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct LineComparer : IComparer<Line2.Segment>
	{
		public int Compare(Line2.Segment x, Line2.Segment y)
		{
			return math.csum(math.select(0, math.select(new int4(-8, -4, -2, -1), new int4(8, 4, 2, 1), x.ab > y.ab), x.ab != y.ab));
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Standalone> __Game_Net_Standalone_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Node> __Game_Net_Node_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> __Game_Net_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Standalone_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Standalone>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_Node_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Node>();
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<OutsideConnection>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
		}
	}

	private EntityQuery m_NodeQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NodeQuery = GetEntityQuery(ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Updated>());
		RequireForUpdate(m_NodeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new UpdateNodeRotationsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_StandaloneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Standalone_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef)
		}, m_NodeQuery, base.Dependency);
		base.Dependency = dependency;
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
	public NodeAlignSystem()
	{
	}
}
