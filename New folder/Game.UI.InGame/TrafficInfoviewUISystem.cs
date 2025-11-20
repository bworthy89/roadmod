using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Common;
using Game.Net;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class TrafficInfoviewUISystem : InfoviewUISystemBase
{
	[BurstCompile]
	private struct UpdateFlowJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Road> m_RoadHandle;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Road> nativeArray = chunk.GetNativeArray(ref m_RoadHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				float4 @float = NetUtils.GetTrafficFlowSpeed(nativeArray[i]) * 100f;
				m_Results[0] += @float.x;
				m_Results[1] += @float.y;
				m_Results[2] += @float.z;
				m_Results[3] += @float.w;
			}
			m_Results[4] += nativeArray.Length;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Road> __Game_Net_Road_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Road_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Road>(isReadOnly: true);
		}
	}

	private const string kGroup = "trafficInfo";

	private EntityQuery m_AggregateQuery;

	private RawValueBinding m_TrafficFlow;

	private NativeArray<float> m_Results;

	private float[] m_Flow;

	private TypeHandle __TypeHandle;

	protected override bool Active
	{
		get
		{
			if (!base.Active)
			{
				return m_TrafficFlow.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AggregateQuery = GetEntityQuery(ComponentType.ReadOnly<Aggregated>(), ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Road>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Native>());
		AddBinding(m_TrafficFlow = new RawValueBinding("trafficInfo", "trafficFlow", UpdateTrafficFlowBinding));
		m_Flow = new float[5];
		m_Results = new NativeArray<float>(5, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		Reset();
		JobChunkExtensions.Schedule(new UpdateFlowJob
		{
			m_RoadHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_AggregateQuery, base.Dependency).Complete();
		m_TrafficFlow.Update();
	}

	private void Reset()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0f;
		}
		for (int j = 0; j < m_Flow.Length; j++)
		{
			m_Flow[j] = 0f;
		}
	}

	private void UpdateTrafficFlowBinding(IJsonWriter writer)
	{
		int num = math.select((int)m_Results[4], 1, (int)m_Results[4] == 0);
		m_Flow[0] = m_Results[0] / (float)num;
		m_Flow[1] = m_Results[1] / (float)num;
		m_Flow[2] = m_Results[2] / (float)num;
		m_Flow[3] = m_Results[3] / (float)num;
		m_Flow[4] = m_Flow[0];
		writer.ArrayBegin(m_Flow.Length);
		for (int i = 0; i < m_Flow.Length; i++)
		{
			writer.Write(m_Flow[i]);
		}
		writer.ArrayEnd();
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
	public TrafficInfoviewUISystem()
	{
	}
}
