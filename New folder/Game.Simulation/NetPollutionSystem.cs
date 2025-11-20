using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
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
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class NetPollutionSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateNetPollutionJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Upgraded> m_UpgradedType;

		[ReadOnly]
		public ComponentTypeHandle<Elevation> m_ElevationType;

		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

		public ComponentTypeHandle<Game.Net.Pollution> m_PollutionType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<NetPollutionData> m_NetPollutionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		public int m_MapSize;

		public int m_AirPollutionTextureSize;

		public int m_NoisePollutionTextureSize;

		public NativeArray<AirPollution> m_AirPollutionMap;

		public NativeArray<NoisePollution> m_NoisePollutionMap;

		[ReadOnly]
		public PollutionParameterData m_PollutionParameters;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			PollutionParameterData pollutionParameters = m_PollutionParameters;
			float t = 4f / (float)kUpdatesPerDay;
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Node> nativeArray2 = chunk.GetNativeArray(ref m_NodeType);
			NativeArray<Game.Net.Pollution> nativeArray3 = chunk.GetNativeArray(ref m_PollutionType);
			NativeArray<Elevation> nativeArray4 = chunk.GetNativeArray(ref m_ElevationType);
			if (nativeArray2.Length != 0)
			{
				NativeArray<Entity> nativeArray5 = chunk.GetNativeArray(m_EntityType);
				BufferAccessor<ConnectedEdge> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					PrefabRef prefabRef = nativeArray[i];
					ref Game.Net.Pollution reference = ref nativeArray3.ElementAt(i);
					reference.m_Accumulation = math.lerp(reference.m_Accumulation, reference.m_Pollution, t);
					reference.m_Pollution = default(float2);
					if (!m_NetPollutionData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						continue;
					}
					Entity entity = nativeArray5[i];
					Node node = nativeArray2[i];
					float2 @float = reference.m_Accumulation * componentData.m_Factors;
					float4 float2 = 0f;
					float num = pollutionParameters.m_NetNoiseRadius;
					Elevation value;
					bool flag = CollectionUtils.TryGet(nativeArray4, i, out value) && math.all(value.m_Elevation < 0f);
					DynamicBuffer<ConnectedEdge> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						ConnectedEdge connectedEdge = dynamicBuffer[j];
						Edge edge = m_EdgeData[connectedEdge.m_Edge];
						bool2 x = new bool2(edge.m_Start == entity, edge.m_End == entity);
						if (!math.any(x))
						{
							continue;
						}
						float3 noisePollution = @float.x;
						if (m_CompositionData.TryGetComponent(connectedEdge.m_Edge, out var componentData2))
						{
							NetCompositionData netCompositionData = m_NetCompositionData[x.x ? componentData2.m_StartNode : componentData2.m_EndNode];
							num = math.max(num, netCompositionData.m_Width * 0.5f);
							if (flag)
							{
								flag = (netCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0;
							}
						}
						if (m_UpgradedData.TryGetComponent(connectedEdge.m_Edge, out var componentData3))
						{
							CheckUpgrades(ref noisePollution, componentData3);
						}
						float2 += new float4(noisePollution, 1f);
					}
					if (!flag)
					{
						if (float2.w != 0f)
						{
							float2 /= float2.w;
							float2.x = (float2.x + float2.z) * 0.5f;
						}
						ApplyPollution(node.m_Position, num, float2.xy, @float.y, ref pollutionParameters);
					}
				}
				return;
			}
			NativeArray<Curve> nativeArray6 = chunk.GetNativeArray(ref m_CurveType);
			NativeArray<Upgraded> nativeArray7 = chunk.GetNativeArray(ref m_UpgradedType);
			NativeArray<Composition> nativeArray8 = chunk.GetNativeArray(ref m_CompositionType);
			for (int k = 0; k < nativeArray6.Length; k++)
			{
				PrefabRef prefabRef2 = nativeArray[k];
				ref Game.Net.Pollution reference2 = ref nativeArray3.ElementAt(k);
				reference2.m_Accumulation = math.lerp(reference2.m_Accumulation, reference2.m_Pollution, t);
				reference2.m_Pollution = default(float2);
				if (!m_NetPollutionData.TryGetComponent(prefabRef2.m_Prefab, out var componentData4))
				{
					continue;
				}
				float num2 = pollutionParameters.m_NetNoiseRadius;
				if (CollectionUtils.TryGet(nativeArray8, k, out var value2))
				{
					NetCompositionData netCompositionData2 = m_NetCompositionData[value2.m_Edge];
					num2 = math.max(num2, netCompositionData2.m_Width * 0.5f);
					if (CollectionUtils.TryGet(nativeArray4, k, out var value3) && math.all(value3.m_Elevation < 0f) && (netCompositionData2.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
					{
						continue;
					}
				}
				Curve curve = nativeArray6[k];
				float2 float3 = reference2.m_Accumulation * componentData4.m_Factors;
				float3 noisePollution2 = float3.x;
				noisePollution2.y *= 2f;
				if (CollectionUtils.TryGet(nativeArray7, k, out var value4))
				{
					CheckUpgrades(ref noisePollution2, value4);
				}
				ApplyPollution(curve, num2, noisePollution2, float3.y, ref pollutionParameters);
			}
		}

		private void CheckUpgrades(ref float3 noisePollution, Upgraded upgraded)
		{
			if ((upgraded.m_Flags.m_Left & upgraded.m_Flags.m_Right & CompositionFlags.Side.SoundBarrier) != 0)
			{
				noisePollution *= new float3(0f, 0.5f, 0f);
			}
			else if ((upgraded.m_Flags.m_Left & CompositionFlags.Side.SoundBarrier) != 0)
			{
				noisePollution *= new float3(0f, 0.5f, 1.5f);
			}
			else if ((upgraded.m_Flags.m_Right & CompositionFlags.Side.SoundBarrier) != 0)
			{
				noisePollution *= new float3(1.5f, 0.5f, 0f);
			}
			if ((upgraded.m_Flags.m_Left & upgraded.m_Flags.m_Right & CompositionFlags.Side.PrimaryBeautification) != 0)
			{
				noisePollution *= new float3(0.5f, 0.5f, 0.5f);
			}
			else if ((upgraded.m_Flags.m_Left & CompositionFlags.Side.PrimaryBeautification) != 0)
			{
				noisePollution *= new float3(0.5f, 0.75f, 1f);
			}
			else if ((upgraded.m_Flags.m_Right & CompositionFlags.Side.PrimaryBeautification) != 0)
			{
				noisePollution *= new float3(1f, 0.75f, 0.5f);
			}
			if ((upgraded.m_Flags.m_Left & upgraded.m_Flags.m_Right & CompositionFlags.Side.SecondaryBeautification) != 0)
			{
				noisePollution *= new float3(0.5f, 0.5f, 0.5f);
			}
			else if ((upgraded.m_Flags.m_Left & CompositionFlags.Side.SecondaryBeautification) != 0)
			{
				noisePollution *= new float3(0.5f, 0.75f, 1f);
			}
			else if ((upgraded.m_Flags.m_Right & CompositionFlags.Side.SecondaryBeautification) != 0)
			{
				noisePollution *= new float3(1f, 0.75f, 0.5f);
			}
			if ((upgraded.m_Flags.m_General & CompositionFlags.General.PrimaryMiddleBeautification) != 0)
			{
				noisePollution *= new float3(0.875f, 0.5f, 0.875f);
			}
			if ((upgraded.m_Flags.m_General & CompositionFlags.General.SecondaryMiddleBeautification) != 0)
			{
				noisePollution *= new float3(0.875f, 0.5f, 0.875f);
			}
		}

		private void ApplyPollution(float3 position, float radius, float2 noisePollution, float airPollution, ref PollutionParameterData pollutionParameters)
		{
			if (airPollution != 0f)
			{
				short amount = (short)(pollutionParameters.m_NetAirMultiplier * airPollution);
				AddAirPollution(position, amount);
			}
			if (math.any(noisePollution != 0f))
			{
				int2 @int = (int2)(pollutionParameters.m_NetNoiseMultiplier * noisePollution / 8f);
				if (radius > pollutionParameters.m_NetNoiseRadius)
				{
					AddNoise(position + new float3(radius * -0.33333f, 0f, radius * -0.33333f), (short)@int.y);
					AddNoise(position + new float3(radius * 0.33333f, 0f, radius * -0.33333f), (short)@int.y);
					AddNoise(position + new float3(radius * -0.33333f, 0f, radius * 0.33333f), (short)@int.y);
					AddNoise(position + new float3(radius * 0.33333f, 0f, radius * 0.33333f), (short)@int.y);
				}
				else
				{
					AddNoise(position, (short)(4 * @int.y));
				}
				AddNoise(position + new float3(0f - radius, 0f, 0f), (short)@int.x);
				AddNoise(position + new float3(radius, 0f, 0f), (short)@int.x);
				AddNoise(position + new float3(0f, 0f, radius), (short)@int.x);
				AddNoise(position + new float3(0f, 0f, 0f - radius), (short)@int.x);
			}
		}

		private void ApplyPollution(Curve curve, float radius, float3 noisePollution, float airPollution, ref PollutionParameterData pollutionParameters)
		{
			if (airPollution != 0f)
			{
				float num = (float)m_MapSize / (float)m_AirPollutionTextureSize;
				int num2 = Mathf.CeilToInt(2f * curve.m_Length / num);
				short amount = (short)(pollutionParameters.m_NetAirMultiplier * airPollution / (float)num2);
				for (int i = 1; i <= num2; i++)
				{
					float3 position = MathUtils.Position(curve.m_Bezier, (float)i / ((float)num2 + 1f));
					AddAirPollution(position, amount);
				}
			}
			if (!math.any(noisePollution != 0f))
			{
				return;
			}
			float num3 = (float)m_MapSize / (float)m_NoisePollutionTextureSize;
			int num4 = Mathf.CeilToInt(2f * curve.m_Length / num3);
			int3 @int = (int3)(pollutionParameters.m_NetNoiseMultiplier * noisePollution / (4f * (float)num4));
			if (radius > pollutionParameters.m_NetNoiseRadius)
			{
				@int.y >>= 1;
			}
			for (int j = 1; j <= num4; j++)
			{
				float t = (float)j / ((float)num4 + 1f);
				float3 @float = MathUtils.Position(curve.m_Bezier, t);
				float3 float2 = MathUtils.Tangent(curve.m_Bezier, t);
				float2 = math.normalize(new float3(0f - float2.z, 0f, float2.x));
				if (radius > pollutionParameters.m_NetNoiseRadius)
				{
					AddNoise(@float + radius * 0.33333f * float2, (short)@int.y);
					AddNoise(@float - radius * 0.33333f * float2, (short)@int.y);
				}
				else
				{
					AddNoise(@float, (short)@int.y);
				}
				if (@int.x != 0)
				{
					AddNoise(@float + radius * float2, (short)@int.x);
				}
				if (@int.z != 0)
				{
					AddNoise(@float - radius * float2, (short)@int.z);
				}
			}
		}

		private void AddAirPollution(float3 position, short amount)
		{
			int2 cell = CellMapSystem<AirPollution>.GetCell(position, m_MapSize, m_AirPollutionTextureSize);
			if (math.all((cell >= 0) & (cell < m_AirPollutionTextureSize)))
			{
				int index = cell.x + cell.y * m_AirPollutionTextureSize;
				AirPollution value = m_AirPollutionMap[index];
				value.Add(amount);
				m_AirPollutionMap[index] = value;
			}
		}

		private void AddNoise(float3 position, short amount)
		{
			float2 cellCoords = CellMapSystem<NoisePollution>.GetCellCoords(position, m_MapSize, m_NoisePollutionTextureSize);
			float2 @float = math.frac(cellCoords);
			float2 float2 = ((@float.x < 0.5f) ? new float2(0f, 1f) : new float2(1f, 0f));
			float2 float3 = ((@float.y < 0.5f) ? new float2(0f, 1f) : new float2(1f, 0f));
			int2 cell = new int2(Mathf.FloorToInt(cellCoords.x - float2.y), Mathf.FloorToInt(cellCoords.y - float3.y));
			AddNoiseSingle(cell, (short)((0.5 + (double)float2.x - (double)@float.x) * (0.5 + (double)float3.x - (double)@float.y) * (double)amount));
			cell.x++;
			AddNoiseSingle(cell, (short)((-0.5 + (double)float2.y + (double)@float.x) * (0.5 + (double)float3.x - (double)@float.y) * (double)amount));
			cell.y++;
			AddNoiseSingle(cell, (short)((-0.5 + (double)float2.y + (double)@float.x) * (-0.5 + (double)float3.y + (double)@float.y) * (double)amount));
			cell.x--;
			AddNoiseSingle(cell, (short)((0.5 + (double)float2.x - (double)@float.x) * (-0.5 + (double)float3.y + (double)@float.y) * (double)amount));
		}

		private void AddNoiseSingle(int2 cell, short amount)
		{
			if (math.all((cell >= 0) & (cell < m_NoisePollutionTextureSize)))
			{
				int index = cell.x + cell.y * m_NoisePollutionTextureSize;
				NoisePollution value = m_NoisePollutionMap[index];
				value.Add(amount);
				m_NoisePollutionMap[index] = value;
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
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Upgraded> __Game_Net_Upgraded_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Elevation> __Game_Net_Elevation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Net.Pollution> __Game_Net_Pollution_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetPollutionData> __Game_Prefabs_NetPollutionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Upgraded>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Elevation>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Pollution_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Pollution>();
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Prefabs_NetPollutionData_RO_ComponentLookup = state.GetComponentLookup<NetPollutionData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 128;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_PollutionQuery;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private EntityQuery m_PollutionParameterQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_PollutionQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Net.Pollution>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_PollutionParameterQuery = GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
		RequireForUpdate(m_PollutionQuery);
		RequireForUpdate(m_PollutionParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		m_PollutionQuery.ResetFilter();
		m_PollutionQuery.SetSharedComponentFilter(new UpdateFrame(updateFrame));
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = JobChunkExtensions.Schedule(new UpdateNetPollutionJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpgradedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ElevationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PollutionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Pollution_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetPollutionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetPollutionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AirPollutionMap = m_AirPollutionSystem.GetMap(readOnly: false, out dependencies),
			m_NoisePollutionMap = m_NoisePollutionSystem.GetMap(readOnly: false, out dependencies2),
			m_AirPollutionTextureSize = AirPollutionSystem.kTextureSize,
			m_NoisePollutionTextureSize = NoisePollutionSystem.kTextureSize,
			m_MapSize = CellMapSystem<AirPollution>.kMapSize,
			m_PollutionParameters = m_PollutionParameterQuery.GetSingleton<PollutionParameterData>()
		}, m_PollutionQuery, JobHandle.CombineDependencies(dependencies, dependencies2, base.Dependency));
		m_AirPollutionSystem.AddWriter(jobHandle);
		m_NoisePollutionSystem.AddWriter(jobHandle);
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
	public NetPollutionSystem()
	{
	}
}
