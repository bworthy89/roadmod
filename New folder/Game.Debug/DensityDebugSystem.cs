using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class DensityDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct DensityGizmoJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<Density> m_DensityType;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<EdgeGeometry> nativeArray = chunk.GetNativeArray(ref m_EdgeGeometryType);
			NativeArray<Density> nativeArray2 = chunk.GetNativeArray(ref m_DensityType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Density density = nativeArray2[i];
				EdgeGeometry edgeGeometry = nativeArray[i];
				Color color = Color.Lerp(Color.red, Color.green, math.saturate(5f * density.m_Density));
				int num = (int)math.ceil(edgeGeometry.m_Start.middleLength * 0.5f);
				int num2 = (int)math.ceil(edgeGeometry.m_End.middleLength * 0.5f);
				float3 @float = math.lerp(edgeGeometry.m_Start.m_Left.a, edgeGeometry.m_Start.m_Right.a, 0.5f);
				Line3.Segment segment = new Line3.Segment(@float, @float + new float3(0f, density.m_Density * 100f, 0f));
				m_GizmoBatcher.DrawLine(segment.a, segment.b, color);
				for (int j = 1; j <= num; j++)
				{
					float2 float2 = j / new float2(num, num + num2);
					@float = math.lerp(MathUtils.Position(edgeGeometry.m_Start.m_Left, float2.x), MathUtils.Position(edgeGeometry.m_Start.m_Right, float2.x), 0.5f);
					Line3.Segment segment2 = new Line3.Segment(@float, @float + new float3(0f, density.m_Density * 100f, 0f));
					m_GizmoBatcher.DrawLine(segment2.a, segment2.b, color);
					m_GizmoBatcher.DrawLine(segment.b, segment2.b, color);
					segment = segment2;
				}
				for (int k = 1; k <= num2; k++)
				{
					float2 float3 = new float2(k, num + k) / new float2(num2, num + num2);
					@float = math.lerp(MathUtils.Position(edgeGeometry.m_End.m_Left, float3.x), MathUtils.Position(edgeGeometry.m_End.m_Right, float3.x), 0.5f);
					Line3.Segment segment3 = new Line3.Segment(@float, @float + new float3(0f, density.m_Density * 100f, 0f));
					m_GizmoBatcher.DrawLine(segment3.a, segment3.b, color);
					m_GizmoBatcher.DrawLine(segment.b, segment3.b, color);
					segment = segment3;
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
		public ComponentTypeHandle<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Density> __Game_Net_Density_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_EdgeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
			__Game_Net_Density_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Density>(isReadOnly: true);
		}
	}

	private EntityQuery m_EdgeGroup;

	private GizmosSystem m_GizmosSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_EdgeGroup = GetEntityQuery(ComponentType.ReadOnly<Density>(), ComponentType.ReadOnly<EdgeGeometry>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		base.Enabled = false;
		RequireForUpdate(m_EdgeGroup);
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		inputDeps = JobChunkExtensions.ScheduleParallel(new DensityGizmoJob
		{
			m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DensityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Density_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out var dependencies)
		}, m_EdgeGroup, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(inputDeps);
		return inputDeps;
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
	public DensityDebugSystem()
	{
	}
}
