using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
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

namespace Game.Buildings;

[CompilerGenerated]
public class WaterPoweredInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct WaterPoweredInitializeJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubNet> m_SubnetType;

		public ComponentTypeHandle<WaterPowered> m_WaterPoweredType;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PlaceableNetData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public ComponentLookup<TerrainComposition> m_TerrainCompositionData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<WaterPowered> nativeArray = chunk.GetNativeArray(ref m_WaterPoweredType);
			BufferAccessor<Game.Net.SubNet> bufferAccessor = chunk.GetBufferAccessor(ref m_SubnetType);
			if (chunk.Has(ref m_TempType) || chunk.Has(ref m_CreatedType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					WaterPowered waterPowered = nativeArray[i];
					DynamicBuffer<Game.Net.SubNet> subNets = bufferAccessor[i];
					waterPowered.m_Length = 0f;
					waterPowered.m_Height = 0f;
					waterPowered.m_Estimate = 0f;
					CalculateWaterPowered(ref waterPowered, subNets);
					waterPowered.m_Height /= math.max(1f, waterPowered.m_Length);
					nativeArray[i] = waterPowered;
				}
			}
		}

		private void CalculateWaterPowered(ref WaterPowered waterPowered, DynamicBuffer<Game.Net.SubNet> subNets)
		{
			for (int i = 0; i < subNets.Length; i++)
			{
				Entity subNet = subNets[i].m_SubNet;
				PrefabRef prefabRef = m_PrefabData[subNet];
				if (m_CurveData.TryGetComponent(subNet, out var componentData) && m_CompositionData.TryGetComponent(subNet, out var componentData2) && m_PlaceableNetData.TryGetComponent(prefabRef.m_Prefab, out var componentData3) && m_NetCompositionData.TryGetComponent(componentData2.m_Edge, out var componentData4) && m_TerrainCompositionData.TryGetComponent(componentData2.m_Edge, out var componentData5) && (componentData3.m_PlacementFlags & (PlacementFlags.FlowLeft | PlacementFlags.FlowRight)) != PlacementFlags.None)
				{
					CalculateWaterPowered(ref waterPowered, componentData, componentData3, componentData4, componentData5);
				}
			}
		}

		private void CalculateWaterPowered(ref WaterPowered waterPowered, Curve curve, PlaceableNetData placeableData, NetCompositionData compositionData, TerrainComposition terrainComposition)
		{
			int num = math.max(1, Mathf.RoundToInt(curve.m_Length * m_WaterSurfaceData.scale.x));
			bool test = (placeableData.m_PlacementFlags & PlacementFlags.FlowLeft) != 0;
			float num2 = 0f;
			float num3 = 0f;
			for (int i = 0; i < num; i++)
			{
				float t = ((float)i + 0.5f) / (float)num;
				float3 worldPosition = MathUtils.Position(curve.m_Bezier, t);
				float3 @float = MathUtils.Tangent(curve.m_Bezier, t);
				float2 y = math.normalizesafe(math.select(MathUtils.Right(@float.xz), MathUtils.Left(@float.xz), test));
				worldPosition.y += compositionData.m_SurfaceHeight.min + terrainComposition.m_MaxHeightOffset.y;
				WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, worldPosition, out var terrainHeight, out var waterHeight, out var waterDepth);
				float2 x = WaterUtils.SampleVelocity(ref m_WaterSurfaceData, worldPosition);
				num2 += math.max(0f, worldPosition.y - terrainHeight);
				num3 += math.dot(x, y) * waterDepth * math.max(0f, worldPosition.y - waterHeight);
			}
			waterPowered.m_Length += curve.m_Length;
			waterPowered.m_Height += num2 * curve.m_Length / (float)num;
			waterPowered.m_Estimate += num3 * curve.m_Length / (float)num;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferTypeHandle;

		public ComponentTypeHandle<WaterPowered> __Game_Buildings_WaterPowered_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TerrainComposition> __Game_Prefabs_TerrainComposition_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubNet>(isReadOnly: true);
			__Game_Buildings_WaterPowered_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPowered>();
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_TerrainComposition_RO_ComponentLookup = state.GetComponentLookup<TerrainComposition>(isReadOnly: true);
		}
	}

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_WaterPoweredQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_WaterPoweredQuery = GetEntityQuery(ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<WaterPowered>(), ComponentType.Exclude<ServiceUpgrade>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_WaterPoweredQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new WaterPoweredInitializeJob
		{
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubnetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubNet_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_WaterPoweredType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterPowered_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TerrainComposition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetVelocitiesSurfaceData(out deps)
		}, m_WaterPoweredQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
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
	public WaterPoweredInitializeSystem()
	{
	}
}
