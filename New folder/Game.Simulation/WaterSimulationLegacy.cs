using Colossal.IO.AssetDatabase;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Simulation;

public class WaterSimulationLegacy : IWaterSimulation
{
	[BurstCompile]
	internal struct SourceJobLegacy : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_SourceChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EventChunks;

		[ReadOnly]
		public ComponentTypeHandle<WaterLevelChange> m_ChangeType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<WaterSourceData> m_SourceType;

		[ReadOnly]
		public ComponentLookup<WaterLevelChangeData> m_ChangePrefabDatas;

		public NativeList<WaterSourceCache> m_Cache;

		public float3 m_TerrainOffset;

		private void HandleSource(WaterSourceData source, Game.Objects.Transform transform)
		{
			float3 position = transform.m_Position - m_TerrainOffset;
			WaterSourceCache value = new WaterSourceCache
			{
				m_Height = source.m_Height,
				m_ConstantDepth = source.m_ConstantDepth,
				m_Multiplier = source.m_Multiplier,
				m_Polluted = source.m_Polluted,
				m_Radius = source.m_Radius,
				m_Position = position
			};
			if (source.m_ConstantDepth == 2 || source.m_ConstantDepth == 3)
			{
				float num = source.m_Height;
				WaterLevelTargetType waterLevelTargetType = ((source.m_ConstantDepth == 2) ? WaterLevelTargetType.River : WaterLevelTargetType.Sea);
				for (int i = 0; i < m_EventChunks.Length; i++)
				{
					NativeArray<WaterLevelChange> nativeArray = m_EventChunks[i].GetNativeArray(ref m_ChangeType);
					NativeArray<PrefabRef> nativeArray2 = m_EventChunks[i].GetNativeArray(ref m_PrefabType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						WaterLevelChange waterLevelChange = nativeArray[j];
						Entity prefab = nativeArray2[j].m_Prefab;
						if (m_ChangePrefabDatas.HasComponent(prefab))
						{
							WaterLevelChangeData waterLevelChangeData = m_ChangePrefabDatas[prefab];
							if (SourceMatchesDirection(source, transform, waterLevelChange.m_Direction) && (waterLevelChangeData.m_TargetType & waterLevelTargetType) != WaterLevelTargetType.None)
							{
								num += source.m_Multiplier * waterLevelChange.m_Intensity;
							}
						}
					}
				}
				value.m_Height = num;
			}
			m_Cache.Add(in value);
		}

		public void Execute()
		{
			m_Cache.Clear();
			for (int i = 0; i < m_SourceChunks.Length; i++)
			{
				NativeArray<WaterSourceData> nativeArray = m_SourceChunks[i].GetNativeArray(ref m_SourceType);
				NativeArray<Game.Objects.Transform> nativeArray2 = m_SourceChunks[i].GetNativeArray(ref m_TransformType);
				if (nativeArray2.Length > 0)
				{
					for (int j = 0; j < nativeArray.Length; j++)
					{
						HandleSource(nativeArray[j], nativeArray2[j]);
					}
				}
			}
		}
	}

	public float m_TimeStep = 0.03f;

	public float m_FlowSpeed = 0.003f;

	public float m_ConstantDepthDepth = 200f;

	private ComputeShader m_UpdateShader;

	private int m_VelocityKernel;

	private int m_DownsampleKernel;

	private int m_VerticalBlurKernel;

	private int m_HorizontalBlurKernel;

	private int m_FlowPostProcessKernel;

	private int m_DepthKernel;

	private int m_CopyToHeightmapKernel;

	private int m_RestoreHeightFromHeightmapKernel;

	private int m_AddKernel;

	private int m_AddConstantKernel;

	private int m_EvaporateKernel;

	private int m_ResetKernel;

	private int m_ResetActiveKernel;

	private int m_ResetToLevelKernel;

	private int m_LoadKernel;

	private int m_LoadFlowMapKernel;

	private int m_AddBorderKernel;

	private int m_ID_AddPosition;

	private int m_ID_AddRadius;

	private int m_ID_AddAmount;

	private int m_ID_AddPolluted;

	private int m_ID_AreaX;

	private int m_ID_AreaY;

	private int m_ID_CellsPerArea;

	private int m_ID_AreaCountX;

	private int m_ID_AreaCountY;

	private int m_ID_Evaporation;

	private int m_ID_CurrentActiveIndices;

	private int m_ID_RainConstant;

	private int m_ID_TerrainScale;

	private int m_ID_Timestep;

	private int m_ID_Fluidness;

	private int m_ID_Damping;

	private int m_ID_FlowInterpolationFatcor;

	private int m_ID_CellSize;

	private int m_ID_SoilWaterDepthConstant;

	private int m_ID_SoilOutputMultiplier;

	private int m_ID_AddBorderPosition;

	private int m_ID_PollutionDecayRate;

	private int m_ID_Previous;

	private int m_ID_Result;

	private int m_ID_Terrain;

	private int m_ID_TerrainLod;

	private int m_ID_MaxVelocity;

	private int m_ID_RestoreHeightMinWaterHeight;

	private int m_ID_Active;

	private WaterSystem m_waterSystem;

	private TerrainSystem m_TerrainSystem;

	private CommandBuffer m_CommandBuffer;

	public float MaxVelocity { get; set; } = 7f;

	public float Damping { get; set; } = 0.995f;

	public float Evaporation { get; set; } = 0.0001f;

	public float RainConstant { get; set; } = 5E-05f;

	public float PollutionDecayRate { get; set; } = 0.001f;

	public float Fluidness { get; set; } = 0.1f;

	public float WindVelocityScale { get; set; } = 0.08f;

	public float WaterSourceSpeed { get; set; } = 2f;

	private ActiveWaterTilesHelper ActiveTilesHelper => m_waterSystem.WaterSimActiveTilesHelper;

	public WaterSimulationLegacy(WaterSystem waterSystem, TerrainSystem terrainSystem)
	{
		m_waterSystem = waterSystem;
		m_TerrainSystem = terrainSystem;
	}

	public void OnDestroy()
	{
	}

	public void InitShader()
	{
		m_UpdateShader = AssetDatabase.global.resources.shaders.waterUpdateLegacy;
		m_VelocityKernel = m_UpdateShader.FindKernel("VelocityUpdate");
		m_DownsampleKernel = m_UpdateShader.FindKernel("CSDownsample");
		m_VerticalBlurKernel = m_UpdateShader.FindKernel("CSVerticalBlur");
		m_HorizontalBlurKernel = m_UpdateShader.FindKernel("CSHorizontalBlur");
		m_FlowPostProcessKernel = m_UpdateShader.FindKernel("CSFlowPostProcess");
		m_DepthKernel = m_UpdateShader.FindKernel("DepthUpdate");
		m_CopyToHeightmapKernel = m_UpdateShader.FindKernel("CopyToHeightmap");
		m_RestoreHeightFromHeightmapKernel = m_UpdateShader.FindKernel("RestoreHeightFromHeightmap");
		m_AddKernel = m_UpdateShader.FindKernel("Add");
		m_AddConstantKernel = m_UpdateShader.FindKernel("AddConstant");
		m_EvaporateKernel = m_UpdateShader.FindKernel("Evaporate");
		m_ResetKernel = m_UpdateShader.FindKernel("Reset");
		m_ResetActiveKernel = m_UpdateShader.FindKernel("ResetActive");
		m_ResetToLevelKernel = m_UpdateShader.FindKernel("ResetToLevel");
		m_LoadKernel = m_UpdateShader.FindKernel("Load");
		m_AddBorderKernel = m_UpdateShader.FindKernel("AddBorder");
		m_ID_AddAmount = Shader.PropertyToID("addAmount");
		m_ID_AddPolluted = Shader.PropertyToID("addPolluted");
		m_ID_AddPosition = Shader.PropertyToID("addPosition");
		m_ID_AddRadius = Shader.PropertyToID("addRadius");
		m_ID_AreaX = Shader.PropertyToID("areax");
		m_ID_AreaY = Shader.PropertyToID("areay");
		m_ID_CellsPerArea = Shader.PropertyToID("cellsPerArea");
		m_ID_AreaCountX = Shader.PropertyToID("areaCountX");
		m_ID_AreaCountY = Shader.PropertyToID("areaCountY");
		m_ID_Evaporation = Shader.PropertyToID("evaporation");
		m_ID_CurrentActiveIndices = Shader.PropertyToID("_CurrentActiveIndices");
		m_ID_RainConstant = Shader.PropertyToID("rainConstant");
		m_ID_TerrainScale = Shader.PropertyToID("terrainScale");
		m_ID_Timestep = Shader.PropertyToID("timestep");
		m_ID_Fluidness = Shader.PropertyToID("fluidness");
		m_ID_Damping = Shader.PropertyToID("damping");
		m_ID_CellSize = Shader.PropertyToID("cellSize");
		m_ID_FlowInterpolationFatcor = Shader.PropertyToID("flowInterpolationFatcor");
		m_ID_PollutionDecayRate = Shader.PropertyToID("pollutionDecayRate");
		m_ID_AddBorderPosition = Shader.PropertyToID("addBorderPosition");
		m_ID_RestoreHeightMinWaterHeight = Shader.PropertyToID("restoreHeightMinWaterHeight");
		m_ID_Previous = Shader.PropertyToID("_Previous");
		m_ID_Result = Shader.PropertyToID("_Result");
		m_ID_Terrain = Shader.PropertyToID("_Terrain");
		m_ID_TerrainLod = Shader.PropertyToID("_TerrainLod");
		m_ID_Active = Shader.PropertyToID("_Active");
		m_ID_MaxVelocity = Shader.PropertyToID("maxVelo");
		m_ID_SoilWaterDepthConstant = Shader.PropertyToID("soilWaterDepthConstant");
		m_ID_SoilOutputMultiplier = Shader.PropertyToID("soilOutputMultiplier");
	}

	private bool BorderCircleIntersection(bool isX, bool isPositive, float2 center, float radius, out int2 result)
	{
		float num = (float)WaterSystem.kMapSize / 2f;
		float num2 = radius * radius;
		float num3 = math.abs((isX ? center.x : center.y) - (isPositive ? num : (0f - num)));
		float num4 = num2 - num3 * num3;
		if (num4 < 0f)
		{
			result = default(int2);
			return false;
		}
		float num5 = (isX ? center.y : center.x);
		float num6 = math.sqrt(num4);
		float2 @float = new float2(num5 - num6 + num, num5 + num6 + num);
		result = new int2(Mathf.FloorToInt((float)m_waterSystem.TextureSize.x * math.saturate(@float.x / (float)WaterSystem.kMapSize)), Mathf.CeilToInt((float)m_waterSystem.TextureSize.y * math.saturate(@float.y / (float)WaterSystem.kMapSize)));
		int num7 = (isX ? m_waterSystem.TextureSize.y : m_waterSystem.TextureSize.x) - 2;
		if (isX && isPositive)
		{
			num7++;
		}
		result.y = math.min(result.y, num7);
		result.x = math.min(result.x, result.y);
		return true;
	}

	public void SourceStep(CommandBuffer cmd, NativeList<WaterSourceCache> LastFrameSourceCache)
	{
		int num = 0;
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.SourceStep)))
		{
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, WaterSystem.kCellSize);
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.positionOffset.y, 0f, 0f));
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Timestep, m_waterSystem.GetTimeStep());
			float num2 = float.MaxValue;
			foreach (WaterSourceCache item in LastFrameSourceCache)
			{
				float3 position = item.m_Position;
				float2 center = position.xz + m_TerrainSystem.positionOffset.xz;
				ComputeShader updateShader = m_UpdateShader;
				int iD_AddPosition = m_ID_AddPosition;
				position = item.m_Position;
				cmd.SetComputeVectorParam(updateShader, iD_AddPosition, new float4(position.xz, 0f, 0f));
				cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddRadius, item.m_Radius);
				int num3 = Mathf.CeilToInt(item.m_Radius / WaterSystem.kCellSize);
				num3 = 2 * num3 + 1;
				if (item.m_ConstantDepth == 1)
				{
					num++;
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddConstantKernel, m_ID_Result, m_waterSystem.WaterTexture);
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddConstantKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
					cmd.SetComputeBufferParam(m_UpdateShader, m_AddConstantKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
					cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddAmount, item.m_Height);
					cmd.DispatchCompute(m_UpdateShader, m_AddConstantKernel, num3, num3, 1);
				}
				else if (item.m_ConstantDepth == 0)
				{
					num++;
					cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddAmount, (float)m_waterSystem.SimulationCycleSteps * item.m_Multiplier * item.m_Height);
					cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddPolluted, item.m_Polluted);
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddKernel, m_ID_Result, m_waterSystem.WaterTexture);
					cmd.SetComputeBufferParam(m_UpdateShader, m_AddKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
					cmd.DispatchCompute(m_UpdateShader, m_AddKernel, num3, num3, 1);
				}
				else if (item.m_ConstantDepth == 2 || item.m_ConstantDepth == 3)
				{
					num2 = math.min(num2, item.m_Height);
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddBorderKernel, m_ID_Result, m_waterSystem.WaterTexture);
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddBorderKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
					cmd.SetComputeBufferParam(m_UpdateShader, m_AddBorderKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
					int4 v = default(int4);
					if (BorderCircleIntersection(isX: false, isPositive: false, center, item.m_Radius, out var result))
					{
						num++;
						v.x = result.x;
						v.y = 0;
						cmd.SetComputeVectorParam(m_UpdateShader, m_ID_AddBorderPosition, new float4(v));
						cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddAmount, item.m_Height);
						cmd.DispatchCompute(m_UpdateShader, m_AddBorderKernel, result.y - result.x + 1, 1, 1);
					}
					if (BorderCircleIntersection(isX: false, isPositive: true, center, item.m_Radius, out result))
					{
						num++;
						v.x = result.x;
						v.y = m_waterSystem.TextureSize.y - 1;
						cmd.SetComputeVectorParam(m_UpdateShader, m_ID_AddBorderPosition, new float4(v));
						cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddAmount, item.m_Height);
						cmd.DispatchCompute(m_UpdateShader, m_AddBorderKernel, result.y - result.x + 1, 1, 1);
					}
					if (BorderCircleIntersection(isX: true, isPositive: false, center, item.m_Radius, out result))
					{
						num++;
						v.x = 0;
						v.y = result.x;
						cmd.SetComputeVectorParam(m_UpdateShader, m_ID_AddBorderPosition, new float4(v));
						cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddAmount, item.m_Height);
						cmd.DispatchCompute(m_UpdateShader, m_AddBorderKernel, 1, 1, result.y - result.x + 1);
					}
					if (BorderCircleIntersection(isX: true, isPositive: true, center, item.m_Radius, out result))
					{
						num++;
						v.x = m_waterSystem.TextureSize.x - 1;
						v.y = result.x;
						cmd.SetComputeVectorParam(m_UpdateShader, m_ID_AddBorderPosition, new float4(v));
						cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddAmount, item.m_Height);
						cmd.DispatchCompute(m_UpdateShader, m_AddBorderKernel, 1, 1, result.y - result.x + 1);
					}
				}
			}
			if (num2 != float.MaxValue)
			{
				Shader.SetGlobalVector("colossal_WaterParams", new Vector4(num2, 0f, 0f, 0f));
				m_waterSystem.SeaLevel = num2;
			}
		}
	}

	public void EvaporateStep(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.EvaporateStep)))
		{
			bool num = m_waterSystem.HasActiveGridSizeChanged(cmd);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, WaterSystem.kCellSize);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountX, m_waterSystem.TextureSize.x / m_waterSystem.GridSize);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountY, m_waterSystem.TextureSize.y / m_waterSystem.GridSize);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_MaxVelocity, MaxVelocity);
			cmd.SetComputeBufferParam(m_UpdateShader, m_EvaporateKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
			cmd.SetComputeTextureParam(m_UpdateShader, m_EvaporateKernel, m_ID_Result, m_waterSystem.WaterTexture);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Timestep, m_waterSystem.GetTimeStep());
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, m_waterSystem.GridSize);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Evaporation, Evaporation);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_RainConstant, RainConstant);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_PollutionDecayRate, PollutionDecayRate);
			bool flag = num | m_waterSystem.IsNewMap;
			if (flag)
			{
				cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Timestep, m_TimeStep);
			}
			ActiveTilesHelper.UpdateActiveIndices(flag || !m_waterSystem.UseActiveCellsCulling);
			cmd.SetComputeBufferParam(m_UpdateShader, m_EvaporateKernel, m_ID_CurrentActiveIndices, ActiveTilesHelper.GetActiveTilesIndices());
			if (ActiveTilesHelper.numThreadGroupsTotal > 0)
			{
				cmd.DispatchCompute(m_UpdateShader, m_EvaporateKernel, ActiveTilesHelper.numThreadGroupsX, ActiveTilesHelper.numThreadGroupsY, ActiveTilesHelper.numThreadGroupsY);
			}
		}
	}

	public void VelocityStep(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.VelocityStep)))
		{
			cmd.SetComputeTextureParam(m_UpdateShader, m_ResetKernel, m_ID_Result, m_waterSystem.WaterRenderTexture);
			cmd.DispatchCompute(m_UpdateShader, m_ResetKernel, m_waterSystem.TextureSize.x / 16, m_waterSystem.TextureSize.y / 16, 1);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.xy, 0f, 0f));
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, WaterSystem.kCellSize);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_Previous, m_waterSystem.WaterTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_Result, m_waterSystem.WaterRenderTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, "_DownscaledResult", m_waterSystem.FlowDownScaled(0));
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, m_waterSystem.GridSize);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Fluidness, Fluidness);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Damping, Damping);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_FlowInterpolationFatcor, m_waterSystem.IsNewMap ? 1f : 0.1f);
			int y = (m_waterSystem.TextureSize / m_waterSystem.GridSize).y;
			_ = 0;
			cmd.SetComputeBufferParam(m_UpdateShader, m_VelocityKernel, m_ID_CurrentActiveIndices, ActiveTilesHelper.GetActiveTilesIndices());
			if (ActiveTilesHelper.numThreadGroupsTotal > 0)
			{
				cmd.DispatchCompute(m_UpdateShader, m_VelocityKernel, ActiveTilesHelper.numThreadGroupsX, ActiveTilesHelper.numThreadGroupsY, ActiveTilesHelper.numThreadGroupsY);
			}
		}
	}

	public void DepthStep(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.DepthStep)))
		{
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, WaterSystem.kCellSize);
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthKernel, m_ID_Previous, m_waterSystem.WaterRenderTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthKernel, m_ID_Result, m_waterSystem.WaterTexture);
			cmd.SetComputeBufferParam(m_UpdateShader, m_DepthKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, m_waterSystem.GridSize);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountX, m_waterSystem.TextureSize.x / m_waterSystem.GridSize);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountY, m_waterSystem.TextureSize.y / m_waterSystem.GridSize);
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.heightScaleOffset.y, 0f, 0f));
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			int y = (m_waterSystem.TextureSize / m_waterSystem.GridSize).y;
			_ = 0;
			cmd.SetComputeBufferParam(m_UpdateShader, m_DepthKernel, m_ID_CurrentActiveIndices, ActiveTilesHelper.GetActiveTilesIndices());
			if (ActiveTilesHelper.numThreadGroupsTotal > 0)
			{
				cmd.DispatchCompute(m_UpdateShader, m_DepthKernel, ActiveTilesHelper.numThreadGroupsX, ActiveTilesHelper.numThreadGroupsY, ActiveTilesHelper.numThreadGroupsY);
			}
			if (m_waterSystem.FlowMapNumDownscale > 0)
			{
				int2 @int = m_waterSystem.TextureSize / 2 / 8;
				int num = m_waterSystem.FlowMapNumDownscale - 1;
				for (int i = 0; i < num; i++)
				{
					@int /= 2;
					cmd.SetComputeTextureParam(m_UpdateShader, m_DownsampleKernel, m_ID_Previous, m_waterSystem.FlowDownScaled(i));
					cmd.SetComputeTextureParam(m_UpdateShader, m_DownsampleKernel, m_ID_Result, m_waterSystem.FlowDownScaled(i + 1));
					cmd.DispatchCompute(m_UpdateShader, m_DownsampleKernel, @int.x, @int.y, 1);
				}
				if (m_waterSystem.BlurFlowMap && m_waterSystem.FlowMapNumDownscale > 1)
				{
					cmd.SetComputeTextureParam(m_UpdateShader, m_VerticalBlurKernel, m_ID_Previous, m_waterSystem.FlowDownScaled(m_waterSystem.FlowMapNumDownscale - 1));
					cmd.SetComputeTextureParam(m_UpdateShader, m_VerticalBlurKernel, m_ID_Result, m_waterSystem.FlowDownScaled(m_waterSystem.FlowMapNumDownscale - 2));
					cmd.DispatchCompute(m_UpdateShader, m_VerticalBlurKernel, @int.x, @int.y, 1);
					cmd.SetComputeTextureParam(m_UpdateShader, m_HorizontalBlurKernel, m_ID_Previous, m_waterSystem.FlowDownScaled(m_waterSystem.FlowMapNumDownscale - 2));
					cmd.SetComputeTextureParam(m_UpdateShader, m_HorizontalBlurKernel, m_ID_Result, m_waterSystem.FlowDownScaled(m_waterSystem.FlowMapNumDownscale - 1));
					cmd.DispatchCompute(m_UpdateShader, m_HorizontalBlurKernel, @int.x, @int.y, 1);
				}
				if (m_waterSystem.FlowPostProcess)
				{
					cmd.SetComputeTextureParam(m_UpdateShader, m_FlowPostProcessKernel, m_ID_Result, m_waterSystem.FlowDownScaled(m_waterSystem.FlowMapNumDownscale - 1));
					cmd.SetComputeFloatParam(m_UpdateShader, "maxFlowlengthForRender", m_waterSystem.MaxFlowlengthForRender);
					cmd.SetComputeFloatParam(m_UpdateShader, "postFlowspeedMultiplier", m_waterSystem.PostFlowspeedMultiplier);
					cmd.DispatchCompute(m_UpdateShader, m_FlowPostProcessKernel, @int.x, @int.y, 1);
				}
			}
		}
	}

	public static bool SourceMatchesDirection(WaterSourceData source, Game.Objects.Transform transform, float2 direction)
	{
		if (source.m_ConstantDepth != 2 && source.m_ConstantDepth != 3)
		{
			return false;
		}
		if (math.abs(transform.m_Position.x) > math.abs(transform.m_Position.z))
		{
			return math.sign(transform.m_Position.x) != math.sign(direction.x);
		}
		return math.sign(transform.m_Position.z) != math.sign(direction.y);
	}
}
