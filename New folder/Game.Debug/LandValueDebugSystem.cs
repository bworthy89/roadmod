using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class LandValueDebugSystem : BaseDebugSystem
{
	private struct LandValueEdgeGizmoJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<LandValue> m_LandValues;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		public GizmoBatcher m_GizmoBatcher;

		public bool m_LandValueOption;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (!m_LandValueOption)
			{
				return;
			}
			NativeArray<Edge> nativeArray = chunk.GetNativeArray(ref m_EdgeType);
			NativeArray<LandValue> nativeArray2 = chunk.GetNativeArray(ref m_LandValues);
			NativeArray<Curve> nativeArray3 = chunk.GetNativeArray(ref m_CurveType);
			if (nativeArray.Length == 0)
			{
				return;
			}
			if (nativeArray3.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Curve curve = nativeArray3[i];
					float landValue = nativeArray2[i].m_LandValue;
					Color color = GetColor(Color.gray, Color.blue, Color.magenta, landValue, 30f, 500f);
					float3 @float = MathUtils.Position(curve.m_Bezier, 0.5f);
					@float.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, @float);
					@float.y += heightScale * landValue / 2f;
					m_GizmoBatcher.DrawWireCylinder(@float, 5f, heightScale * landValue, color);
				}
				return;
			}
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Edge edge = nativeArray[j];
				Node node = m_NodeData[edge.m_Start];
				Node node2 = m_NodeData[edge.m_End];
				float landValue2 = nativeArray2[j].m_LandValue;
				Color color2 = GetColor(Color.gray, Color.blue, Color.magenta, landValue2, 30f, 500f);
				float3 float2 = 0.5f * (node.m_Position + node2.m_Position);
				float2.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, float2);
				float2.y += heightScale * landValue2 / 2f;
				m_GizmoBatcher.DrawWireCylinder(float2, 5f, heightScale * landValue2, color2);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct LandValueGizmoJob : IJob
	{
		[ReadOnly]
		public NativeArray<LandValueCell> m_LandValueMap;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public GizmoBatcher m_GizmoBatcher;

		public bool m_LandValueOption;

		[ReadOnly]
		public LandValueParameterData m_LandValueParameterData;

		public void Execute()
		{
			if (!m_LandValueOption)
			{
				return;
			}
			for (int i = 0; i < m_LandValueMap.Length; i++)
			{
				float landValue = m_LandValueMap[i].m_LandValue;
				Color color = GetColor(Color.red, Color.yellow, Color.green, landValue, 30f, 500f);
				float3 cellCenter = LandValueSystem.GetCellCenter(i);
				cellCenter.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cellCenter);
				cellCenter.y += heightScale * landValue / 2f;
				if (landValue > m_LandValueParameterData.m_LandValueBaseline)
				{
					m_GizmoBatcher.DrawWireCube(cellCenter, new float3(15f, heightScale * landValue, 15f), color);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LandValue> __Game_Net_LandValue_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_LandValue_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LandValue>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
		}
	}

	private LandValueSystem m_LandValueSystem;

	private GizmosSystem m_GizmosSystem;

	private TerrainSystem m_TerrainSystem;

	private DefaultToolSystem m_DefaultToolSystem;

	private EntityQuery m_LandValueEdgeQuery;

	private EntityQuery m_LandValueParameterQuery;

	public Option m_LandValueCellOption;

	private Option m_EdgeLandValueOption;

	private static readonly float heightScale = 1f;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LandValueSystem = base.World.GetOrCreateSystemManaged<LandValueSystem>();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_LandValueParameterQuery = GetEntityQuery(ComponentType.ReadOnly<LandValueParameterData>());
		m_LandValueEdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<LandValue>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Hidden>());
		m_LandValueCellOption = AddOption("Land value (Cell)", defaultEnabled: true);
		m_EdgeLandValueOption = AddOption("Land value (Edge)", defaultEnabled: true);
		base.Enabled = false;
	}

	public override void OnEnabled(DebugUI.Container container)
	{
		base.OnEnabled(container);
		m_DefaultToolSystem.debugLandValue = true;
	}

	public override void OnDisabled(DebugUI.Container container)
	{
		base.OnDisabled(container);
		m_DefaultToolSystem.debugLandValue = false;
	}

	private static Color GetColor(Color a, Color b, Color c, float value, float maxValue1, float maxValue2)
	{
		if (value < maxValue1)
		{
			return Color.Lerp(a, b, value / maxValue1);
		}
		return Color.Lerp(b, c, math.saturate((value - maxValue1) / (maxValue2 - maxValue1)));
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		if (m_LandValueCellOption.enabled)
		{
			JobHandle dependencies;
			JobHandle dependencies2;
			LandValueGizmoJob jobData = new LandValueGizmoJob
			{
				m_LandValueMap = m_LandValueSystem.GetMap(readOnly: true, out dependencies),
				m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies2),
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
				m_LandValueOption = m_LandValueCellOption.enabled,
				m_LandValueParameterData = m_LandValueParameterQuery.GetSingleton<LandValueParameterData>()
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(inputDeps, dependencies2, dependencies));
			m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
		}
		if (m_EdgeLandValueOption.enabled)
		{
			JobHandle dependencies3;
			LandValueEdgeGizmoJob jobData2 = new LandValueEdgeGizmoJob
			{
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LandValues = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LandValue_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
				m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies3),
				m_LandValueOption = m_EdgeLandValueOption.enabled
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_LandValueEdgeQuery, JobHandle.CombineDependencies(inputDeps, dependencies3));
			m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
		}
		m_TerrainSystem.AddCPUHeightReader(base.Dependency);
		return base.Dependency;
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
	public LandValueDebugSystem()
	{
	}
}
