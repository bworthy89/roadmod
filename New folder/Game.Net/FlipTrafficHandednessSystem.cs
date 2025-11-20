using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class FlipTrafficHandednessSystem : GameSystemBase
{
	[BurstCompile]
	private struct FlipOnewayRoadsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		public ComponentTypeHandle<Edge> m_EdgeType;

		public ComponentTypeHandle<Curve> m_CurveType;

		public ComponentTypeHandle<Elevation> m_ElevationType;

		public ComponentTypeHandle<Upgraded> m_UpgradedType;

		public ComponentTypeHandle<BuildOrder> m_BuildOrderType;

		public BufferTypeHandle<ConnectedNode> m_ConnectedNodeType;

		public BufferTypeHandle<ServiceCoverage> m_ServiceCoverageType;

		public BufferTypeHandle<ResourceAvailability> m_ResourceAvailabilityType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Edge> nativeArray2 = chunk.GetNativeArray(ref m_EdgeType);
			NativeArray<Curve> nativeArray3 = chunk.GetNativeArray(ref m_CurveType);
			NativeArray<Elevation> nativeArray4 = chunk.GetNativeArray(ref m_ElevationType);
			NativeArray<Upgraded> nativeArray5 = chunk.GetNativeArray(ref m_UpgradedType);
			NativeArray<BuildOrder> nativeArray6 = chunk.GetNativeArray(ref m_BuildOrderType);
			BufferAccessor<ConnectedNode> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedNodeType);
			BufferAccessor<ServiceCoverage> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceCoverageType);
			BufferAccessor<ResourceAvailability> bufferAccessor3 = chunk.GetBufferAccessor(ref m_ResourceAvailabilityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				if (!m_PrefabGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					continue;
				}
				if ((m_PrefabGeometryData[prefabRef.m_Prefab].m_Flags & GeometryFlags.FlipTrafficHandedness) == 0)
				{
					if (nativeArray5.Length != 0)
					{
						Upgraded value = nativeArray5[i];
						NetUtils.FlipUpgradeTrafficHandedness(ref value.m_Flags);
						nativeArray5[i] = value;
					}
					continue;
				}
				Edge value2 = nativeArray2[i];
				CommonUtils.Swap(ref value2.m_Start, ref value2.m_End);
				nativeArray2[i] = value2;
				Curve value3 = nativeArray3[i];
				value3.m_Bezier = MathUtils.Invert(value3.m_Bezier);
				nativeArray3[i] = value3;
				if (nativeArray4.Length != 0)
				{
					Elevation value4 = nativeArray4[i];
					value4.m_Elevation = value4.m_Elevation.yx;
					nativeArray4[i] = value4;
				}
				if (nativeArray5.Length != 0)
				{
					Upgraded value5 = nativeArray5[i];
					NetUtils.FixInvertedUpgradeTrafficHandedness(ref value5.m_Flags);
					nativeArray5[i] = value5;
				}
				if (nativeArray6.Length != 0)
				{
					BuildOrder value6 = nativeArray6[i];
					CommonUtils.Swap(ref value6.m_Start, ref value6.m_End);
					nativeArray6[i] = value6;
				}
				if (bufferAccessor.Length != 0)
				{
					DynamicBuffer<ConnectedNode> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						ConnectedNode value7 = dynamicBuffer[j];
						value7.m_CurvePosition = math.saturate(1f - value7.m_CurvePosition);
						dynamicBuffer[j] = value7;
					}
				}
				if (bufferAccessor2.Length != 0)
				{
					DynamicBuffer<ServiceCoverage> dynamicBuffer2 = bufferAccessor2[i];
					for (int k = 0; k < dynamicBuffer2.Length; k++)
					{
						ServiceCoverage value8 = dynamicBuffer2[k];
						value8.m_Coverage = value8.m_Coverage.yx;
						dynamicBuffer2[k] = value8;
					}
				}
				if (bufferAccessor3.Length != 0)
				{
					DynamicBuffer<ResourceAvailability> dynamicBuffer3 = bufferAccessor3[i];
					for (int l = 0; l < dynamicBuffer3.Length; l++)
					{
						ResourceAvailability value9 = dynamicBuffer3[l];
						value9.m_Availability = value9.m_Availability.yx;
						dynamicBuffer3[l] = value9;
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		public ComponentTypeHandle<Edge> __Game_Net_Edge_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Curve> __Game_Net_Curve_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Elevation> __Game_Net_Elevation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Upgraded> __Game_Net_Upgraded_RW_ComponentTypeHandle;

		public ComponentTypeHandle<BuildOrder> __Game_Net_BuildOrder_RW_ComponentTypeHandle;

		public BufferTypeHandle<ConnectedNode> __Game_Net_ConnectedNode_RW_BufferTypeHandle;

		public BufferTypeHandle<ServiceCoverage> __Game_Net_ServiceCoverage_RW_BufferTypeHandle;

		public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Net_Edge_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>();
			__Game_Net_Curve_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>();
			__Game_Net_Elevation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Elevation>();
			__Game_Net_Upgraded_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Upgraded>();
			__Game_Net_BuildOrder_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildOrder>();
			__Game_Net_ConnectedNode_RW_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedNode>();
			__Game_Net_ServiceCoverage_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceCoverage>();
			__Game_Net_ResourceAvailability_RW_BufferTypeHandle = state.GetBufferTypeHandle<ResourceAvailability>();
		}
	}

	private EntityQuery m_RoadEdgeQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RoadEdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Road>(), ComponentType.ReadOnly<Edge>());
		RequireForUpdate(m_RoadEdgeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		FlipOnewayRoadsJob jobData = new FlipOnewayRoadsJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ElevationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Elevation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpgradedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Upgraded_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildOrderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_BuildOrder_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedNodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedNode_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceCoverageType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ServiceCoverage_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourceAvailabilityType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ResourceAvailability_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_RoadEdgeQuery, base.Dependency);
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
	public FlipTrafficHandednessSystem()
	{
	}
}
