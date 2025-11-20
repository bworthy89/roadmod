using System;
using System.Collections.Generic;
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
public class AvailabilityDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct AvailabilityGizmoJob : IJobChunk
	{
		[ReadOnly]
		public AvailableResource m_Resource;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometryType;

		[ReadOnly]
		public BufferTypeHandle<ResourceAvailability> m_AvailabilityType;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<EdgeGeometry> nativeArray = chunk.GetNativeArray(ref m_EdgeGeometryType);
			BufferAccessor<ResourceAvailability> bufferAccessor = chunk.GetBufferAccessor(ref m_AvailabilityType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<ResourceAvailability> dynamicBuffer = bufferAccessor[i];
				if (dynamicBuffer.Length != 0)
				{
					EdgeGeometry edgeGeometry = nativeArray[i];
					ResourceAvailability resourceAvailability = dynamicBuffer[(int)m_Resource];
					Color color = Color.Lerp(Color.red, Color.green, resourceAvailability.m_Availability.x * 0.25f);
					Color b = Color.Lerp(Color.red, Color.green, resourceAvailability.m_Availability.y * 0.25f);
					int num = (int)math.ceil(edgeGeometry.m_Start.middleLength * 0.5f);
					int num2 = (int)math.ceil(edgeGeometry.m_End.middleLength * 0.5f);
					float3 @float = math.lerp(edgeGeometry.m_Start.m_Left.a, edgeGeometry.m_Start.m_Right.a, 0.5f);
					Line3.Segment segment = new Line3.Segment(@float, @float + new float3(0f, resourceAvailability.m_Availability.x * 10f, 0f));
					m_GizmoBatcher.DrawLine(segment.a, segment.b, color);
					for (int j = 1; j <= num; j++)
					{
						float2 float2 = j / new float2(num, num + num2);
						@float = math.lerp(MathUtils.Position(edgeGeometry.m_Start.m_Left, float2.x), MathUtils.Position(edgeGeometry.m_Start.m_Right, float2.x), 0.5f);
						float num3 = math.lerp(resourceAvailability.m_Availability.x, resourceAvailability.m_Availability.y, float2.y);
						Color color2 = Color.Lerp(color, b, float2.y);
						Line3.Segment segment2 = new Line3.Segment(@float, @float + new float3(0f, num3 * 10f, 0f));
						m_GizmoBatcher.DrawLine(segment2.a, segment2.b, color2);
						m_GizmoBatcher.DrawLine(segment.b, segment2.b, color2);
						segment = segment2;
					}
					for (int k = 1; k <= num2; k++)
					{
						float2 float3 = new float2(k, num + k) / new float2(num2, num + num2);
						@float = math.lerp(MathUtils.Position(edgeGeometry.m_End.m_Left, float3.x), MathUtils.Position(edgeGeometry.m_End.m_Right, float3.x), 0.5f);
						float num4 = math.lerp(resourceAvailability.m_Availability.x, resourceAvailability.m_Availability.y, float3.y);
						Color color3 = Color.Lerp(color, b, float3.y);
						Line3.Segment segment3 = new Line3.Segment(@float, @float + new float3(0f, num4 * 10f, 0f));
						m_GizmoBatcher.DrawLine(segment3.a, segment3.b, color3);
						m_GizmoBatcher.DrawLine(segment.b, segment3.b, color3);
						segment = segment3;
					}
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
		public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_EdgeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
			__Game_Net_ResourceAvailability_RO_BufferTypeHandle = state.GetBufferTypeHandle<ResourceAvailability>(isReadOnly: true);
		}
	}

	private EntityQuery m_AvailabilityGroup;

	private GizmosSystem m_GizmosSystem;

	private Dictionary<AvailableResource, Option> m_AvailabilityOptions;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_AvailabilityGroup = GetEntityQuery(ComponentType.ReadOnly<ResourceAvailability>(), ComponentType.ReadOnly<EdgeGeometry>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_AvailabilityOptions = new Dictionary<AvailableResource, Option>();
		string[] names = Enum.GetNames(typeof(AvailableResource));
		Array values = Enum.GetValues(typeof(AvailableResource));
		for (int i = 0; i < names.Length; i++)
		{
			AvailableResource availableResource = (AvailableResource)values.GetValue(i);
			if (availableResource != AvailableResource.Count)
			{
				m_AvailabilityOptions.Add(availableResource, AddOption(names[i], i == 0));
			}
		}
		RequireForUpdate(m_AvailabilityGroup);
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle jobHandle = inputDeps;
		foreach (KeyValuePair<AvailableResource, Option> item in m_AvailabilityOptions)
		{
			if (item.Value.enabled)
			{
				JobHandle dependencies;
				JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new AvailabilityGizmoJob
				{
					m_Resource = item.Key,
					m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_AvailabilityType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
				}, m_AvailabilityGroup, JobHandle.CombineDependencies(inputDeps, dependencies));
				m_GizmosSystem.AddGizmosBatcherWriter(jobHandle2);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
		}
		return jobHandle;
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
	public AvailabilityDebugSystem()
	{
	}
}
