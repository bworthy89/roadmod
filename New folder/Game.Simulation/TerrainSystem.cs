using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.AssetPipeline.Native;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Mathematics;
using Colossal.Rendering;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Rendering.Utilities;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Simulation;

[FormerlySerializedAs("Colossal.Terrain.TerrainSystem, Game")]
[CompilerGenerated]
public class TerrainSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public static class ShaderID
	{
		public static readonly int _BlurTempHorz = Shader.PropertyToID("_BlurTempHorz");

		public static readonly int _AvgTerrainHeightsTemp = Shader.PropertyToID("_AvgTerrainHeightsTemp");

		public static readonly int _DebugSmooth = Shader.PropertyToID("_DebugSmooth");

		public static readonly int _Heightmap = Shader.PropertyToID("_Heightmap");

		public static readonly int _HeightmapDownscaled = Shader.PropertyToID("_HeightmapDownscaled");

		public static readonly int _BrushTexture = Shader.PropertyToID("_BrushTexture");

		public static readonly int _WorldTexture = Shader.PropertyToID("_WorldTexture");

		public static readonly int _WaterTexture = Shader.PropertyToID("_WaterTexture");

		public static readonly int _Range = Shader.PropertyToID("_Range");

		public static readonly int _CenterSizeRotation = Shader.PropertyToID("_CenterSizeRotation");

		public static readonly int _Dims = Shader.PropertyToID("_Dims");

		public static readonly int _BrushData = Shader.PropertyToID("_BrushData");

		public static readonly int _BrushData2 = Shader.PropertyToID("_BrushData2");

		public static readonly int _ClampArea = Shader.PropertyToID("_ClampArea");

		public static readonly int _WorldOffsetScale = Shader.PropertyToID("_WorldOffsetScale");

		public static readonly int _EdgeMaxDifference = Shader.PropertyToID("_EdgeMaxDifference");

		public static readonly int _BuildingLotID = Shader.PropertyToID("_BuildingLots");

		public static readonly int _LanesID = Shader.PropertyToID("_Lanes");

		public static readonly int _TrianglesID = Shader.PropertyToID("_Triangles");

		public static readonly int _EdgesID = Shader.PropertyToID("_Edges");

		public static readonly int _HeightmapID = Shader.PropertyToID("_BaseHeightMap");

		public static readonly int _TerrainScaleOffsetID = Shader.PropertyToID("_TerrainScaleOffset");

		public static readonly int _MapOffsetScaleID = Shader.PropertyToID("_MapOffsetScale");

		public static readonly int _BrushID = Shader.PropertyToID("_Brush");

		public static readonly int _CascadeRangesID = Shader.PropertyToID("colossal_TerrainCascadeRanges");

		public static readonly int _CascadeOffsetScale = Shader.PropertyToID("_CascadeOffsetScale");

		public static readonly int _HeightScaleOffset = Shader.PropertyToID("_HeightScaleOffset");

		public static readonly int _RoadData = Shader.PropertyToID("_RoadData");

		public static readonly int _ClipOffset = Shader.PropertyToID("_ClipOffset");
	}

	public struct BuildingLotDraw
	{
		public float2x4 m_HeightsX;

		public float2x4 m_HeightsZ;

		public float3 m_FlatX0;

		public float3 m_FlatZ0;

		public float3 m_FlatX1;

		public float3 m_FlatZ1;

		public float3 m_Position;

		public float3 m_AxisX;

		public float3 m_AxisZ;

		public float2 m_Size;

		public float4 m_MinLimit;

		public float4 m_MaxLimit;

		public float m_Circular;

		public float m_SmoothingWidth;
	}

	public struct LaneSection
	{
		public Bounds2 m_Bounds;

		public float4x3 m_Left;

		public float4x3 m_Right;

		public float3 m_MinOffset;

		public float3 m_MaxOffset;

		public float2 m_ClipOffset;

		public float m_WidthOffset;

		public float m_MiddleSize;

		public LaneFlags m_Flags;
	}

	public struct LaneDraw
	{
		public float4x3 m_Left;

		public float4x3 m_Right;

		public float4 m_MinOffset;

		public float4 m_MaxOffset;

		public float2 m_WidthOffset;
	}

	public struct AreaTriangle
	{
		public float3 m_PositionA;

		public float3 m_PositionB;

		public float3 m_PositionC;

		public float2 m_NoiseSize;

		public float2 m_HeightDelta;
	}

	public struct AreaEdge
	{
		public float2 m_PositionA;

		public float2 m_PositionB;

		public float2 m_Angles;

		public float m_SideOffset;
	}

	[Flags]
	public enum LaneFlags
	{
		ShiftTerrain = 1,
		ClipTerrain = 2,
		MiddleLeft = 4,
		MiddleRight = 8,
		InverseClipOffset = 0x10,
		Raised = 0x20
	}

	private class CascadeCullInfo
	{
		public JobHandle m_BuildingHandle;

		public NativeList<BuildingLotDraw> m_BuildingRenderList;

		public Material m_LotMaterial;

		public JobHandle m_LaneHandle;

		public NativeList<LaneDraw> m_LaneRenderList;

		public NativeList<LaneDraw> m_LaneRaisedRenderList;

		public Material m_LaneMaterial;

		public Material m_LaneRaisedMaterial;

		public JobHandle m_AreaHandle;

		public NativeList<AreaTriangle> m_TriangleRenderList;

		public NativeList<AreaEdge> m_EdgeRenderList;

		public Material m_AreaMaterial;

		public CascadeCullInfo(Material building, Material lane, Material area)
		{
			m_LotMaterial = new Material(building);
			m_LaneMaterial = new Material(lane);
			m_LaneRaisedMaterial = new Material(lane);
			m_AreaMaterial = new Material(area);
			m_BuildingRenderList = default(NativeList<BuildingLotDraw>);
			m_BuildingHandle = default(JobHandle);
			m_LaneHandle = default(JobHandle);
			m_LaneRenderList = default(NativeList<LaneDraw>);
			m_LaneRaisedRenderList = default(NativeList<LaneDraw>);
			m_TriangleRenderList = default(NativeList<AreaTriangle>);
			m_EdgeRenderList = default(NativeList<AreaEdge>);
			m_AreaHandle = default(JobHandle);
		}
	}

	private struct ClipMapDraw
	{
		public float4x3 m_Left;

		public float4x3 m_Right;

		public float m_Height;

		public float m_OffsetFactor;
	}

	private class TerrainMinMaxMap
	{
		private RenderTexture[] m_IntermediateTex;

		private RenderTexture m_DownsampledDetail;

		private RenderTexture m_ResultTex;

		public NativeArray<half4> MinMaxMap;

		private NativeArray<half4> m_UpdateBuffer;

		private AsyncGPUReadbackRequest m_Current;

		private ComputeShader m_Shader;

		private int2 m_IntermediateSize;

		private int2 m_ResultSize;

		private int4 m_UpdatedArea;

		private int4 m_DebugArea;

		private bool m_Pending;

		private bool m_Updated;

		private bool m_Valid;

		private bool m_Partial;

		private int m_Steps;

		private int m_DetailSteps;

		private int m_BlockSize;

		private int m_DetailBlockSize;

		private int m_ID_WorldTexture;

		private int m_ID_DetailTexture;

		private int m_ID_UpdateArea;

		private int m_ID_WorldOffsetScale;

		private int m_ID_DetailOffsetScale;

		private int m_ID_WorldTextureSizeInvSize;

		private int m_ID_Result;

		private int m_KernalCSTerainMinMax;

		private int m_KernalCSWorldTerainMinMax;

		private int m_KernalCSDownsampleMinMax;

		private int2 m_InitValues = int2.zero;

		private Texture m_AsyncNeeded;

		private List<int4> m_UpdatesRequested = new List<int4>();

		private TerrainSystem m_TerrainSystem;

		private JobHandle m_UpdateJob;

		public bool isValid => m_Valid;

		public bool isUpdated => m_Updated;

		public int size => m_ResultSize.x;

		public int4 UpdateArea => m_UpdatedArea;

		private RenderTexture CreateRenderTexture(string name, int2 size, bool compact)
		{
			RenderTexture renderTexture = new RenderTexture(size.x, size.y, 0, compact ? GraphicsFormat.R16G16_SFloat : GraphicsFormat.R16G16B16A16_SFloat);
			renderTexture.name = name;
			renderTexture.hideFlags = HideFlags.DontSave;
			renderTexture.enableRandomWrite = true;
			renderTexture.wrapMode = TextureWrapMode.Clamp;
			renderTexture.filterMode = FilterMode.Bilinear;
			renderTexture.Create();
			return renderTexture;
		}

		public void Init(int size, int original)
		{
			if (m_IntermediateTex != null && size == m_InitValues.x && original == m_InitValues.y)
			{
				m_UpdateJob.Complete();
				m_UpdateJob = default(JobHandle);
				return;
			}
			Dispose();
			m_InitValues = new int2(size, original);
			m_IntermediateSize = original / 2;
			m_ResultSize = size;
			if (m_ResultSize.x > m_IntermediateSize.x || m_ResultSize.y > m_IntermediateSize.y)
			{
				m_ResultSize = m_IntermediateSize;
				m_Steps = 1;
			}
			else
			{
				m_Steps = math.floorlog2(original) + 1 - (math.floorlog2(size) + 1);
			}
			int num = math.max(math.floorlog2(original) - 2, 1);
			int num2 = (int)math.pow(2f, num - 1);
			m_DetailSteps = math.floorlog2(original) + 1 - num;
			m_BlockSize = (int)math.pow(2f, m_Steps);
			m_DetailBlockSize = (int)math.pow(2f, m_DetailSteps);
			m_IntermediateTex = new RenderTexture[2];
			m_IntermediateTex[0] = CreateRenderTexture("HeightMinMax_Setup0", m_IntermediateSize, compact: true);
			m_IntermediateTex[1] = CreateRenderTexture("HeightMinMax_Setup1", m_IntermediateSize / 2, compact: true);
			m_DownsampledDetail = CreateRenderTexture("HeightMinMax_Detail", num2, compact: true);
			m_ResultTex = CreateRenderTexture("HeightMinMax_Result", m_ResultSize.x, compact: false);
			m_Valid = false;
			m_Partial = false;
			m_Updated = false;
			m_Pending = false;
			MinMaxMap = new NativeArray<half4>(size * size, Allocator.Persistent);
			m_UpdateBuffer = new NativeArray<half4>(size * size, Allocator.Persistent);
			m_Shader = Resources.Load<ComputeShader>("TerrainMinMax");
			m_KernalCSTerainMinMax = m_Shader.FindKernel("CSTerainGenerateMinMax");
			m_KernalCSWorldTerainMinMax = m_Shader.FindKernel("CSTerainWorldGenerateMinMax");
			m_KernalCSDownsampleMinMax = m_Shader.FindKernel("CSDownsampleMinMax");
			m_ID_WorldTexture = Shader.PropertyToID("_WorldTexture");
			m_ID_DetailTexture = Shader.PropertyToID("_DetailHeightTexture");
			m_ID_UpdateArea = Shader.PropertyToID("_UpdateArea");
			m_ID_WorldOffsetScale = Shader.PropertyToID("_WorldOffsetScale");
			m_ID_DetailOffsetScale = Shader.PropertyToID("_DetailOffsetScale");
			m_ID_WorldTextureSizeInvSize = Shader.PropertyToID("_WorldTextureSizeInvSize");
			m_ID_Result = Shader.PropertyToID("ResultMinMax");
		}

		public void Debug(TerrainSystem terrain, Texture map, Texture worldMap)
		{
			using CommandBuffer commandBuffer = new CommandBuffer
			{
				name = "DebugMinMax"
			};
			commandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
			RequestUpdate(terrain, map, worldMap, m_DebugArea, commandBuffer, debug: true);
			Graphics.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Dispose();
		}

		public void UpdateMap(TerrainSystem terrain, Texture map, Texture worldMap)
		{
			m_Valid = false;
			m_Updated = false;
			m_Partial = false;
			m_UpdateJob.Complete();
			m_UpdateJob = default(JobHandle);
			if (m_Pending && !m_Current.done)
			{
				m_Current.WaitForCompletion();
			}
			using (CommandBuffer commandBuffer = new CommandBuffer
			{
				name = "TerrainMinMaxInit"
			})
			{
				m_AsyncNeeded = RequestUpdate(terrain, map, worldMap, new int4(0, 0, map.width, map.height), commandBuffer);
				Graphics.ExecuteCommandBuffer(commandBuffer);
			}
			m_Pending = true;
		}

		private int4 RemapArea(int4 area, int blockSize, int textureWidth, int textureHeight)
		{
			int2 @int = area.xy / new int2(blockSize, blockSize) * new int2(blockSize, blockSize);
			area.zw += area.xy - @int;
			area.xy = @int;
			area.zw = (area.zw + new int2(blockSize - 1, blockSize - 1)) / new int2(blockSize, blockSize) * new int2(blockSize, blockSize);
			if (area.z > textureWidth)
			{
				area.z = textureWidth;
			}
			if (area.x + area.z > textureWidth)
			{
				area.x = textureWidth - area.z;
			}
			if (area.w > textureHeight)
			{
				area.w = textureHeight;
			}
			if (area.y + area.w > textureHeight)
			{
				area.y = textureHeight - area.w;
			}
			return area;
		}

		public bool RequestUpdate(TerrainSystem terrain, Texture map, Texture worldMap, int4 area)
		{
			if (m_Pending || m_Updated)
			{
				m_UpdatesRequested.Add(area);
				m_TerrainSystem = terrain;
				return false;
			}
			int2 @int = area.xy / new int2(m_BlockSize, m_BlockSize) * new int2(m_BlockSize, m_BlockSize);
			area.zw += area.xy - @int;
			area.xy = @int;
			area.zw = (area.zw + new int2(m_BlockSize - 1, m_BlockSize - 1)) / new int2(m_BlockSize, m_BlockSize) * new int2(m_BlockSize, m_BlockSize);
			if (area.z > map.width)
			{
				area.z = map.width;
			}
			if (area.x + area.z > map.width)
			{
				area.x = map.width - area.z;
			}
			if (area.w > map.height)
			{
				area.w = map.height;
			}
			if (area.y + area.w > map.height)
			{
				area.y = map.height - area.w;
			}
			area = RemapArea(area, m_BlockSize, (worldMap != null) ? worldMap.width : map.width, (worldMap != null) ? worldMap.height : map.height);
			using (CommandBuffer commandBuffer = new CommandBuffer
			{
				name = "TerainMinMaxUpdate"
			})
			{
				commandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
				m_AsyncNeeded = RequestUpdate(terrain, map, worldMap, area, commandBuffer);
				m_Pending = true;
				m_Partial = true;
				Graphics.ExecuteCommandBuffer(commandBuffer);
			}
			return true;
		}

		private bool Downsample(CommandBuffer commandBuffer, Texture target, int steps, int4 area, ref int4 updated)
		{
			if (steps == 1)
			{
				return false;
			}
			float4 @float = new float4(area.x, area.y, area.x / 2, area.y / 2);
			int num = 1;
			int2 @int = area.zw / 2;
			int4 int2 = area / 2;
			int2.zw = math.max(int2.zw, new int2(1, 1));
			@int.xy = math.max(@int.xy, new int2(1, 1));
			updated = area / 2;
			updated.zw = math.max(updated.zw, new int2(1, 1));
			Texture texture = m_IntermediateTex[1];
			Texture texture2 = m_IntermediateTex[0];
			do
			{
				Texture texture3 = texture2;
				texture2 = texture;
				texture = texture3;
				if (num == steps - 1)
				{
					texture2 = target;
				}
				@float.xy = int2.xy;
				int2 /= 2;
				int2.zw = math.max(int2.zw, new int2(1, 1));
				@float.zw = int2.xy;
				@int /= 2;
				@int.xy = math.max(@int.xy, new int2(1, 1));
				updated /= 2;
				updated.zw = math.max(updated.zw, new int2(1, 1));
				commandBuffer.SetComputeVectorParam(m_Shader, m_ID_UpdateArea, @float);
				commandBuffer.SetComputeTextureParam(m_Shader, m_KernalCSDownsampleMinMax, m_ID_WorldTexture, texture);
				commandBuffer.SetComputeTextureParam(m_Shader, m_KernalCSDownsampleMinMax, m_ID_Result, texture2);
				commandBuffer.DispatchCompute(m_Shader, m_KernalCSDownsampleMinMax, (@int.x + 7) / 8, (@int.y + 7) / 8, 1);
			}
			while (++num < steps);
			return true;
		}

		private Texture RequestUpdate(TerrainSystem terrain, Texture map, Texture worldMap, int4 area, CommandBuffer commandBuffer, bool debug = false)
		{
			if (!debug)
			{
				m_DebugArea = area;
			}
			bool num = worldMap != null;
			float4 @float = new float4(area.x, area.y, area.x / 2, area.y / 2);
			commandBuffer.SetComputeVectorParam(val: new float4(terrain.heightScaleOffset.y, terrain.heightScaleOffset.x, 0f, 0f), computeShader: m_Shader, nameID: m_ID_WorldOffsetScale);
			int4 updated = int4.zero;
			if (num)
			{
				float4 float2 = new float4((terrain.worldOffset - terrain.playableOffset) / terrain.playableArea, 1f / (float)worldMap.width * (terrain.worldSize / terrain.playableArea));
				float4 valueToClamp = new float4(area.xy * float2.zw + float2.xy, (area.xy + area.zw) * float2.zw + float2.xy);
				if (!(valueToClamp.x > 1f) && !(valueToClamp.z < 0f) && !(valueToClamp.y > 1f) && !(valueToClamp.w < 0f))
				{
					valueToClamp = math.clamp(valueToClamp, float4.zero, new float4(1f, 1f, 1f, 1f));
					valueToClamp.zw -= valueToClamp.xy;
					valueToClamp.xy = math.floor(valueToClamp.xy * new float2(map.width, map.height));
					valueToClamp.zw = math.max(math.ceil(valueToClamp.zw * new float2(map.width, map.height)), new float2(1f, 1f));
					int4 area2 = RemapArea(new int4((int)valueToClamp.x, (int)valueToClamp.y, (int)valueToClamp.z, (int)valueToClamp.w), m_DetailBlockSize, map.width, map.height);
					commandBuffer.SetComputeVectorParam(val: new float4(valueToClamp.x, valueToClamp.y, valueToClamp.x / 2f, valueToClamp.y / 2f), computeShader: m_Shader, nameID: m_ID_UpdateArea);
					commandBuffer.SetComputeTextureParam(m_Shader, m_KernalCSTerainMinMax, m_ID_WorldTexture, map);
					commandBuffer.SetComputeTextureParam(m_Shader, m_KernalCSTerainMinMax, m_ID_Result, m_IntermediateTex[0]);
					commandBuffer.DispatchCompute(m_Shader, m_KernalCSTerainMinMax, (area2.z + 7) / 8, (area2.w + 7) / 8, 1);
					Downsample(commandBuffer, m_DownsampledDetail, m_DetailSteps, area2, ref updated);
				}
				commandBuffer.SetComputeVectorParam(val: new float4(m_DownsampledDetail.width, m_DownsampledDetail.height, 1f / (float)m_DownsampledDetail.width, 1f / (float)m_DownsampledDetail.height), computeShader: m_Shader, nameID: m_ID_WorldTextureSizeInvSize);
				commandBuffer.SetComputeVectorParam(m_Shader, m_ID_DetailOffsetScale, float2);
				commandBuffer.SetComputeVectorParam(m_Shader, m_ID_UpdateArea, @float);
				commandBuffer.SetComputeTextureParam(m_Shader, m_KernalCSWorldTerainMinMax, m_ID_WorldTexture, worldMap);
				commandBuffer.SetComputeTextureParam(m_Shader, m_KernalCSWorldTerainMinMax, m_ID_DetailTexture, m_DownsampledDetail);
				commandBuffer.SetComputeTextureParam(m_Shader, m_KernalCSWorldTerainMinMax, m_ID_Result, (m_Steps == 1) ? m_ResultTex : m_IntermediateTex[0]);
				commandBuffer.DispatchCompute(m_Shader, m_KernalCSWorldTerainMinMax, (area.z + 7) / 8, (area.w + 7) / 8, 1);
			}
			else
			{
				commandBuffer.SetComputeVectorParam(m_Shader, m_ID_UpdateArea, @float);
				commandBuffer.SetComputeTextureParam(m_Shader, m_KernalCSTerainMinMax, m_ID_WorldTexture, map);
				commandBuffer.SetComputeTextureParam(m_Shader, m_KernalCSTerainMinMax, m_ID_Result, (m_Steps == 1) ? m_ResultTex : m_IntermediateTex[0]);
				commandBuffer.DispatchCompute(m_Shader, m_KernalCSTerainMinMax, (area.z + 7) / 8, (area.w + 7) / 8, 1);
			}
			if (!debug)
			{
				m_UpdatedArea = area / 2;
				m_UpdatedArea.zw = math.max(m_UpdatedArea.zw, new int2(1, 1));
			}
			Downsample(commandBuffer, m_ResultTex, m_Steps, area, ref updated);
			if (!debug)
			{
				m_UpdatedArea = updated;
			}
			return m_ResultTex;
		}

		public unsafe void Update()
		{
			m_UpdateJob.Complete();
			m_UpdateJob = default(JobHandle);
			if (m_Pending)
			{
				if (m_AsyncNeeded != null)
				{
					if (m_Partial)
					{
						m_Current = AsyncGPUReadback.RequestIntoNativeArray(ref m_UpdateBuffer, m_AsyncNeeded, 0, m_UpdatedArea.x, m_UpdatedArea.z, m_UpdatedArea.y, m_UpdatedArea.w, 0, 1, GraphicsFormat.R16G16B16A16_SFloat, delegate(AsyncGPUReadbackRequest request)
						{
							m_Pending = false;
							if (!request.hasError)
							{
								m_Valid = true;
								m_Updated = true;
								if (m_Partial)
								{
									int num2 = m_UpdatedArea.y * m_ResultSize.x + m_UpdatedArea.x;
									for (int i = 0; i < m_UpdatedArea.w; i++)
									{
										UnsafeUtility.MemCpy((byte*)MinMaxMap.GetUnsafePtr() + (nint)num2 * (nint)sizeof(float2), (byte*)m_UpdateBuffer.GetUnsafePtr() + (nint)(i * m_UpdatedArea.z) * (nint)sizeof(float2), 8 * m_UpdatedArea.z);
										num2 += m_ResultSize.x;
									}
									m_Partial = false;
								}
							}
						});
					}
					else
					{
						m_Current = AsyncGPUReadback.RequestIntoNativeArray(ref MinMaxMap, m_AsyncNeeded, 0, 0, m_ResultSize.x, 0, m_ResultSize.y, 0, 1, GraphicsFormat.R16G16B16A16_SFloat, delegate(AsyncGPUReadbackRequest request)
						{
							m_Pending = false;
							if (!request.hasError)
							{
								m_Valid = true;
								m_Updated = true;
								if (m_Partial)
								{
									int num2 = m_UpdatedArea.y * m_ResultSize.x + m_UpdatedArea.x;
									for (int i = 0; i < m_UpdatedArea.w; i++)
									{
										UnsafeUtility.MemCpy((byte*)MinMaxMap.GetUnsafePtr() + (nint)num2 * (nint)sizeof(float2), (byte*)m_UpdateBuffer.GetUnsafePtr() + (nint)(i * m_UpdatedArea.z) * (nint)sizeof(float2), 8 * m_UpdatedArea.z);
										num2 += m_ResultSize.x;
									}
									m_Partial = false;
								}
							}
						});
					}
					m_AsyncNeeded = null;
				}
				else
				{
					m_Current.Update();
				}
			}
			else if (!m_Updated && m_UpdatesRequested.Count > 0)
			{
				int4 area = m_UpdatesRequested[0];
				area.zw += area.xy;
				for (int num = 1; num < m_UpdatesRequested.Count; num++)
				{
					area.xy = math.min(area.xy, m_UpdatesRequested[num].xy);
					area.zw = math.max(area.zw, m_UpdatesRequested[num].xy + m_UpdatesRequested[num].zw);
				}
				area.zw -= area.xy;
				area.zw = math.clamp(area.zw, new int2(1, 1), new int2(m_ResultSize.x, m_ResultSize.y));
				RequestUpdate(m_TerrainSystem, m_TerrainSystem.heightmap, m_TerrainSystem.worldHeightmap, area);
				m_TerrainSystem = null;
				m_UpdatesRequested.Clear();
			}
		}

		private unsafe void UpdateMinMax(AsyncGPUReadbackRequest request)
		{
			m_Pending = false;
			if (request.hasError)
			{
				return;
			}
			m_Valid = true;
			m_Updated = true;
			if (m_Partial)
			{
				int num = m_UpdatedArea.y * m_ResultSize.x + m_UpdatedArea.x;
				for (int i = 0; i < m_UpdatedArea.w; i++)
				{
					UnsafeUtility.MemCpy((byte*)MinMaxMap.GetUnsafePtr() + (nint)num * (nint)sizeof(float2), (byte*)m_UpdateBuffer.GetUnsafePtr() + (nint)(i * m_UpdatedArea.z) * (nint)sizeof(float2), 8 * m_UpdatedArea.z);
					num += m_ResultSize.x;
				}
				m_Partial = false;
			}
		}

		public int4 ComsumeUpdate()
		{
			m_Updated = false;
			return m_UpdatedArea;
		}

		public float2 GetMinMax(int4 area)
		{
			float2 result = new float2(999999f, 0f);
			for (int i = 0; i < area.z * area.w; i++)
			{
				int index = (area.y + i / area.z) * m_ResultSize.x + area.x + i % area.z;
				result.x = math.min(result.x, MinMaxMap[index].x);
				result.y = math.max(result.y, MinMaxMap[index].y);
			}
			return result;
		}

		public void RegisterJobUpdate(JobHandle handle)
		{
			m_UpdateJob = JobHandle.CombineDependencies(handle, m_UpdateJob);
		}

		public void Dispose()
		{
			m_Current.WaitForCompletion();
			m_Pending = false;
			m_AsyncNeeded = null;
			m_Updated = false;
			if (m_IntermediateTex != null)
			{
				RenderTexture[] array = m_IntermediateTex;
				for (int i = 0; i < array.Length; i++)
				{
					CoreUtils.Destroy(array[i]);
				}
			}
			CoreUtils.Destroy(m_DownsampledDetail);
			CoreUtils.Destroy(m_ResultTex);
			if (MinMaxMap.IsCreated)
			{
				MinMaxMap.Dispose(m_UpdateJob);
			}
			if (m_UpdateBuffer.IsCreated)
			{
				m_UpdateBuffer.Dispose();
			}
			m_UpdateJob = default(JobHandle);
		}
	}

	private class TerrainDesc
	{
		public Colossal.Hash128 heightMapGuid { get; set; }

		public Colossal.Hash128 diffuseMapGuid { get; set; }

		public float heightScale { get; set; }

		public float heightOffset { get; set; }

		public Colossal.Hash128 worldHeightMapGuid { get; set; }

		public float2 mapSize { get; set; }

		public float2 worldSize { get; set; }

		public float2 worldHeightMinMax { get; set; }

		private static void SupportValueTypesForAOT()
		{
			JSON.SupportTypeForAOT<float2>();
		}
	}

	[BurstCompile]
	private struct CullBuildingLotsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Lot> m_LotHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Elevation> m_ElevationHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stack> m_StackHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AssetStampData> m_PrefabAssetStampData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> m_OverrideTerraform;

		[ReadOnly]
		public BufferLookup<AdditionalBuildingTerraformElement> m_AdditionalLots;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public float4 m_Area;

		public NativeQueue<BuildingUtils.LotInfo>.ParallelWriter Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Buildings.Lot> nativeArray = chunk.GetNativeArray(ref m_LotHandle);
			NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformHandle);
			NativeArray<Game.Objects.Elevation> nativeArray3 = chunk.GetNativeArray(ref m_ElevationHandle);
			NativeArray<Stack> nativeArray4 = chunk.GetNativeArray(ref m_StackHandle);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefHandle);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeHandle);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				PrefabRef prefabRef = nativeArray5[i];
				Game.Objects.Transform transform = nativeArray2[i];
				Game.Objects.Elevation elevation = default(Game.Objects.Elevation);
				if (nativeArray3.Length != 0)
				{
					elevation = nativeArray3[i];
				}
				Game.Buildings.Lot lot = default(Game.Buildings.Lot);
				if (nativeArray.Length != 0)
				{
					lot = nativeArray[i];
				}
				bool flag = m_PrefabBuildingData.HasComponent(prefabRef.m_Prefab);
				bool flag2 = !flag && m_PrefabBuildingExtensionData.HasComponent(prefabRef.m_Prefab);
				bool flag3 = !flag && !flag2 && m_PrefabAssetStampData.HasComponent(prefabRef.m_Prefab);
				bool flag4 = !flag && !flag2 && !flag3 && m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab);
				if (!(flag || flag2 || flag3 || flag4))
				{
					continue;
				}
				ObjectGeometryData objectGeometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
				Bounds2 xz = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, objectGeometryData).xz;
				float2 @float;
				if (flag)
				{
					@float = new float2(m_PrefabBuildingData[prefabRef.m_Prefab].m_LotSize) * 4f;
				}
				else if (flag2)
				{
					BuildingExtensionData buildingExtensionData = m_PrefabBuildingExtensionData[prefabRef.m_Prefab];
					if (!buildingExtensionData.m_External)
					{
						continue;
					}
					@float = new float2(buildingExtensionData.m_LotSize) * 4f;
				}
				else if (flag3)
				{
					@float = new float2(m_PrefabAssetStampData[prefabRef.m_Prefab].m_Size) * 4f;
				}
				else
				{
					if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
					{
						@float = objectGeometryData.m_LegSize.xz * 0.5f + objectGeometryData.m_LegOffset;
					}
					else
					{
						transform.m_Position.xz += MathUtils.Center(objectGeometryData.m_Bounds.xz);
						@float = MathUtils.Size(objectGeometryData.m_Bounds.xz) * 0.5f;
					}
					if (nativeArray4.Length != 0)
					{
						Stack stack = nativeArray4[i];
						transform.m_Position.y += stack.m_Range.min - objectGeometryData.m_Bounds.min.y;
					}
				}
				xz = MathUtils.Expand(xz, ObjectUtils.GetTerrainSmoothingWidth(objectGeometryData) - 8f);
				if (xz.max.x < m_Area.x || xz.min.x > m_Area.z || xz.max.y < m_Area.y || xz.min.y > m_Area.w)
				{
					continue;
				}
				DynamicBuffer<InstalledUpgrade> upgrades = default(DynamicBuffer<InstalledUpgrade>);
				if (bufferAccessor.Length != 0)
				{
					upgrades = bufferAccessor[i];
				}
				bool hasExtensionLots;
				BuildingUtils.LotInfo lotInfo = BuildingUtils.CalculateLotInfo(@float, transform, elevation, lot, prefabRef, upgrades, m_TransformData, m_PrefabRefData, m_ObjectGeometryData, m_OverrideTerraform, m_PrefabBuildingExtensionData, flag4, out hasExtensionLots);
				float terrainSmoothingWidth = ObjectUtils.GetTerrainSmoothingWidth(@float * 2f);
				lotInfo.m_Radius += terrainSmoothingWidth;
				Result.Enqueue(lotInfo);
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
				{
					BuildingUtils.LotInfo value = lotInfo;
					value.m_Extents = MathUtils.Size(objectGeometryData.m_Bounds.xz) * 0.5f;
					float terrainSmoothingWidth2 = ObjectUtils.GetTerrainSmoothingWidth(value.m_Extents * 2f);
					value.m_Position.xz += MathUtils.Center(objectGeometryData.m_Bounds.xz);
					value.m_Position.y += objectGeometryData.m_LegSize.y;
					value.m_MaxLimit = new float4(terrainSmoothingWidth2, terrainSmoothingWidth2, 0f - terrainSmoothingWidth2, 0f - terrainSmoothingWidth2);
					value.m_MinLimit = new float4(-value.m_Extents.xy, value.m_Extents.xy);
					value.m_FrontHeights = default(float3);
					value.m_RightHeights = default(float3);
					value.m_BackHeights = default(float3);
					value.m_LeftHeights = default(float3);
					value.m_FlatX0 = value.m_MinLimit.x * 0.5f;
					value.m_FlatZ0 = value.m_MinLimit.y * 0.5f;
					value.m_FlatX1 = value.m_MinLimit.z * 0.5f;
					value.m_FlatZ1 = value.m_MinLimit.w * 0.5f;
					value.m_Radius = math.length(value.m_Extents) + terrainSmoothingWidth2;
					value.m_Circular = math.select(0f, 1f, (objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != 0);
					Result.Enqueue(value);
				}
				if (m_AdditionalLots.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
				{
					for (int j = 0; j < bufferData.Length; j++)
					{
						AdditionalBuildingTerraformElement additionalBuildingTerraformElement = bufferData[j];
						BuildingUtils.LotInfo value2 = lotInfo;
						value2.m_Position.y += additionalBuildingTerraformElement.m_HeightOffset;
						value2.m_MinLimit = new float4(additionalBuildingTerraformElement.m_Area.min, additionalBuildingTerraformElement.m_Area.max);
						value2.m_FlatX0 = math.max(value2.m_FlatX0, value2.m_MinLimit.x);
						value2.m_FlatZ0 = math.max(value2.m_FlatZ0, value2.m_MinLimit.y);
						value2.m_FlatX1 = math.min(value2.m_FlatX1, value2.m_MinLimit.z);
						value2.m_FlatZ1 = math.min(value2.m_FlatZ1, value2.m_MinLimit.w);
						value2.m_Circular = math.select(0f, 1f, additionalBuildingTerraformElement.m_Circular);
						value2.m_MaxLimit = math.select(value2.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), additionalBuildingTerraformElement.m_DontRaise);
						value2.m_MinLimit = math.select(value2.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), additionalBuildingTerraformElement.m_DontLower);
						Result.Enqueue(value2);
					}
				}
				if (!hasExtensionLots)
				{
					continue;
				}
				for (int k = 0; k < upgrades.Length; k++)
				{
					Entity upgrade = upgrades[k].m_Upgrade;
					PrefabRef prefabRef2 = m_PrefabRefData[upgrade];
					if (!m_PrefabBuildingExtensionData.TryGetComponent(prefabRef2.m_Prefab, out var componentData) || componentData.m_External || !m_OverrideTerraform.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
					{
						continue;
					}
					float3 float2 = m_TransformData[upgrade].m_Position - transform.m_Position;
					float num = 0f;
					if (m_ObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData3))
					{
						bool flag5 = (componentData3.m_Flags & Game.Objects.GeometryFlags.Standing) != 0;
						bool test = ((uint)componentData3.m_Flags & (uint)((!flag5) ? 1 : 256)) != 0;
						num = math.select(0f, 1f, test);
					}
					if (!math.all(componentData2.m_Smooth + float2.xzxz == lotInfo.m_MaxLimit) || num != lotInfo.m_Circular)
					{
						BuildingUtils.LotInfo value3 = lotInfo;
						value3.m_Circular = num;
						value3.m_Position.y += componentData2.m_HeightOffset;
						value3.m_MinLimit = componentData2.m_Smooth + float2.xzxz;
						value3.m_MaxLimit = value3.m_MinLimit;
						value3.m_MinLimit.xy = math.min(new float2(value3.m_FlatX0.y, value3.m_FlatZ0.y), value3.m_MinLimit.xy);
						value3.m_MinLimit.zw = math.max(new float2(value3.m_FlatX1.y, value3.m_FlatZ1.y), value3.m_MinLimit.zw);
						value3.m_MinLimit = math.select(value3.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), componentData2.m_DontLower);
						value3.m_MaxLimit = math.select(value3.m_MaxLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), componentData2.m_DontRaise);
						Result.Enqueue(value3);
					}
					if (m_AdditionalLots.TryGetBuffer(prefabRef2.m_Prefab, out var bufferData2))
					{
						for (int l = 0; l < bufferData2.Length; l++)
						{
							AdditionalBuildingTerraformElement additionalBuildingTerraformElement2 = bufferData2[l];
							BuildingUtils.LotInfo value4 = lotInfo;
							value4.m_Position.y += additionalBuildingTerraformElement2.m_HeightOffset;
							value4.m_MinLimit = new float4(additionalBuildingTerraformElement2.m_Area.min, additionalBuildingTerraformElement2.m_Area.max) + float2.xzxz;
							value4.m_FlatX0 = math.max(value4.m_FlatX0, value4.m_MinLimit.x);
							value4.m_FlatZ0 = math.max(value4.m_FlatZ0, value4.m_MinLimit.y);
							value4.m_FlatX1 = math.min(value4.m_FlatX1, value4.m_MinLimit.z);
							value4.m_FlatZ1 = math.min(value4.m_FlatZ1, value4.m_MinLimit.w);
							value4.m_Circular = math.select(0f, 1f, additionalBuildingTerraformElement2.m_Circular);
							value4.m_MaxLimit = math.select(value4.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), additionalBuildingTerraformElement2.m_DontRaise);
							value4.m_MinLimit = math.select(value4.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), additionalBuildingTerraformElement2.m_DontLower);
							Result.Enqueue(value4);
						}
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DequeBuildingLotsJob : IJob
	{
		[ReadOnly]
		public NativeQueue<BuildingUtils.LotInfo> m_Queue;

		public NativeList<BuildingUtils.LotInfo> m_List;

		public void Execute()
		{
			NativeArray<BuildingUtils.LotInfo> other = m_Queue.ToArray(Allocator.Temp);
			m_List.CopyFrom(in other);
			other.Dispose();
		}
	}

	[BurstCompile]
	private struct CullRoadsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> m_NodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<TerrainComposition> m_TerrainCompositionData;

		[ReadOnly]
		public float4 m_Area;

		public NativeList<LaneSection>.ParallelWriter Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				if (!m_PrefabRefData.HasComponent(entity))
				{
					continue;
				}
				Entity prefab = m_PrefabRefData[entity].m_Prefab;
				if (!m_NetGeometryData.HasComponent(prefab))
				{
					continue;
				}
				NetData net = m_NetData[prefab];
				NetGeometryData netGeometry = m_NetGeometryData[prefab];
				if (m_CompositionData.HasComponent(entity))
				{
					Composition composition = m_CompositionData[entity];
					EdgeGeometry geometry = m_EdgeGeometryData[entity];
					StartNodeGeometry startNodeGeometry = m_StartNodeGeometryData[entity];
					EndNodeGeometry endNodeGeometry = m_EndNodeGeometryData[entity];
					if (math.any(geometry.m_Start.m_Length + geometry.m_End.m_Length > 0.1f))
					{
						NetCompositionData prefabCompositionData = m_PrefabCompositionData[composition.m_Edge];
						TerrainComposition terrainComposition = default(TerrainComposition);
						if (m_TerrainCompositionData.HasComponent(composition.m_Edge))
						{
							terrainComposition = m_TerrainCompositionData[composition.m_Edge];
						}
						AddEdge(geometry, m_Area, net, netGeometry, prefabCompositionData, terrainComposition);
					}
					if (math.any(startNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(startNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
					{
						NetCompositionData prefabCompositionData2 = m_PrefabCompositionData[composition.m_StartNode];
						TerrainComposition terrainComposition2 = default(TerrainComposition);
						if (m_TerrainCompositionData.HasComponent(composition.m_StartNode))
						{
							terrainComposition2 = m_TerrainCompositionData[composition.m_StartNode];
						}
						AddNode(startNodeGeometry.m_Geometry, m_Area, net, netGeometry, prefabCompositionData2, terrainComposition2);
					}
					if (math.any(endNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(endNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
					{
						NetCompositionData prefabCompositionData3 = m_PrefabCompositionData[composition.m_EndNode];
						TerrainComposition terrainComposition3 = default(TerrainComposition);
						if (m_TerrainCompositionData.HasComponent(composition.m_EndNode))
						{
							terrainComposition3 = m_TerrainCompositionData[composition.m_EndNode];
						}
						AddNode(endNodeGeometry.m_Geometry, m_Area, net, netGeometry, prefabCompositionData3, terrainComposition3);
					}
				}
				else if (m_OrphanData.HasComponent(entity))
				{
					Orphan orphan = m_OrphanData[entity];
					Game.Net.Node node = m_NodeData[entity];
					NetCompositionData prefabCompositionData4 = m_PrefabCompositionData[orphan.m_Composition];
					TerrainComposition terrainComposition4 = default(TerrainComposition);
					if (m_TerrainCompositionData.HasComponent(orphan.m_Composition))
					{
						terrainComposition4 = m_TerrainCompositionData[orphan.m_Composition];
					}
					NodeGeometry nodeGeometry = m_NodeGeometryData[entity];
					AddOrphans(node, nodeGeometry, m_Area, net, netGeometry, prefabCompositionData4, terrainComposition4);
				}
			}
		}

		private LaneFlags GetFlags(NetGeometryData netGeometry, NetCompositionData prefabCompositionData)
		{
			LaneFlags laneFlags = (LaneFlags)0;
			if ((netGeometry.m_Flags & Game.Net.GeometryFlags.ClipTerrain) != 0)
			{
				laneFlags |= LaneFlags.ClipTerrain;
			}
			if ((netGeometry.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) != 0)
			{
				laneFlags |= LaneFlags.ShiftTerrain;
			}
			if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.Raised) != 0 || (prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.Raised) != 0)
			{
				laneFlags |= LaneFlags.Raised;
			}
			return laneFlags;
		}

		private void AddEdge(EdgeGeometry geometry, float4 area, NetData net, NetGeometryData netGeometry, NetCompositionData prefabCompositionData, TerrainComposition terrainComposition)
		{
			LaneFlags laneFlags = GetFlags(netGeometry, prefabCompositionData);
			if ((laneFlags & (LaneFlags.ShiftTerrain | LaneFlags.ClipTerrain)) == 0)
			{
				return;
			}
			Bounds2 xz = geometry.m_Bounds.xz;
			if (!math.any(xz.max < area.xy) && !math.any(xz.min > area.zw))
			{
				if ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
				{
					laneFlags |= LaneFlags.InverseClipOffset;
				}
				AddSegment(geometry.m_Start, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags, isStart: true);
				AddSegment(geometry.m_End, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags, isStart: false);
			}
		}

		private void MoveTowards(ref float3 position, float3 other, float amount)
		{
			float3 value = other - position;
			value = MathUtils.Normalize(value, value.xz);
			position += value * amount;
		}

		private void AddNode(EdgeNodeGeometry node, float4 area, NetData net, NetGeometryData netGeometry, NetCompositionData prefabCompositionData, TerrainComposition terrainComposition)
		{
			LaneFlags laneFlags = GetFlags(netGeometry, prefabCompositionData);
			if ((laneFlags & (LaneFlags.ShiftTerrain | LaneFlags.ClipTerrain)) == 0)
			{
				return;
			}
			Bounds2 xz = node.m_Bounds.xz;
			if (math.any(xz.max < area.xy) || math.any(xz.min > area.zw))
			{
				return;
			}
			if (node.m_MiddleRadius > 0f)
			{
				NetCompositionData compositionData = prefabCompositionData;
				float num = 0f;
				float num2 = 0f;
				if ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Elevated) != 0)
				{
					if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.HighTransition) != 0)
					{
						num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
						compositionData.m_Flags.m_General &= ~CompositionFlags.General.Elevated;
						compositionData.m_Flags.m_Left &= ~CompositionFlags.Side.HighTransition;
					}
					else if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.LowTransition) != 0)
					{
						num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
						compositionData.m_Flags.m_General &= ~CompositionFlags.General.Elevated;
						compositionData.m_Flags.m_Left &= ~CompositionFlags.Side.LowTransition;
						compositionData.m_Flags.m_Left |= CompositionFlags.Side.Raised;
					}
					if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.HighTransition) != 0)
					{
						num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
						compositionData.m_Flags.m_General &= ~CompositionFlags.General.Elevated;
						compositionData.m_Flags.m_Right &= ~CompositionFlags.Side.HighTransition;
					}
					else if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.LowTransition) != 0)
					{
						num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
						compositionData.m_Flags.m_General &= ~CompositionFlags.General.Elevated;
						compositionData.m_Flags.m_Right &= ~CompositionFlags.Side.LowTransition;
						compositionData.m_Flags.m_Right |= CompositionFlags.Side.Raised;
					}
				}
				else if ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
				{
					laneFlags |= LaneFlags.InverseClipOffset;
					if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.HighTransition) != 0)
					{
						num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
						compositionData.m_Flags.m_General &= ~CompositionFlags.General.Tunnel;
						compositionData.m_Flags.m_Left &= ~CompositionFlags.Side.HighTransition;
						laneFlags &= ~LaneFlags.InverseClipOffset;
					}
					else if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.LowTransition) != 0)
					{
						num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
						compositionData.m_Flags.m_General &= ~CompositionFlags.General.Tunnel;
						compositionData.m_Flags.m_Left &= ~CompositionFlags.Side.LowTransition;
						compositionData.m_Flags.m_Left |= CompositionFlags.Side.Lowered;
						laneFlags &= ~LaneFlags.InverseClipOffset;
					}
					if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.HighTransition) != 0)
					{
						num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
						compositionData.m_Flags.m_General &= ~CompositionFlags.General.Tunnel;
						compositionData.m_Flags.m_Right &= ~CompositionFlags.Side.HighTransition;
						laneFlags &= ~LaneFlags.InverseClipOffset;
					}
					else if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.LowTransition) != 0)
					{
						num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
						compositionData.m_Flags.m_General &= ~CompositionFlags.General.Tunnel;
						compositionData.m_Flags.m_Right &= ~CompositionFlags.Side.LowTransition;
						compositionData.m_Flags.m_Right |= CompositionFlags.Side.Lowered;
						laneFlags &= ~LaneFlags.InverseClipOffset;
					}
				}
				else
				{
					if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.LowTransition) != 0)
					{
						if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.Raised) != 0)
						{
							num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
							compositionData.m_Flags.m_Left &= ~(CompositionFlags.Side.Raised | CompositionFlags.Side.LowTransition);
						}
						else if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.Lowered) != 0)
						{
							num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
							compositionData.m_Flags.m_Left &= ~(CompositionFlags.Side.Lowered | CompositionFlags.Side.LowTransition);
						}
						else if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.SoundBarrier) != 0)
						{
							num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
							compositionData.m_Flags.m_Left &= ~(CompositionFlags.Side.LowTransition | CompositionFlags.Side.SoundBarrier);
						}
					}
					if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.LowTransition) != 0)
					{
						if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.Raised) != 0)
						{
							num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
							compositionData.m_Flags.m_Right &= ~(CompositionFlags.Side.Raised | CompositionFlags.Side.LowTransition);
						}
						else if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.Lowered) != 0)
						{
							num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
							compositionData.m_Flags.m_Right &= ~(CompositionFlags.Side.Lowered | CompositionFlags.Side.LowTransition);
						}
						else if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.SoundBarrier) != 0)
						{
							num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
							compositionData.m_Flags.m_Right &= ~(CompositionFlags.Side.LowTransition | CompositionFlags.Side.SoundBarrier);
						}
					}
				}
				if (num != 0f)
				{
					num *= math.distance(node.m_Left.m_Left.a.xz, node.m_Middle.a.xz);
				}
				if (num2 != 0f)
				{
					num2 *= math.distance(node.m_Middle.a.xz, node.m_Left.m_Right.a.xz);
				}
				Segment left = node.m_Left;
				left.m_Right = node.m_Middle;
				AddSegment(left, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
				left.m_Left = left.m_Right;
				left.m_Right = node.m_Left.m_Right;
				AddSegment(left, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
				left = node.m_Right;
				left.m_Right = new Bezier4x3(node.m_Middle.d, node.m_Middle.d, node.m_Middle.d, node.m_Middle.d);
				if (num != 0f)
				{
					MoveTowards(ref left.m_Left.a, node.m_Middle.d, num);
					MoveTowards(ref left.m_Left.b, node.m_Middle.d, num);
					MoveTowards(ref left.m_Left.c, node.m_Middle.d, num);
					MoveTowards(ref left.m_Left.d, node.m_Middle.d, num);
				}
				AddSegment(left, net, netGeometry, compositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: false);
				left.m_Left = left.m_Right;
				left.m_Right = node.m_Right.m_Right;
				if (num2 != 0f)
				{
					MoveTowards(ref left.m_Right.a, node.m_Middle.d, num2);
					MoveTowards(ref left.m_Right.b, node.m_Middle.d, num2);
					MoveTowards(ref left.m_Right.c, node.m_Middle.d, num2);
					MoveTowards(ref left.m_Right.d, node.m_Middle.d, num2);
				}
				AddSegment(left, net, netGeometry, compositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: false);
			}
			else if (math.lengthsq(node.m_Left.m_Right.d - node.m_Right.m_Left.d) > 0.0001f)
			{
				Segment left2 = node.m_Left;
				AddSegment(left2, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
				left2.m_Left = left2.m_Right;
				left2.m_Right = node.m_Middle;
				AddSegment(left2, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | (LaneFlags.MiddleLeft | LaneFlags.MiddleRight), isStart: true);
				left2 = node.m_Right;
				AddSegment(left2, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
				left2.m_Right = left2.m_Left;
				left2.m_Left = node.m_Middle;
				AddSegment(left2, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | (LaneFlags.MiddleLeft | LaneFlags.MiddleRight), isStart: true);
			}
			else
			{
				Segment left3 = node.m_Left;
				left3.m_Right = node.m_Middle;
				AddSegment(left3, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
				left3.m_Left = node.m_Middle;
				left3.m_Right = node.m_Right.m_Right;
				AddSegment(left3, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
			}
		}

		private void AddOrphans(Game.Net.Node node, NodeGeometry nodeGeometry, float4 area, NetData net, NetGeometryData netGeometry, NetCompositionData prefabCompositionData, TerrainComposition terrainComposition)
		{
			LaneFlags laneFlags = GetFlags(netGeometry, prefabCompositionData);
			if ((laneFlags & (LaneFlags.ShiftTerrain | LaneFlags.ClipTerrain)) == 0)
			{
				return;
			}
			Segment segment = default(Segment);
			Bounds2 xz = nodeGeometry.m_Bounds.xz;
			if (!math.any(xz.max < area.xy) && !math.any(xz.min > area.zw))
			{
				if ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
				{
					laneFlags |= LaneFlags.InverseClipOffset;
				}
				segment.m_Left.a = new float3(node.m_Position.x - prefabCompositionData.m_Width * 0.5f, node.m_Position.y, node.m_Position.z);
				segment.m_Left.b = new float3(node.m_Position.x - prefabCompositionData.m_Width * 0.5f, node.m_Position.y, node.m_Position.z + prefabCompositionData.m_Width * 0.2761424f);
				segment.m_Left.c = new float3(node.m_Position.x - prefabCompositionData.m_Width * 0.2761424f, node.m_Position.y, node.m_Position.z + prefabCompositionData.m_Width * 0.5f);
				segment.m_Left.d = new float3(node.m_Position.x, node.m_Position.y, node.m_Position.z + prefabCompositionData.m_Width * 0.5f);
				segment.m_Right = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position);
				segment.m_Length = new float2(prefabCompositionData.m_Width * (MathF.PI / 2f), 0f);
				AddSegment(segment, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
				CommonUtils.Swap(ref segment.m_Left, ref segment.m_Right);
				segment.m_Right.a.x += prefabCompositionData.m_Width;
				segment.m_Right.b.x += prefabCompositionData.m_Width;
				segment.m_Right.c.x = node.m_Position.x * 2f - segment.m_Right.c.x;
				AddSegment(segment, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
				segment.m_Left.a = new float3(node.m_Position.x + prefabCompositionData.m_Width * 0.5f, node.m_Position.y, node.m_Position.z);
				segment.m_Left.b = new float3(node.m_Position.x + prefabCompositionData.m_Width * 0.5f, node.m_Position.y, node.m_Position.z - prefabCompositionData.m_Width * 0.2761424f);
				segment.m_Left.c = new float3(node.m_Position.x + prefabCompositionData.m_Width * 0.2761424f, node.m_Position.y, node.m_Position.z - prefabCompositionData.m_Width * 0.5f);
				segment.m_Left.d = new float3(node.m_Position.x, node.m_Position.y, node.m_Position.z - prefabCompositionData.m_Width * 0.5f);
				segment.m_Right = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position);
				segment.m_Length = new float2(prefabCompositionData.m_Width * (MathF.PI / 2f), 0f);
				AddSegment(segment, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
				CommonUtils.Swap(ref segment.m_Left, ref segment.m_Right);
				segment.m_Right.a.x -= prefabCompositionData.m_Width;
				segment.m_Right.b.x -= prefabCompositionData.m_Width;
				segment.m_Right.c.x = node.m_Position.x * 2f - segment.m_Right.c.x;
				AddSegment(segment, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
			}
		}

		private void AddSegment(Segment segment, NetData net, NetGeometryData netGeometry, NetCompositionData compositionData, TerrainComposition terrainComposition, LaneFlags flags, bool isStart)
		{
			float num = compositionData.m_Width;
			if (math.any(terrainComposition.m_WidthOffset != 0f))
			{
				Segment segment2 = segment;
				float4 @float = 1f / math.max(y: new float4(math.distance(segment.m_Left.a.xz, segment.m_Right.a.xz), math.distance(segment.m_Left.b.xz, segment.m_Right.b.xz), math.distance(segment.m_Left.c.xz, segment.m_Right.c.xz), math.distance(segment.m_Left.d.xz, segment.m_Right.d.xz)), x: 0.001f);
				if (terrainComposition.m_WidthOffset.x != 0f && (flags & LaneFlags.MiddleLeft) == 0)
				{
					segment.m_Left = MathUtils.Lerp(t: new Bezier4x1
					{
						abcd = terrainComposition.m_WidthOffset.x * @float
					}, curve1: segment2.m_Left, curve2: segment2.m_Right);
					num -= terrainComposition.m_WidthOffset.x;
				}
				if (terrainComposition.m_WidthOffset.y != 0f && (flags & LaneFlags.MiddleRight) == 0)
				{
					segment.m_Right = MathUtils.Lerp(t: new Bezier4x1
					{
						abcd = terrainComposition.m_WidthOffset.y * @float
					}, curve1: segment2.m_Right, curve2: segment2.m_Left);
					num -= terrainComposition.m_WidthOffset.y;
				}
			}
			float3 float2 = math.select(new float3(compositionData.m_EdgeHeights.z, compositionData.m_SurfaceHeight.min, compositionData.m_EdgeHeights.w), new float3(compositionData.m_EdgeHeights.x, compositionData.m_SurfaceHeight.min, compositionData.m_EdgeHeights.y), isStart);
			float3 float3 = float2;
			float2 clipOffset = new float2(math.cmin(float2), math.cmax(float3));
			float terrainSmoothingWidth = NetUtils.GetTerrainSmoothingWidth(net);
			clipOffset += terrainComposition.m_ClipHeightOffset;
			float2 += terrainComposition.m_MinHeightOffset;
			float3 += terrainComposition.m_MaxHeightOffset;
			float3 float4 = 1000000f;
			float3 float5 = 1000000f;
			float3 float6 = 1000000f;
			if ((compositionData.m_State & CompositionState.HasSurface) == 0)
			{
				if ((compositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
				{
					float2 = 1000000f;
					float3 = compositionData.m_HeightRange.max + 1f + terrainComposition.m_MaxHeightOffset;
				}
				else
				{
					float2 = compositionData.m_HeightRange.min + terrainComposition.m_MinHeightOffset;
					float3 = -1000000f;
				}
			}
			else if ((compositionData.m_Flags.m_General & CompositionFlags.General.Elevated) != 0 || (netGeometry.m_MergeLayers & Layer.Waterway) != Layer.None)
			{
				if (((compositionData.m_Flags.m_Left | compositionData.m_Flags.m_Right) & (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) == 0)
				{
					float2 = compositionData.m_HeightRange.min;
				}
				float3 = -1000000f;
			}
			else if ((compositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
			{
				if ((compositionData.m_Flags.m_Left & CompositionFlags.Side.HighTransition) != 0)
				{
					float4.xy = math.min(float4.xy, float2.xy);
				}
				if ((compositionData.m_Flags.m_Right & CompositionFlags.Side.HighTransition) != 0)
				{
					float4.yz = math.min(float4.yz, float2.yz);
				}
				if (((compositionData.m_Flags.m_Left | compositionData.m_Flags.m_Right) & (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) == 0)
				{
					float2 = 1000000f;
					float3 = compositionData.m_HeightRange.max + 1f;
				}
				else
				{
					float5 = netGeometry.m_ElevationLimit * 3f;
					float6 = compositionData.m_HeightRange.max + 1f;
					float2 = math.max(float2, netGeometry.m_ElevationLimit * 3f);
					clipOffset.y = math.max(clipOffset.y, netGeometry.m_ElevationLimit * 3f);
				}
			}
			else
			{
				if ((compositionData.m_Flags.m_Left & CompositionFlags.Side.Lowered) != 0)
				{
					if ((compositionData.m_Flags.m_Left & CompositionFlags.Side.LowTransition) != 0)
					{
						float4.xy = math.min(float4.xy, float2.xy);
					}
					float2.xy = math.max(float2.xy, netGeometry.m_ElevationLimit * 3f);
					clipOffset.y = math.max(clipOffset.y, netGeometry.m_ElevationLimit * 3f);
				}
				else if ((compositionData.m_Flags.m_Left & CompositionFlags.Side.Raised) != 0)
				{
					float3.xy = -1000000f;
				}
				if ((compositionData.m_Flags.m_Right & CompositionFlags.Side.Lowered) != 0)
				{
					if ((compositionData.m_Flags.m_Right & CompositionFlags.Side.LowTransition) != 0)
					{
						float4.yz = math.min(float4.yz, float2.yz);
					}
					float2.yz = math.max(float2.yz, netGeometry.m_ElevationLimit * 3f);
					clipOffset.y = math.max(clipOffset.y, netGeometry.m_ElevationLimit * 3f);
				}
				else if ((compositionData.m_Flags.m_Right & CompositionFlags.Side.Raised) != 0)
				{
					float3.yz = -1000000f;
				}
			}
			float middleSize = math.saturate(1f - math.max(num * 0.2f, 3f) / math.max(1f, num));
			Bounds3 bounds = MathUtils.Bounds(segment.m_Left) | MathUtils.Bounds(segment.m_Right);
			bounds.min.xz -= terrainSmoothingWidth;
			bounds.max.xz += terrainSmoothingWidth;
			bounds.min.y += math.cmin(math.min(float2, float3));
			bounds.max.y += math.cmax(math.max(float2, float3));
			LaneSection value = new LaneSection
			{
				m_Bounds = bounds.xz,
				m_Left = new float4x3(segment.m_Left.a.x, segment.m_Left.a.y, segment.m_Left.a.z, segment.m_Left.b.x, segment.m_Left.b.y, segment.m_Left.b.z, segment.m_Left.c.x, segment.m_Left.c.y, segment.m_Left.c.z, segment.m_Left.d.x, segment.m_Left.d.y, segment.m_Left.d.z),
				m_Right = new float4x3(segment.m_Right.a.x, segment.m_Right.a.y, segment.m_Right.a.z, segment.m_Right.b.x, segment.m_Right.b.y, segment.m_Right.b.z, segment.m_Right.c.x, segment.m_Right.c.y, segment.m_Right.c.z, segment.m_Right.d.x, segment.m_Right.d.y, segment.m_Right.d.z),
				m_MinOffset = float2,
				m_MaxOffset = float3,
				m_ClipOffset = clipOffset,
				m_WidthOffset = terrainSmoothingWidth,
				m_MiddleSize = middleSize,
				m_Flags = flags
			};
			Result.AddNoResize(value);
			if (math.any(float4 != 1000000f) && (flags & LaneFlags.ShiftTerrain) != 0)
			{
				Bounds1 t = new Bounds1(0f, 1f);
				Bounds1 t2 = new Bounds1(0f, 1f);
				MathUtils.ClampLengthInverse(segment.m_Left.xz, ref t, 3f);
				MathUtils.ClampLengthInverse(segment.m_Right.xz, ref t2, 3f);
				Segment segment3 = segment;
				segment3.m_Left = MathUtils.Cut(segment.m_Left, t);
				segment3.m_Right = MathUtils.Cut(segment.m_Right, t2);
				bounds = MathUtils.Bounds(segment3.m_Left) | MathUtils.Bounds(segment3.m_Right);
				bounds.min.xz -= terrainSmoothingWidth;
				bounds.max.xz += terrainSmoothingWidth;
				bounds.min.y += math.cmin(math.min(float4, float3));
				bounds.max.y += math.cmax(math.max(float4, float3));
				value = new LaneSection
				{
					m_Bounds = bounds.xz,
					m_Left = new float4x3(segment3.m_Left.a.x, segment3.m_Left.a.y, segment3.m_Left.a.z, segment3.m_Left.b.x, segment3.m_Left.b.y, segment3.m_Left.b.z, segment3.m_Left.c.x, segment3.m_Left.c.y, segment3.m_Left.c.z, segment3.m_Left.d.x, segment3.m_Left.d.y, segment3.m_Left.d.z),
					m_Right = new float4x3(segment3.m_Right.a.x, segment3.m_Right.a.y, segment3.m_Right.a.z, segment3.m_Right.b.x, segment3.m_Right.b.y, segment3.m_Right.b.z, segment3.m_Right.c.x, segment3.m_Right.c.y, segment3.m_Right.c.z, segment3.m_Right.d.x, segment3.m_Right.d.y, segment3.m_Right.d.z),
					m_MinOffset = float4,
					m_MaxOffset = float3,
					m_ClipOffset = clipOffset,
					m_WidthOffset = terrainSmoothingWidth,
					m_MiddleSize = middleSize,
					m_Flags = (flags & ~LaneFlags.ClipTerrain)
				};
				Result.AddNoResize(value);
			}
			if ((math.any(float5 != 1000000f) || math.any(float6 != 1000000f)) && (flags & LaneFlags.ShiftTerrain) != 0)
			{
				float3 value2 = MathUtils.StartTangent(segment.m_Left);
				float3 value3 = MathUtils.StartTangent(segment.m_Right);
				value2 = MathUtils.Normalize(value2, value2.xz);
				value3 = MathUtils.Normalize(value3, value3.xz);
				Segment segment4 = segment;
				segment4.m_Left = NetUtils.StraightCurve(segment.m_Left.a + value2 * 2f, segment.m_Left.a - value2 * 2f);
				segment4.m_Right = NetUtils.StraightCurve(segment.m_Right.a + value3 * 2f, segment.m_Right.a - value3 * 2f);
				bounds = MathUtils.Bounds(segment4.m_Left) | MathUtils.Bounds(segment4.m_Right);
				bounds.min.xz -= terrainSmoothingWidth;
				bounds.max.xz += terrainSmoothingWidth;
				bounds.min.y += math.cmin(math.min(float5, float3));
				bounds.max.y += math.cmax(math.max(float5, float3));
				value = new LaneSection
				{
					m_Bounds = bounds.xz,
					m_Left = new float4x3(segment4.m_Left.a.x, segment4.m_Left.a.y, segment4.m_Left.a.z, segment4.m_Left.b.x, segment4.m_Left.b.y, segment4.m_Left.b.z, segment4.m_Left.c.x, segment4.m_Left.c.y, segment4.m_Left.c.z, segment4.m_Left.d.x, segment4.m_Left.d.y, segment4.m_Left.d.z),
					m_Right = new float4x3(segment4.m_Right.a.x, segment4.m_Right.a.y, segment4.m_Right.a.z, segment4.m_Right.b.x, segment4.m_Right.b.y, segment4.m_Right.b.z, segment4.m_Right.c.x, segment4.m_Right.c.y, segment4.m_Right.c.z, segment4.m_Right.d.x, segment4.m_Right.d.y, segment4.m_Right.d.z),
					m_MinOffset = float5,
					m_MaxOffset = float6,
					m_ClipOffset = clipOffset,
					m_WidthOffset = terrainSmoothingWidth,
					m_MiddleSize = middleSize,
					m_Flags = (flags & ~LaneFlags.ClipTerrain)
				};
				Result.AddNoResize(value);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DequeBuildingDrawsJob : IJob
	{
		[ReadOnly]
		public NativeQueue<BuildingLotDraw> m_Queue;

		public NativeList<BuildingLotDraw> m_List;

		public void Execute()
		{
			NativeArray<BuildingLotDraw> other = m_Queue.ToArray(Allocator.Temp);
			m_List.CopyFrom(in other);
			other.Dispose();
		}
	}

	[BurstCompile]
	private struct CullBuildingsCascadeJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeList<BuildingUtils.LotInfo> m_LotsToCull;

		[ReadOnly]
		public float4 m_Area;

		public NativeQueue<BuildingLotDraw>.ParallelWriter Result;

		public void Execute(int index)
		{
			if (index < m_LotsToCull.Length)
			{
				BuildingUtils.LotInfo lotInfo = m_LotsToCull[index];
				if (!(lotInfo.m_Position.x + lotInfo.m_Radius < m_Area.x) && !(lotInfo.m_Position.x - lotInfo.m_Radius > m_Area.z) && !(lotInfo.m_Position.z + lotInfo.m_Radius < m_Area.y) && !(lotInfo.m_Position.z - lotInfo.m_Radius > m_Area.w))
				{
					float2 @float = 0.5f / math.max(0.01f, lotInfo.m_Extents);
					BuildingLotDraw value = new BuildingLotDraw
					{
						m_HeightsX = math.transpose(new float4x2(new float4(lotInfo.m_RightHeights, lotInfo.m_BackHeights.x), new float4(lotInfo.m_FrontHeights.x, lotInfo.m_LeftHeights.zyx))),
						m_HeightsZ = math.transpose(new float4x2(new float4(lotInfo.m_RightHeights.x, lotInfo.m_FrontHeights.zyx), new float4(lotInfo.m_BackHeights, lotInfo.m_LeftHeights.x))),
						m_FlatX0 = lotInfo.m_FlatX0 * @float.x + 0.5f,
						m_FlatZ0 = lotInfo.m_FlatZ0 * @float.y + 0.5f,
						m_FlatX1 = lotInfo.m_FlatX1 * @float.x + 0.5f,
						m_FlatZ1 = lotInfo.m_FlatZ1 * @float.y + 0.5f,
						m_Position = lotInfo.m_Position,
						m_AxisX = math.mul(lotInfo.m_Rotation, new float3(1f, 0f, 0f)),
						m_AxisZ = math.mul(lotInfo.m_Rotation, new float3(0f, 0f, 1f)),
						m_Size = lotInfo.m_Extents,
						m_MinLimit = lotInfo.m_MinLimit * @float.xyxy + 0.5f,
						m_MaxLimit = lotInfo.m_MaxLimit * @float.xyxy + 0.5f,
						m_Circular = lotInfo.m_Circular,
						m_SmoothingWidth = ObjectUtils.GetTerrainSmoothingWidth(lotInfo.m_Extents * 2f)
					};
					Result.Enqueue(value);
				}
			}
		}
	}

	[BurstCompile]
	private struct CullTrianglesJob : IJob
	{
		[ReadOnly]
		public NativeList<AreaTriangle> m_Triangles;

		[ReadOnly]
		public float4 m_Area;

		public NativeList<AreaTriangle> Result;

		public void Execute()
		{
			for (int i = 0; i < m_Triangles.Length; i++)
			{
				AreaTriangle value = m_Triangles[i];
				float2 @float = math.min(value.m_PositionA.xz, math.min(value.m_PositionB.xz, value.m_PositionC.xz));
				float2 float2 = math.max(value.m_PositionA.xz, math.max(value.m_PositionB.xz, value.m_PositionC.xz));
				if (!(float2.x < m_Area.x) && !(@float.x > m_Area.z) && !(float2.y < m_Area.y) && !(@float.y > m_Area.w))
				{
					Result.Add(in value);
				}
			}
		}
	}

	[BurstCompile]
	private struct CullEdgesJob : IJob
	{
		[ReadOnly]
		public NativeList<AreaEdge> m_Edges;

		[ReadOnly]
		public float4 m_Area;

		public NativeList<AreaEdge> Result;

		public void Execute()
		{
			for (int i = 0; i < m_Edges.Length; i++)
			{
				AreaEdge value = m_Edges[i];
				float2 @float = math.min(value.m_PositionA, value.m_PositionB) - value.m_SideOffset;
				float2 float2 = math.max(value.m_PositionA, value.m_PositionB) + value.m_SideOffset;
				if (!(float2.x < m_Area.x) && !(@float.x > m_Area.z) && !(float2.y < m_Area.y) && !(@float.y > m_Area.w))
				{
					Result.Add(in value);
				}
			}
		}
	}

	[BurstCompile]
	private struct GenerateClipDataJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeList<LaneSection> m_RoadsToCull;

		public NativeList<ClipMapDraw>.ParallelWriter Result;

		public void Execute(int index)
		{
			LaneSection laneSection = m_RoadsToCull[index];
			if ((laneSection.m_Flags & LaneFlags.ClipTerrain) != 0)
			{
				laneSection.m_ClipOffset.x -= 0.3f;
				laneSection.m_ClipOffset.y += 0.3f;
				laneSection.m_Left.c1 += laneSection.m_ClipOffset.x;
				laneSection.m_Right.c1 += laneSection.m_ClipOffset.x;
				ClipMapDraw value = new ClipMapDraw
				{
					m_Left = laneSection.m_Left,
					m_Right = laneSection.m_Right,
					m_Height = laneSection.m_ClipOffset.y - laneSection.m_ClipOffset.x,
					m_OffsetFactor = math.select(1f, -1f, (laneSection.m_Flags & LaneFlags.InverseClipOffset) != 0)
				};
				Result.AddNoResize(value);
			}
		}
	}

	[BurstCompile]
	private struct CullAreasJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Clip> m_ClipType;

		[ReadOnly]
		public ComponentTypeHandle<Area> m_AreaType;

		[ReadOnly]
		public ComponentTypeHandle<Geometry> m_GeometryType;

		[ReadOnly]
		public ComponentTypeHandle<Storage> m_StorageType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		[ReadOnly]
		public ComponentLookup<TerrainAreaData> m_PrefabTerrainAreaData;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> m_PrefabStorageAreaData;

		[ReadOnly]
		public float4 m_Area;

		public NativeQueue<AreaTriangle>.ParallelWriter m_Triangles;

		public NativeQueue<AreaEdge>.ParallelWriter m_Edges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_ClipType))
			{
				return;
			}
			NativeArray<Area> nativeArray = chunk.GetNativeArray(ref m_AreaType);
			NativeArray<Geometry> nativeArray2 = chunk.GetNativeArray(ref m_GeometryType);
			NativeArray<Storage> nativeArray3 = chunk.GetNativeArray(ref m_StorageType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Game.Areas.Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			BufferAccessor<Triangle> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TriangleType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Area area = nativeArray[i];
				Geometry geometry = nativeArray2[i];
				PrefabRef prefabRef = nativeArray4[i];
				DynamicBuffer<Game.Areas.Node> nodes = bufferAccessor[i];
				DynamicBuffer<Triangle> dynamicBuffer = bufferAccessor2[i];
				if (geometry.m_Bounds.max.x < m_Area.x || geometry.m_Bounds.min.x > m_Area.z || geometry.m_Bounds.max.z < m_Area.y || geometry.m_Bounds.min.z > m_Area.w || dynamicBuffer.Length == 0 || !m_PrefabTerrainAreaData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					continue;
				}
				float2 noiseSize = new float2(componentData.m_NoiseFactor, componentData.m_NoiseScale);
				float2 heightDelta = new float2(componentData.m_HeightOffset, componentData.m_AbsoluteHeight);
				float num = math.abs(componentData.m_SlopeWidth);
				float expandAmount = math.max(0f, componentData.m_SlopeWidth * -1.5f);
				bool flag = (area.m_Flags & AreaFlags.CounterClockwise) != 0;
				if (nativeArray3.Length != 0 && m_PrefabStorageAreaData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					Storage storage = nativeArray3[i];
					int y = AreaUtils.CalculateStorageCapacity(geometry, componentData2);
					float num2 = (float)(int)((long)storage.m_Amount * 100L / math.max(1, y)) * 0.015f;
					float num3 = math.min(1f, num2);
					noiseSize.x *= math.clamp(2f - num2, 0.5f, 1f);
					heightDelta.x *= num3;
					num *= math.sqrt(num3);
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Triangle triangle = dynamicBuffer[j];
					m_Triangles.Enqueue(new AreaTriangle
					{
						m_PositionA = AreaUtils.GetExpandedNode(nodes, triangle.m_Indices.x, expandAmount, isComplete: true, flag),
						m_PositionB = AreaUtils.GetExpandedNode(nodes, triangle.m_Indices.y, expandAmount, isComplete: true, flag),
						m_PositionC = AreaUtils.GetExpandedNode(nodes, triangle.m_Indices.z, expandAmount, isComplete: true, flag),
						m_NoiseSize = noiseSize,
						m_HeightDelta = heightDelta
					});
				}
				if (flag)
				{
					float2 xz = AreaUtils.GetExpandedNode(nodes, 0, expandAmount, isComplete: true, flag).xz;
					float2 @float = AreaUtils.GetExpandedNode(nodes, 1, expandAmount, isComplete: true, flag).xz;
					float2 xz2 = AreaUtils.GetExpandedNode(nodes, 2, expandAmount, isComplete: true, flag).xz;
					float2 float2 = math.normalizesafe(@float - xz);
					float2 float3 = math.normalizesafe(xz2 - @float);
					float num4 = MathUtils.RotationAngleRight(-float2, float3);
					for (int k = 0; k < nodes.Length; k++)
					{
						int num5 = k + 3;
						num5 -= math.select(0, nodes.Length, num5 >= nodes.Length);
						xz = @float;
						@float = xz2;
						xz2 = AreaUtils.GetExpandedNode(nodes, num5, expandAmount, isComplete: true, flag).xz;
						float2 = float3;
						float3 = math.normalizesafe(xz2 - @float);
						float y2 = num4;
						num4 = MathUtils.RotationAngleRight(-float2, float3);
						m_Edges.Enqueue(new AreaEdge
						{
							m_PositionA = @float,
							m_PositionB = xz,
							m_Angles = new float2(num4, y2),
							m_SideOffset = num
						});
					}
				}
				else
				{
					float2 xz3 = AreaUtils.GetExpandedNode(nodes, 0, expandAmount, isComplete: true, flag).xz;
					float2 float4 = AreaUtils.GetExpandedNode(nodes, 1, expandAmount, isComplete: true, flag).xz;
					float2 xz4 = AreaUtils.GetExpandedNode(nodes, 2, expandAmount, isComplete: true, flag).xz;
					float2 float5 = math.normalizesafe(float4 - xz3);
					float2 float6 = math.normalizesafe(xz4 - float4);
					float num6 = MathUtils.RotationAngleLeft(-float5, float6);
					for (int l = 0; l < nodes.Length; l++)
					{
						int num7 = l + 3;
						num7 -= math.select(0, nodes.Length, num7 >= nodes.Length);
						xz3 = float4;
						float4 = xz4;
						xz4 = AreaUtils.GetExpandedNode(nodes, num7, expandAmount, isComplete: true, flag).xz;
						float5 = float6;
						float6 = math.normalizesafe(xz4 - float4);
						float x = num6;
						num6 = MathUtils.RotationAngleLeft(-float5, float6);
						m_Edges.Enqueue(new AreaEdge
						{
							m_PositionA = xz3,
							m_PositionB = float4,
							m_Angles = new float2(x, num6),
							m_SideOffset = num
						});
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DequeTrianglesJob : IJob
	{
		[ReadOnly]
		public NativeQueue<AreaTriangle> m_Queue;

		public NativeList<AreaTriangle> m_List;

		public void Execute()
		{
			NativeArray<AreaTriangle> other = m_Queue.ToArray(Allocator.Temp);
			m_List.CopyFrom(in other);
			other.Dispose();
		}
	}

	[BurstCompile]
	private struct DequeEdgesJob : IJob
	{
		[ReadOnly]
		public NativeQueue<AreaEdge> m_Queue;

		public NativeList<AreaEdge> m_List;

		public void Execute()
		{
			NativeArray<AreaEdge> other = m_Queue.ToArray(Allocator.Temp);
			m_List.CopyFrom(in other);
			other.Dispose();
		}
	}

	[BurstCompile]
	private struct GenerateAreaClipMeshJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Clip> m_ClipType;

		[ReadOnly]
		public ComponentTypeHandle<Area> m_AreaType;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		public Mesh.MeshDataArray m_MeshData;

		public void Execute()
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				if (archetypeChunk.Has(ref m_ClipType))
				{
					BufferAccessor<Game.Areas.Node> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_NodeType);
					BufferAccessor<Triangle> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_TriangleType);
					for (int j = 0; j < bufferAccessor.Length; j++)
					{
						DynamicBuffer<Game.Areas.Node> dynamicBuffer = bufferAccessor[j];
						DynamicBuffer<Triangle> dynamicBuffer2 = bufferAccessor2[j];
						num += dynamicBuffer.Length * 2;
						num2 += dynamicBuffer2.Length * 6 + dynamicBuffer.Length * 6;
					}
				}
			}
			Mesh.MeshData meshData = m_MeshData[0];
			NativeArray<VertexAttributeDescriptor> attributes = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory) { [0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4) };
			meshData.SetVertexBufferParams(num, attributes);
			meshData.SetIndexBufferParams(num2, IndexFormat.UInt32);
			attributes.Dispose();
			meshData.subMeshCount = 1;
			meshData.SetSubMesh(0, new SubMeshDescriptor
			{
				vertexCount = num,
				indexCount = num2,
				topology = MeshTopology.Triangles
			}, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
			NativeArray<float4> vertexData = meshData.GetVertexData<float4>();
			NativeArray<uint> indexData = meshData.GetIndexData<uint>();
			SubMeshDescriptor subMesh = meshData.GetSubMesh(0);
			Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
			int num3 = 0;
			int num4 = 0;
			for (int k = 0; k < m_Chunks.Length; k++)
			{
				ArchetypeChunk archetypeChunk2 = m_Chunks[k];
				if (!archetypeChunk2.Has(ref m_ClipType))
				{
					continue;
				}
				NativeArray<Area> nativeArray = archetypeChunk2.GetNativeArray(ref m_AreaType);
				BufferAccessor<Game.Areas.Node> bufferAccessor3 = archetypeChunk2.GetBufferAccessor(ref m_NodeType);
				BufferAccessor<Triangle> bufferAccessor4 = archetypeChunk2.GetBufferAccessor(ref m_TriangleType);
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Area area = nativeArray[l];
					DynamicBuffer<Game.Areas.Node> dynamicBuffer3 = bufferAccessor3[l];
					DynamicBuffer<Triangle> dynamicBuffer4 = bufferAccessor4[l];
					int4 @int = num3 + new int4(0, 1, dynamicBuffer3.Length, dynamicBuffer3.Length + 1);
					float num5 = 0f;
					float num6 = 0f;
					for (int m = 0; m < dynamicBuffer4.Length; m++)
					{
						Triangle triangle = dynamicBuffer4[m];
						int3 indices = triangle.m_Indices;
						num5 = math.min(num5, triangle.m_HeightRange.min);
						num6 = math.max(num6, triangle.m_HeightRange.max);
						int3 int2 = indices + @int.x;
						indexData[num4++] = (uint)int2.z;
						indexData[num4++] = (uint)int2.y;
						indexData[num4++] = (uint)int2.x;
						int3 int3 = indices + @int.z;
						indexData[num4++] = (uint)int3.x;
						indexData[num4++] = (uint)int3.y;
						indexData[num4++] = (uint)int3.z;
					}
					if ((area.m_Flags & AreaFlags.CounterClockwise) != 0)
					{
						for (int n = 0; n < dynamicBuffer3.Length; n++)
						{
							int4 int4 = n + @int;
							int4.yw -= math.select(0, dynamicBuffer3.Length, n == dynamicBuffer3.Length - 1);
							indexData[num4++] = (uint)int4.x;
							indexData[num4++] = (uint)int4.y;
							indexData[num4++] = (uint)int4.w;
							indexData[num4++] = (uint)int4.w;
							indexData[num4++] = (uint)int4.z;
							indexData[num4++] = (uint)int4.x;
						}
					}
					else
					{
						for (int num7 = 0; num7 < dynamicBuffer3.Length; num7++)
						{
							int4 int5 = num7 + @int;
							int5.yw -= math.select(0, dynamicBuffer3.Length, num7 == dynamicBuffer3.Length - 1);
							indexData[num4++] = (uint)int5.x;
							indexData[num4++] = (uint)int5.z;
							indexData[num4++] = (uint)int5.w;
							indexData[num4++] = (uint)int5.w;
							indexData[num4++] = (uint)int5.y;
							indexData[num4++] = (uint)int5.x;
						}
					}
					num5 -= 0.3f;
					num6 += 0.3f;
					for (int num8 = 0; num8 < dynamicBuffer3.Length; num8++)
					{
						float3 position = dynamicBuffer3[num8].m_Position;
						position.y += num5;
						bounds |= position;
						vertexData[num3++] = new float4(position, 0f);
					}
					for (int num9 = 0; num9 < dynamicBuffer3.Length; num9++)
					{
						float3 position2 = dynamicBuffer3[num9].m_Position;
						position2.y += num6;
						bounds |= position2;
						vertexData[num3++] = new float4(position2, 1f);
					}
				}
			}
			subMesh.bounds = RenderingUtils.ToBounds(bounds);
			meshData.SetSubMesh(0, subMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
		}
	}

	[BurstCompile]
	private struct CullRoadsCacscadeJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeList<LaneSection> m_RoadsToCull;

		[ReadOnly]
		public float4 m_Area;

		[ReadOnly]
		public float m_Scale;

		[ReadOnly]
		public bool m_addRaised;

		public NativeList<LaneDraw>.ParallelWriter Result;

		public NativeList<LaneDraw>.ParallelWriter ResultRaised;

		public void Execute(int index)
		{
			LaneSection laneSection = m_RoadsToCull[index];
			if ((laneSection.m_Flags & LaneFlags.ShiftTerrain) != 0 && !math.any(laneSection.m_Bounds.max < m_Area.xy) && !math.any(laneSection.m_Bounds.min > m_Area.zw))
			{
				float4 minOffset;
				float4 maxOffset;
				float2 widthOffset;
				if ((laneSection.m_Flags & (LaneFlags.MiddleLeft | LaneFlags.MiddleRight)) == (LaneFlags.MiddleLeft | LaneFlags.MiddleRight))
				{
					minOffset = new float4(laneSection.m_MinOffset.yyy, 1f);
					maxOffset = new float4(laneSection.m_MaxOffset.yyy, 1f);
					widthOffset = 0f;
				}
				else if ((laneSection.m_Flags & LaneFlags.MiddleLeft) != 0)
				{
					minOffset = new float4(laneSection.m_MinOffset.yyz, (laneSection.m_MiddleSize - 0.5f) * 2f);
					maxOffset = new float4(laneSection.m_MaxOffset.yyz, (laneSection.m_MiddleSize - 0.5f) * 2f);
					widthOffset = new float2(0f, laneSection.m_WidthOffset);
				}
				else if ((laneSection.m_Flags & LaneFlags.MiddleRight) != 0)
				{
					minOffset = new float4(laneSection.m_MinOffset.xyy, (laneSection.m_MiddleSize - 0.5f) * 2f);
					maxOffset = new float4(laneSection.m_MaxOffset.xyy, (laneSection.m_MiddleSize - 0.5f) * 2f);
					widthOffset = new float2(laneSection.m_WidthOffset, 0f);
				}
				else
				{
					minOffset = new float4(laneSection.m_MinOffset, laneSection.m_MiddleSize);
					maxOffset = new float4(laneSection.m_MaxOffset, laneSection.m_MiddleSize);
					widthOffset = laneSection.m_WidthOffset;
				}
				LaneDraw value = new LaneDraw
				{
					m_Left = laneSection.m_Left,
					m_Right = laneSection.m_Right,
					m_MinOffset = minOffset,
					m_MaxOffset = maxOffset,
					m_WidthOffset = widthOffset
				};
				Result.AddNoResize(value);
				if (m_addRaised && (laneSection.m_Flags & LaneFlags.Raised) != 0)
				{
					ResultRaised.AddNoResize(value);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Updated> __Game_Common_Updated_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Areas.Terrain> __Game_Areas_Terrain_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Clip> __Game_Areas_Clip_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Geometry> __Game_Areas_Geometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Lot> __Game_Buildings_Lot_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stack> __Game_Objects_Stack_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AssetStampData> __Game_Prefabs_AssetStampData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<AdditionalBuildingTerraformElement> __Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TerrainComposition> __Game_Prefabs_TerrainComposition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Area> __Game_Areas_Area_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Storage> __Game_Areas_Storage_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<TerrainAreaData> __Game_Prefabs_TerrainAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> __Game_Prefabs_StorageAreaData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Updated>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Net_NodeGeometry_RO_ComponentLookup = state.GetComponentLookup<NodeGeometry>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Areas_Terrain_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Areas.Terrain>(isReadOnly: true);
			__Game_Areas_Clip_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Clip>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Geometry>(isReadOnly: true);
			__Game_Buildings_Lot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Lot>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Stack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stack>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_AssetStampData_RO_ComponentLookup = state.GetComponentLookup<AssetStampData>(isReadOnly: true);
			__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup = state.GetComponentLookup<BuildingTerraformData>(isReadOnly: true);
			__Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup = state.GetBufferLookup<AdditionalBuildingTerraformElement>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_TerrainComposition_RO_ComponentLookup = state.GetComponentLookup<TerrainComposition>(isReadOnly: true);
			__Game_Areas_Area_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Area>(isReadOnly: true);
			__Game_Areas_Storage_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Storage>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
			__Game_Prefabs_TerrainAreaData_RO_ComponentLookup = state.GetComponentLookup<TerrainAreaData>(isReadOnly: true);
			__Game_Prefabs_StorageAreaData_RO_ComponentLookup = state.GetComponentLookup<StorageAreaData>(isReadOnly: true);
		}
	}

	private const float kShiftTerrainAmount = 2000f;

	private const float kSoftenTerrainAmount = 1000f;

	private const float kSlopeAndLevelTerrainAmount = 4000f;

	public static readonly int kDefaultHeightmapWidth = 4096;

	public static readonly int kDefaultHeightmapHeight = kDefaultHeightmapWidth;

	public static readonly int kDownScaledHeightmapScale = 4;

	private static readonly float2 kDefaultMapSize = new float2(14336f, 14336f);

	private static readonly float2 kDefaultMapOffset = kDefaultMapSize * -0.5f;

	private static readonly float2 kDefaultWorldSize = kDefaultMapSize * 4f;

	private static readonly float2 kDefaultWorldOffset = kDefaultWorldSize * -0.5f;

	private static readonly float2 kDefaultHeightScaleOffset = new float2(4096f, 0f);

	private AsyncGPUReadbackHelper m_AsyncGPUReadback;

	private NativeArray<ushort> m_CPUHeights;

	private AsyncGPUReadbackHelper m_AsyncDownscaledHeightsGPUReadback;

	private NativeArray<ushort> m_CPUHeightsDownscaled;

	private JobHandle m_CPUHeightReaders;

	private JobHandle m_CPUDownSampleHeightReaders;

	private RenderTexture m_Heightmap;

	private RenderTexture m_HeightmapCascade;

	private RenderTexture m_HeightmapObjectsLayer;

	private RenderTexture m_HeightmapDepth;

	private RenderTexture m_WorldMapEditable;

	private RenderTexture m_DownscaledHeightmap;

	private Vector4 m_MapOffsetScale;

	private bool m_HeightMapChanged;

	private int4 m_LastPreviewWrite;

	private int4 m_LastWorldPreviewWrite;

	private int4 m_LastWrite;

	private int4 m_LastWorldWrite;

	private int4 m_LastRequest;

	private int m_FailCount;

	private Vector4 m_WorldOffsetScale;

	private bool m_NewMap;

	private bool m_NewMapThisFrame;

	private bool m_Loaded;

	private bool m_HeightsReadyAfterLoading;

	private bool m_UpdateOutOfDate;

	private ComputeShader m_AdjustTerrainCS;

	private int m_ShiftTerrainKernal;

	private int m_BlurHorzKernal;

	private int m_BlurVertKernal;

	private int m_SmoothTerrainKernal;

	private int m_LevelTerrainKernal;

	private int m_SlopeTerrainKernal;

	private int m_DownsampleTerrainKernel;

	private CommandBuffer m_CommandBuffer;

	private CommandBuffer m_CascadeCB;

	private Material m_TerrainBlit;

	private Material m_ClipMaterial;

	private EntityQuery m_BrushQuery;

	public bool doCapture;

	private NativeList<BuildingUtils.LotInfo> m_BuildingCullList;

	private NativeList<LaneSection> m_LaneCullList;

	private NativeList<AreaTriangle> m_TriangleCullList;

	private NativeList<AreaEdge> m_EdgeCullList;

	private JobHandle m_BuildingCull;

	private JobHandle m_LaneCull;

	private JobHandle m_AreaCull;

	private JobHandle m_ClipMapCull;

	private JobHandle m_CullFinished;

	private NativeParallelHashMap<Entity, Entity> m_BuildingUpgrade;

	private JobHandle m_BuildingUpgradeDependencies;

	public const int kCascadeMax = 4;

	private float4 m_LastCullArea;

	private float4[] m_CascadeRanges;

	private Vector4[] m_ShaderCascadeRanges;

	private float4 m_UpdateArea;

	private float4 m_TerrainChangeArea;

	private bool m_CascadeReset;

	private bool m_RoadUpdate;

	private bool m_AreaUpdate;

	private bool m_TerrainChange;

	private EntityQuery m_BuildingsChanged;

	private EntityQuery m_BuildingGroup;

	private EntityQuery m_RoadsChanged;

	private EntityQuery m_RoadsGroup;

	private EntityQuery m_EditorLotQuery;

	private EntityQuery m_AreasChanged;

	private EntityQuery m_AreasQuery;

	private List<CascadeCullInfo> m_CascadeCulling;

	private ManagedStructuredBuffers<BuildingLotDraw> m_BuildingInstanceData;

	private ManagedStructuredBuffers<LaneDraw> m_LaneInstanceData;

	private ManagedStructuredBuffers<LaneDraw> m_LaneRaisedInstanceData;

	private ManagedStructuredBuffers<AreaTriangle> m_TriangleInstanceData;

	private ManagedStructuredBuffers<AreaEdge> m_EdgeInstanceData;

	private Material m_MasterBuildingLotMaterial;

	private Material m_MasterLaneMaterial;

	private Material m_MasterAreaMaterial;

	private Mesh m_LaneMesh;

	private ToolSystem m_ToolSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private GroundHeightSystem m_GroundHeightSystem;

	private RenderingSystem m_RenderingSystem;

	private WaterSystem m_WaterSystem;

	private NativeList<ClipMapDraw> m_ClipMapList;

	private ManagedStructuredBuffers<ClipMapDraw> m_ClipMapBuffer;

	private ComputeBuffer m_CurrentClipMap;

	private Mesh m_ClipMesh;

	private Mesh m_AreaClipMesh;

	private Mesh.MeshDataArray m_AreaClipMeshData;

	private bool m_HasAreaClipMeshData;

	private JobHandle m_AreaClipMeshDataDeps;

	private TerrainMinMaxMap m_TerrainMinMax;

	private TypeHandle __TypeHandle;

	public Vector4 VTScaleOffset => new Vector4(m_WorldOffsetScale.z, m_WorldOffsetScale.w, m_WorldOffsetScale.x, m_WorldOffsetScale.y);

	public bool NewMap => m_NewMapThisFrame;

	public Texture heightmap => m_Heightmap;

	public Texture downscaledHeightmap => m_DownscaledHeightmap;

	public Vector4 mapOffsetScale => m_MapOffsetScale;

	public float2 heightScaleOffset { get; set; }

	public TextureAsset worldMapAsset { get; set; }

	public Texture worldHeightmap { get; set; }

	public bool TerrainShadowUseStencilClip { get; set; } = true;

	public float2 playableArea { get; private set; }

	public float2 playableOffset { get; private set; }

	public float2 worldSize { get; private set; }

	public float2 worldOffset { get; private set; }

	public float2 worldHeightMinMax { get; private set; }

	public float3 positionOffset => new float3(playableOffset.x, heightScaleOffset.y, playableOffset.y);

	public bool heightMapRenderRequired { get; private set; }

	public bool[] heightMapSliceUpdated { get; private set; }

	public float4[] heightMapViewport { get; private set; }

	public float4[] heightMapViewportUpdated { get; private set; }

	public float4[] heightMapSliceArea => m_CascadeRanges;

	public float4[] heightMapCullArea { get; private set; }

	public bool freezeCascadeUpdates { get; set; }

	public bool[] heightMapSliceUpdatedLast { get; private set; }

	public float4 lastCullArea => m_LastCullArea;

	public static bool HasBackdrop => baseLod != 0;

	public static int baseLod { get; private set; }

	private ComputeBuffer clipMapBuffer
	{
		get
		{
			if (m_CurrentClipMap == null)
			{
				m_ClipMapCull.Complete();
				if (m_ClipMapList.Length > 0)
				{
					NativeArray<ClipMapDraw> data = m_ClipMapList.AsArray();
					m_ClipMapBuffer.StartFrame();
					m_CurrentClipMap = m_ClipMapBuffer.Request(data.Length);
					m_CurrentClipMap.SetData(data);
					m_ClipMapBuffer.EndFrame();
				}
			}
			return m_CurrentClipMap;
		}
	}

	private int clipMapInstances
	{
		get
		{
			m_ClipMapCull.Complete();
			return m_ClipMapList.Length;
		}
	}

	public Mesh areaClipMesh
	{
		get
		{
			if (m_AreaClipMesh == null)
			{
				m_AreaClipMesh = new Mesh();
			}
			if (m_HasAreaClipMeshData)
			{
				m_HasAreaClipMeshData = false;
				m_AreaClipMeshDataDeps.Complete();
				Mesh.ApplyAndDisposeWritableMeshData(m_AreaClipMeshData, m_AreaClipMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
			}
			return m_AreaClipMesh;
		}
		private set
		{
			m_AreaClipMesh = value;
		}
	}

	private float GetTerrainAdjustmentSpeed(TerraformingType type)
	{
		return type switch
		{
			TerraformingType.Soften => 1000f, 
			TerraformingType.Shift => 2000f, 
			_ => 4000f, 
		};
	}

	public Bounds GetTerrainBounds()
	{
		float3 @float = new float3(0f, (0f - heightScaleOffset.y) * 0.5f, 0f);
		return new Bounds(size: new float3(14336f, heightScaleOffset.x, 14336f), center: @float);
	}

	public TerrainHeightData GetHeightData(bool waitForPending = false)
	{
		if (waitForPending && m_HeightMapChanged)
		{
			m_AsyncGPUReadback.WaitForCompletion();
			m_CPUHeightReaders.Complete();
			m_CPUHeightReaders = default(JobHandle);
			if (HasBackdrop)
			{
				m_AsyncDownscaledHeightsGPUReadback.WaitForCompletion();
				m_CPUDownSampleHeightReaders.Complete();
				m_CPUDownSampleHeightReaders = default(JobHandle);
			}
			UpdateGPUReadback();
		}
		int3 resolution = ((m_CPUHeights.IsCreated && (!HasBackdrop || m_CPUHeightsDownscaled.IsCreated) && !(m_HeightmapCascade == null) && m_CPUHeights.Length == m_HeightmapCascade.width * m_HeightmapCascade.height) ? new int3(m_HeightmapCascade.width, 65536, m_HeightmapCascade.height) : new int3(2, 2, 2));
		float3 @float = new float3(14336f, math.max(1f, heightScaleOffset.x), 14336f);
		float3 scale = new float3(resolution.x, resolution.y - 1, resolution.z) / @float;
		float3 offset = -positionOffset;
		offset.xz -= 0.5f / scale.xz;
		return new TerrainHeightData(m_CPUHeights, m_CPUHeightsDownscaled, resolution, scale, offset, HasBackdrop);
	}

	public void AddCPUHeightReader(JobHandle handle)
	{
		m_CPUHeightReaders = JobHandle.CombineDependencies(m_CPUHeightReaders, handle);
	}

	public void AddCPUDownsampleHeightReader(JobHandle handle)
	{
		m_CPUDownSampleHeightReaders = JobHandle.CombineDependencies(m_CPUDownSampleHeightReaders, handle);
	}

	public NativeList<LaneSection> GetRoads()
	{
		m_LaneCull.Complete();
		return m_LaneCullList;
	}

	public bool GetTerrainBrushUpdate(out float4 viewport)
	{
		viewport = m_TerrainChangeArea;
		if (m_TerrainChange)
		{
			m_TerrainChange = false;
			viewport = new float4(m_TerrainChangeArea.x - m_CascadeRanges[baseLod].x, m_TerrainChangeArea.y - m_CascadeRanges[baseLod].y, m_TerrainChangeArea.z - m_CascadeRanges[baseLod].x, m_TerrainChangeArea.w - m_CascadeRanges[baseLod].y);
			viewport /= new float4(m_CascadeRanges[baseLod].z - m_CascadeRanges[baseLod].x, m_CascadeRanges[baseLod].w - m_CascadeRanges[baseLod].y, m_CascadeRanges[baseLod].z - m_CascadeRanges[baseLod].x, m_CascadeRanges[baseLod].w - m_CascadeRanges[baseLod].y);
			viewport.zw -= viewport.xy;
			viewport = ClipViewport(viewport);
			m_TerrainChangeArea = viewport;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LastCullArea = float4.zero;
		freezeCascadeUpdates = false;
		m_CPUHeights = new NativeArray<ushort>(4, Allocator.Persistent);
		m_CPUHeightsDownscaled = new NativeArray<ushort>(4, Allocator.Persistent);
		m_AdjustTerrainCS = Resources.Load<ComputeShader>("AdjustTerrain");
		m_DownsampleTerrainKernel = m_AdjustTerrainCS.FindKernel("DownsampleTerrain");
		m_ShiftTerrainKernal = m_AdjustTerrainCS.FindKernel("ShiftTerrain");
		m_BlurHorzKernal = m_AdjustTerrainCS.FindKernel("HorzBlur");
		m_BlurVertKernal = m_AdjustTerrainCS.FindKernel("VertBlur");
		m_SmoothTerrainKernal = m_AdjustTerrainCS.FindKernel("SmoothTerrain");
		m_LevelTerrainKernal = m_AdjustTerrainCS.FindKernel("LevelTerrain");
		m_SlopeTerrainKernal = m_AdjustTerrainCS.FindKernel("SlopeTerrain");
		m_BuildingUpgrade = new NativeParallelHashMap<Entity, Entity>(1024, Allocator.Persistent);
		m_CommandBuffer = new CommandBuffer();
		m_CommandBuffer.name = "TerrainAdjust";
		m_CascadeCB = new CommandBuffer();
		m_CascadeCB.name = "Terrain Cascade";
		Shader shader = Resources.Load<Shader>("BuildingLot");
		m_MasterBuildingLotMaterial = new Material(shader);
		Shader shader2 = Resources.Load<Shader>("Lane");
		m_MasterLaneMaterial = new Material(shader2);
		Shader shader3 = Resources.Load<Shader>("Area");
		m_MasterAreaMaterial = new Material(shader3);
		m_TerrainBlit = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("TerrainCascadeBlit"));
		m_ClipMaterial = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("RoadClip"));
		m_TerrainMinMax = new TerrainMinMaxMap();
		m_MapOffsetScale = new Vector4(0f, 0f, 1f, 1f);
		m_UpdateArea = float4.zero;
		m_TerrainChangeArea = float4.zero;
		m_TerrainChange = false;
		m_BuildingCullList = new NativeList<BuildingUtils.LotInfo>(1000, Allocator.Persistent);
		m_LaneCullList = new NativeList<LaneSection>(1000, Allocator.Persistent);
		m_TriangleCullList = new NativeList<AreaTriangle>(100, Allocator.Persistent);
		m_EdgeCullList = new NativeList<AreaEdge>(100, Allocator.Persistent);
		m_ClipMapList = new NativeList<ClipMapDraw>(1000, Allocator.Persistent);
		m_CascadeCulling = new List<CascadeCullInfo>(4);
		for (int i = 0; i < 4; i++)
		{
			m_CascadeCulling.Add(new CascadeCullInfo(m_MasterBuildingLotMaterial, m_MasterLaneMaterial, m_MasterAreaMaterial));
		}
		m_BuildingInstanceData = new ManagedStructuredBuffers<BuildingLotDraw>(10000);
		m_LaneInstanceData = new ManagedStructuredBuffers<LaneDraw>(10000);
		m_LaneRaisedInstanceData = new ManagedStructuredBuffers<LaneDraw>(10000);
		m_TriangleInstanceData = new ManagedStructuredBuffers<AreaTriangle>(1000);
		m_EdgeInstanceData = new ManagedStructuredBuffers<AreaEdge>(1000);
		m_LastPreviewWrite = int4.zero;
		m_LastWorldPreviewWrite = int4.zero;
		m_LastWorldWrite = int4.zero;
		m_LastWrite = int4.zero;
		m_LastRequest = int4.zero;
		m_FailCount = 0;
		baseLod = 0;
		m_NewMap = true;
		m_NewMapThisFrame = true;
		m_CascadeReset = true;
		m_RoadUpdate = false;
		m_AreaUpdate = false;
		m_ClipMapBuffer = new ManagedStructuredBuffers<ClipMapDraw>(10000);
		m_CurrentClipMap = null;
		heightMapRenderRequired = false;
		heightMapSliceUpdated = new bool[4];
		heightMapSliceUpdatedLast = new bool[4];
		heightMapViewport = new float4[4];
		heightMapViewportUpdated = new float4[4];
		heightMapCullArea = new float4[4];
		m_BrushQuery = GetEntityQuery(ComponentType.ReadOnly<Brush>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
		m_BuildingsChanged = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Game.Objects.Object>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.Lot>(),
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Pillar>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Game.Objects.Object>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.Lot>(),
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Pillar>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Game.Objects.Object>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.Lot>(),
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Pillar>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_BuildingGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.Object>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.Lot>(),
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Pillar>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_RoadsChanged = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<EdgeGeometry>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<NodeGeometry>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_RoadsGroup = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<EdgeGeometry>(),
				ComponentType.ReadOnly<NodeGeometry>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_AreasChanged = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Clip>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Areas.Terrain>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_AreasQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Clip>(),
				ComponentType.ReadOnly<Game.Areas.Terrain>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_EditorLotQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.Lot>(),
				ComponentType.ReadOnly<Game.Objects.Transform>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Error>(),
				ComponentType.ReadOnly<Warning>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Game.Objects.Transform>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Error>(),
				ComponentType.ReadOnly<Warning>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_GroundHeightSystem = base.World.GetOrCreateSystemManaged<GroundHeightSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		CreateRoadMeshes();
		m_Heightmap = null;
		m_HeightmapCascade = null;
		m_HeightmapDepth = null;
		m_WorldMapEditable = null;
		m_DownscaledHeightmap = null;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		CoreUtils.Destroy(m_TerrainBlit);
		CoreUtils.Destroy(m_ClipMaterial);
		if (m_CPUHeights.IsCreated)
		{
			m_CPUHeights.Dispose();
		}
		if (m_CPUHeightsDownscaled.IsCreated)
		{
			m_CPUHeightsDownscaled.Dispose();
		}
		if (m_DownscaledHeightmap != null && m_DownscaledHeightmap.IsCreated())
		{
			CoreUtils.Destroy(m_DownscaledHeightmap);
		}
		CoreUtils.Destroy(m_Heightmap);
		CoreUtils.Destroy(m_HeightmapCascade);
		CoreUtils.Destroy(m_HeightmapObjectsLayer);
		CoreUtils.Destroy(m_WorldMapEditable);
		worldMapAsset?.Unload();
		CoreUtils.Destroy(m_HeightmapDepth);
		if (m_BuildingCullList.IsCreated)
		{
			m_CullFinished.Complete();
			m_BuildingCullList.Dispose();
		}
		if (m_LaneCullList.IsCreated)
		{
			m_CullFinished.Complete();
			m_LaneCullList.Dispose();
		}
		if (m_TriangleCullList.IsCreated)
		{
			m_CullFinished.Complete();
			m_TriangleCullList.Dispose();
		}
		if (m_EdgeCullList.IsCreated)
		{
			m_CullFinished.Complete();
			m_EdgeCullList.Dispose();
		}
		if (m_ClipMapList.IsCreated)
		{
			m_ClipMapCull.Complete();
			m_ClipMapList.Dispose();
		}
		if (m_BuildingInstanceData != null)
		{
			m_BuildingInstanceData.Dispose();
			m_BuildingInstanceData = null;
		}
		if (m_LaneInstanceData != null)
		{
			m_LaneInstanceData.Dispose();
			m_LaneInstanceData = null;
		}
		if (m_LaneRaisedInstanceData != null)
		{
			m_LaneRaisedInstanceData.Dispose();
			m_LaneRaisedInstanceData = null;
		}
		if (m_TriangleInstanceData != null)
		{
			m_TriangleInstanceData.Dispose();
			m_TriangleInstanceData = null;
		}
		if (m_EdgeInstanceData != null)
		{
			m_EdgeInstanceData.Dispose();
			m_EdgeInstanceData = null;
		}
		if (m_ClipMapBuffer != null)
		{
			m_ClipMapBuffer.Dispose();
			m_ClipMapBuffer = null;
		}
		for (int i = 0; i < 4; i++)
		{
			if (!m_CascadeCulling[i].m_BuildingHandle.IsCompleted)
			{
				m_CascadeCulling[i].m_BuildingHandle.Complete();
			}
			if (m_CascadeCulling[i].m_BuildingRenderList.IsCreated)
			{
				m_CascadeCulling[i].m_BuildingRenderList.Dispose();
			}
			if (!m_CascadeCulling[i].m_LaneHandle.IsCompleted)
			{
				m_CascadeCulling[i].m_LaneHandle.Complete();
			}
			if (m_CascadeCulling[i].m_LaneRenderList.IsCreated)
			{
				m_CascadeCulling[i].m_LaneRenderList.Dispose();
			}
			if (m_CascadeCulling[i].m_LaneRaisedRenderList.IsCreated)
			{
				m_CascadeCulling[i].m_LaneRaisedRenderList.Dispose();
			}
			if (!m_CascadeCulling[i].m_AreaHandle.IsCompleted)
			{
				m_CascadeCulling[i].m_AreaHandle.Complete();
			}
			if (m_CascadeCulling[i].m_TriangleRenderList.IsCreated)
			{
				m_CascadeCulling[i].m_TriangleRenderList.Dispose();
			}
			if (m_CascadeCulling[i].m_EdgeRenderList.IsCreated)
			{
				m_CascadeCulling[i].m_EdgeRenderList.Dispose();
			}
		}
		if (m_BuildingUpgrade.IsCreated)
		{
			m_BuildingUpgradeDependencies.Complete();
			m_BuildingUpgrade.Dispose();
		}
		m_CascadeCB.Dispose();
		m_CommandBuffer.Dispose();
		m_TerrainMinMax.Dispose();
		base.OnDestroy();
	}

	private unsafe static void SerializeHeightmap<TWriter>(TWriter writer, Texture heightmap) where TWriter : IWriter
	{
		if (heightmap == null)
		{
			writer.Write(0);
			writer.Write(0);
			return;
		}
		int width = heightmap.width;
		writer.Write(width);
		int height = heightmap.height;
		writer.Write(height);
		NativeArray<ushort> output = new NativeArray<ushort>(heightmap.width * heightmap.height, Allocator.Persistent);
		AsyncGPUReadback.RequestIntoNativeArray(ref output, heightmap).WaitForCompletion();
		NativeArray<byte> nativeArray = new NativeArray<byte>(output.Length * 2, Allocator.Temp);
		NativeCompression.FilterDataBeforeWrite((IntPtr)output.GetUnsafeReadOnlyPtr(), (IntPtr)nativeArray.GetUnsafePtr(), nativeArray.Length, 2);
		output.Dispose();
		NativeArray<byte> value = nativeArray;
		writer.Write(value);
		nativeArray.Dispose();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		SerializeHeightmap(writer, worldHeightmap);
		SerializeHeightmap(writer, m_Heightmap);
		float2 value = heightScaleOffset;
		writer.Write(value);
		float2 value2 = playableOffset;
		writer.Write(value2);
		float2 value3 = playableArea;
		writer.Write(value3);
		float2 value4 = worldOffset;
		writer.Write(value4);
		float2 value5 = worldSize;
		writer.Write(value5);
		float2 value6 = worldHeightMinMax;
		writer.Write(value6);
	}

	private unsafe static Texture2D DeserializeHeightmap<TReader>(TReader reader, string name, ref NativeArray<ushort> unfiltered, bool makeNoLongerReadable) where TReader : IReader
	{
		reader.Read(out int value);
		reader.Read(out int value2);
		if (value != 0 && value2 != 0)
		{
			Texture2D texture2D = new Texture2D(value, value2, GraphicsFormat.R16_UNorm, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate)
			{
				hideFlags = HideFlags.HideAndDontSave,
				name = name,
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			using NativeArray<ushort> nativeArray = texture2D.GetRawTextureData<ushort>();
			if (reader.context.version >= Version.terrainWaterSnowCompression)
			{
				if (unfiltered.Length != nativeArray.Length)
				{
					ArrayExtensions.ResizeArray(ref unfiltered, nativeArray.Length);
				}
				NativeArray<byte> nativeArray2 = unfiltered.Reinterpret<byte>(2);
				NativeArray<byte> value3 = nativeArray2;
				reader.Read(value3);
				NativeCompression.UnfilterDataAfterRead((IntPtr)nativeArray2.GetUnsafePtr(), (IntPtr)nativeArray.GetUnsafePtr(), nativeArray2.Length, 2);
			}
			else
			{
				NativeArray<ushort> value4 = nativeArray;
				reader.Read(value4);
			}
			texture2D.Apply(updateMipmaps: false, makeNoLongerReadable);
			return texture2D;
		}
		return null;
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_Loaded = true;
		if (!reader.context.format.Has(FormatTags.TerrainSystemCleanup))
		{
			if (reader.context.version >= Version.terrainGuidToHash)
			{
				reader.Read(out Colossal.Hash128 _);
			}
			else
			{
				reader.Read(out string _);
			}
		}
		if (reader.context.version >= Version.terrainInSaves)
		{
			Texture2D texture2D = null;
			TextureAsset textureAsset = null;
			if (reader.context.version >= Version.worldmapInSaves)
			{
				texture2D = DeserializeHeightmap(reader, "LoadedWorldHeightMap", ref m_CPUHeights, makeNoLongerReadable: true);
				Texture2D texture2D2 = DeserializeHeightmap(reader, "LoadedHeightmap", ref m_CPUHeights, makeNoLongerReadable: false);
				reader.Read(out float2 value3);
				reader.Read(out float2 value4);
				reader.Read(out float2 value5);
				reader.Read(out float2 value6);
				reader.Read(out float2 value7);
				reader.Read(out float2 value8);
				InitializeTerrainData(texture2D2, texture2D, value3, value4, value5, value6, value7, value8);
				if (textureAsset != worldMapAsset)
				{
					worldMapAsset?.Unload();
				}
				worldMapAsset = textureAsset;
				UnityEngine.Object.Destroy(texture2D2);
				return;
			}
			throw new NotSupportedException($"Saves prior to {Version.worldmapInSaves} are no longer supported");
		}
		throw new NotSupportedException($"Saves prior to {Version.terrainInSaves} are no longer supported");
	}

	public void SetDefaults(Context context)
	{
		m_Loaded = true;
		LoadTerrain();
	}

	public void Clear()
	{
		CoreUtils.Destroy(m_Heightmap);
	}

	public void TerrainHeightsReadyAfterLoading()
	{
		m_HeightsReadyAfterLoading = true;
	}

	private void LoadTerrain()
	{
		InitializeTerrainData(null, null, kDefaultHeightScaleOffset, kDefaultMapOffset, kDefaultMapSize, kDefaultWorldOffset, kDefaultWorldSize, float2.zero);
	}

	private void InitializeTerrainData(Texture2D inMap, Texture2D worldMap, float2 heightScaleOffset, float2 inMapCorner, float2 inMapSize, float2 inWorldCorner, float2 inWorldSize, float2 inWorldHeightMinMax)
	{
		Texture2D texture2D = ((inMap != null) ? inMap : CreateDefaultHeightmap((worldMap != null) ? worldMap.width : kDefaultHeightmapWidth, (worldMap != null) ? worldMap.height : kDefaultHeightmapHeight));
		SetHeightmap(texture2D);
		SetWorldHeightmap(worldMap, m_ToolSystem.actionMode.IsEditor());
		FinalizeTerrainData(texture2D, worldMap, heightScaleOffset, inMapCorner, inMapSize, inWorldCorner, inWorldSize, inWorldHeightMinMax);
		if (texture2D != inMap)
		{
			UnityEngine.Object.Destroy(texture2D);
		}
	}

	public void ReplaceHeightmap(Texture2D inMap)
	{
		Texture2D texture2D = ((inMap != null) ? inMap : CreateDefaultHeightmap((worldHeightmap != null) ? worldHeightmap.width : kDefaultHeightmapWidth, (worldHeightmap != null) ? worldHeightmap.height : kDefaultHeightmapHeight));
		Texture2D texture2D2 = ToR16(texture2D);
		SetHeightmap(texture2D2);
		FinalizeTerrainData(texture2D2, null, heightScaleOffset, kDefaultMapOffset, kDefaultMapSize, kDefaultWorldOffset, kDefaultWorldSize, worldHeightMinMax);
		if (texture2D2 != texture2D)
		{
			UnityEngine.Object.Destroy(texture2D2);
		}
		if (texture2D != inMap)
		{
			UnityEngine.Object.Destroy(texture2D);
		}
		m_WaterSystem.UseActiveCellsCulling = false;
		m_WaterSystem.TerrainWillChange();
	}

	public void ReplaceWorldHeightmap(Texture2D inMap)
	{
		Texture2D texture2D = ToR16(inMap);
		SetWorldHeightmap(texture2D, m_ToolSystem.actionMode.IsEditor());
		FinalizeTerrainData(null, texture2D, heightScaleOffset, kDefaultMapOffset, kDefaultMapSize, kDefaultWorldOffset, kDefaultWorldSize, float2.zero);
		if (texture2D != inMap && texture2D != worldHeightmap)
		{
			UnityEngine.Object.Destroy(texture2D);
		}
		m_WaterSystem.UseActiveCellsCulling = false;
		m_WaterSystem.TerrainWillChange();
	}

	public void SetTerrainProperties(float2 heightScaleOffset)
	{
		FinalizeTerrainData(null, null, heightScaleOffset, playableOffset, playableArea, worldOffset, worldSize, worldHeightMinMax);
	}

	public void DownSampleHeightMap()
	{
		if (HasBackdrop)
		{
			m_CommandBuffer.Clear();
			m_CommandBuffer.SetComputeTextureParam(m_AdjustTerrainCS, m_DownsampleTerrainKernel, "_Terrain", m_HeightmapCascade);
			m_CommandBuffer.SetComputeTextureParam(m_AdjustTerrainCS, m_DownsampleTerrainKernel, ShaderID._HeightmapDownscaled, m_DownscaledHeightmap);
			int num = m_DownscaledHeightmap.width / 8;
			m_CommandBuffer.DispatchCompute(m_AdjustTerrainCS, m_DownsampleTerrainKernel, num, num, 1);
			Graphics.ExecuteCommandBuffer(m_CommandBuffer);
			m_CommandBuffer.Clear();
		}
	}

	private void SetHeightmap(Texture2D map)
	{
		if (m_Heightmap == null || m_Heightmap.width != map.width || m_Heightmap.height != map.height)
		{
			if (m_Heightmap != null)
			{
				m_Heightmap.Release();
				UnityEngine.Object.Destroy(m_Heightmap);
			}
			m_Heightmap = new RenderTexture(map.width, map.height, 0, GraphicsFormat.R16_UNorm)
			{
				hideFlags = HideFlags.HideAndDontSave,
				enableRandomWrite = true,
				name = "TerrainHeights",
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			m_Heightmap.Create();
		}
		Graphics.CopyTexture(map, m_Heightmap);
		if (worldHeightmap != null && (worldHeightmap.width != m_Heightmap.width || worldHeightmap.height != m_Heightmap.height))
		{
			DestroyWorldMap();
		}
	}

	private void SetWorldHeightmap(Texture2D map, bool isEditor)
	{
		if (map == null || map.width != m_Heightmap.width || map.height != m_Heightmap.height)
		{
			DestroyWorldMap();
		}
		else if (isEditor)
		{
			if (m_WorldMapEditable == null || worldHeightmap != m_WorldMapEditable || m_WorldMapEditable.width != map.width || m_WorldMapEditable.height != map.height)
			{
				DestroyWorldMap();
				m_WorldMapEditable = new RenderTexture(map.width, map.height, 0, GraphicsFormat.R16_UNorm)
				{
					hideFlags = HideFlags.HideAndDontSave,
					enableRandomWrite = true,
					name = "TerrainWorldHeights",
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp
				};
				m_WorldMapEditable.Create();
				worldHeightmap = m_WorldMapEditable;
			}
			Graphics.CopyTexture(map, m_WorldMapEditable);
		}
		else
		{
			if (map != worldHeightmap && (m_WorldMapEditable != null || worldHeightmap != null))
			{
				DestroyWorldMap();
			}
			worldHeightmap = map;
		}
	}

	private void FinalizeTerrainData(Texture2D map, Texture2D worldMap, float2 heightScaleOffset, float2 inMapCorner, float2 inMapSize, float2 inWorldCorner, float2 inWorldSize, float2 inWorldHeightMinMax)
	{
		this.heightScaleOffset = heightScaleOffset;
		if (math.all(inWorldSize == inMapSize) || worldHeightmap == null)
		{
			baseLod = 0;
			playableArea = inMapSize;
			worldSize = inMapSize;
			playableOffset = inMapCorner;
			worldOffset = inMapCorner;
		}
		else
		{
			baseLod = 1;
			playableArea = inMapSize;
			worldSize = inWorldSize;
			playableOffset = inMapCorner;
			worldOffset = inWorldCorner;
		}
		m_NewMap = true;
		m_NewMapThisFrame = true;
		m_CascadeReset = true;
		worldHeightMinMax = inWorldHeightMinMax;
		m_WorldOffsetScale = new float4((playableOffset - worldOffset) / worldSize, playableArea / worldSize);
		float3 @float = new float3(playableArea.x, heightScaleOffset.x, playableArea.y);
		float3 xyz = 1f / @float;
		float3 xyz2 = -positionOffset;
		m_MapOffsetScale = new Vector4(0f - positionOffset.x, 0f - positionOffset.z, 1f / @float.x, 1f / @float.z);
		if (m_HeightmapCascade == null || m_HeightmapCascade.width != heightmap.width || m_HeightmapCascade.height != heightmap.height)
		{
			if (m_HeightmapCascade != null)
			{
				m_HeightmapCascade.Release();
				UnityEngine.Object.Destroy(m_HeightmapCascade);
				m_HeightmapCascade = null;
			}
			m_HeightmapCascade = new RenderTexture(heightmap.width, heightmap.height, 0, GraphicsFormat.R16_UNorm)
			{
				hideFlags = HideFlags.HideAndDontSave,
				enableRandomWrite = false,
				name = "TerrainHeightsCascade",
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp,
				dimension = TextureDimension.Tex2DArray,
				volumeDepth = 4
			};
			m_HeightmapCascade.Create();
			if (m_HeightmapObjectsLayer != null)
			{
				m_HeightmapObjectsLayer.Release();
				UnityEngine.Object.Destroy(m_HeightmapObjectsLayer);
				m_HeightmapObjectsLayer = null;
			}
			m_HeightmapObjectsLayer = new RenderTexture(heightmap.width, heightmap.height, 0, GraphicsFormat.R16_UNorm)
			{
				hideFlags = HideFlags.HideAndDontSave,
				enableRandomWrite = false,
				name = "TerrainHeightsObjectsLayer",
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp,
				dimension = TextureDimension.Tex2D
			};
			m_HeightmapObjectsLayer.Create();
		}
		if (m_HeightmapDepth == null || m_HeightmapDepth.width != heightmap.width || m_HeightmapDepth.height != heightmap.height)
		{
			if (m_HeightmapDepth != null)
			{
				m_HeightmapDepth.Release();
				UnityEngine.Object.Destroy(m_HeightmapDepth);
				m_HeightmapDepth = null;
			}
			m_HeightmapDepth = new RenderTexture(heightmap.width, heightmap.height, 16, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear)
			{
				name = "HeightmapDepth"
			};
			m_HeightmapDepth.Create();
		}
		if (map != null)
		{
			Graphics.CopyTexture(map, 0, 0, m_HeightmapCascade, baseLod, 0);
		}
		m_CommandBuffer.SetRenderTarget(m_HeightmapObjectsLayer);
		m_CommandBuffer.ClearRenderTarget(clearDepth: false, clearColor: true, UnityEngine.Color.black);
		Graphics.ExecuteCommandBuffer(m_CommandBuffer);
		m_CommandBuffer.Clear();
		m_CascadeRanges = new float4[4];
		m_ShaderCascadeRanges = new Vector4[4];
		m_CPUHeightReaders.Complete();
		m_CPUHeightReaders = default(JobHandle);
		m_CPUDownSampleHeightReaders.Complete();
		m_CPUDownSampleHeightReaders = default(JobHandle);
		InitializeBackdrop(m_HeightmapCascade);
		for (int i = 0; i < 4; i++)
		{
			m_CascadeRanges[i] = new float4(0f, 0f, 0f, 0f);
		}
		m_CascadeRanges[baseLod] = new float4(playableOffset, playableOffset + playableArea);
		if (baseLod > 0)
		{
			m_CascadeRanges[0] = new float4(worldOffset, worldOffset + worldSize);
			if (worldMap != null)
			{
				Graphics.CopyTexture(worldMap, 0, 0, m_HeightmapCascade, 0, 0);
			}
		}
		m_UpdateArea = new float4(m_CascadeRanges[baseLod]);
		Shader.SetGlobalTexture("colossal_TerrainTexture", m_Heightmap);
		if (HasBackdrop)
		{
			Shader.SetGlobalTexture("colossal_TerrainDownScaledTexture", m_DownscaledHeightmap);
		}
		Shader.SetGlobalVector("colossal_TerrainScale", new float4(xyz, 0f));
		Shader.SetGlobalVector("colossal_TerrainOffset", new float4(xyz2, 0f));
		Shader.SetGlobalVector("colossal_TerrainCascadeLimit", new float4(0.5f / (float)m_HeightmapCascade.width, 0.5f / (float)m_HeightmapCascade.height, 0f, 0f));
		Shader.SetGlobalTexture("colossal_TerrainTextureArray", m_HeightmapCascade);
		Shader.SetGlobalInt("colossal_TerrainTextureArrayBaseLod", baseLod);
		if (map != null)
		{
			WriteCPUHeights(map.GetRawTextureData<ushort>());
		}
		m_TerrainMinMax.Init((worldHeightmap != null) ? 1024 : 512, (worldHeightmap != null) ? worldHeightmap.width : m_Heightmap.width);
		m_TerrainMinMax.UpdateMap(this, m_Heightmap, worldHeightmap);
	}

	private void InitializeBackdrop(RenderTexture map)
	{
		if (HasBackdrop && (m_DownscaledHeightmap == null || m_DownscaledHeightmap.width != map.width / kDownScaledHeightmapScale || m_DownscaledHeightmap.height != map.height / kDownScaledHeightmapScale))
		{
			if (m_DownscaledHeightmap != null)
			{
				m_DownscaledHeightmap.Release();
				UnityEngine.Object.Destroy(m_DownscaledHeightmap);
			}
			m_DownscaledHeightmap = new RenderTexture(map.width / kDownScaledHeightmapScale, map.height / kDownScaledHeightmapScale, 0, GraphicsFormat.R16_UNorm)
			{
				hideFlags = HideFlags.HideAndDontSave,
				enableRandomWrite = true,
				name = "TerrainHeightsDownscaled",
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			m_DownscaledHeightmap.Create();
			if (m_CPUHeightsDownscaled.IsCreated)
			{
				m_CPUHeightsDownscaled.Dispose();
			}
			m_CPUHeightsDownscaled = new NativeArray<ushort>(m_DownscaledHeightmap.width * m_DownscaledHeightmap.height, Allocator.Persistent);
		}
		DownSampleHeightMap();
	}

	private void DestroyWorldMap()
	{
		if (worldHeightmap != null)
		{
			if (worldHeightmap is RenderTexture renderTexture)
			{
				renderTexture.Release();
			}
			UnityEngine.Object.Destroy(worldHeightmap);
			worldHeightmap = null;
		}
		if (m_WorldMapEditable != null)
		{
			m_WorldMapEditable.Release();
			UnityEngine.Object.Destroy(m_WorldMapEditable);
			m_WorldMapEditable = null;
		}
		if (worldMapAsset != null)
		{
			worldMapAsset.Unload();
			worldMapAsset = null;
		}
	}

	private Texture2D CreateDefaultHeightmap(int width, int height)
	{
		Texture2D obj = new Texture2D(width, height, GraphicsFormat.R16_UNorm, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate)
		{
			hideFlags = HideFlags.HideAndDontSave,
			name = "DefaultHeightmap",
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp
		};
		SetDefaultHeights(obj);
		return obj;
	}

	private static void SetDefaultHeights(Texture2D targetHeightmap)
	{
		NativeArray<ushort> rawTextureData = targetHeightmap.GetRawTextureData<ushort>();
		ushort value = 8191;
		for (int i = 0; i < rawTextureData.Length; i++)
		{
			rawTextureData[i] = value;
		}
		targetHeightmap.Apply(updateMipmaps: false, makeNoLongerReadable: false);
	}

	private static Texture2D ToR16(Texture2D textureRGBA64)
	{
		if (textureRGBA64 != null && textureRGBA64.graphicsFormat != GraphicsFormat.R16_UNorm)
		{
			NativeArray<ushort> rawTextureData = textureRGBA64.GetRawTextureData<ushort>();
			NativeArray<ushort> data = new NativeArray<ushort>(textureRGBA64.width * textureRGBA64.height, Allocator.Temp);
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = rawTextureData[i * 4];
			}
			Texture2D texture2D = new Texture2D(textureRGBA64.width, textureRGBA64.height, GraphicsFormat.R16_UNorm, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
			texture2D.SetPixelData(data, 0);
			texture2D.Apply();
			return texture2D;
		}
		return textureRGBA64;
	}

	public static bool IsValidHeightmapFormat(Texture2D tex)
	{
		if (tex.width == kDefaultHeightmapWidth && tex.height == kDefaultHeightmapHeight)
		{
			if (tex.graphicsFormat != GraphicsFormat.R16_UNorm)
			{
				return tex.graphicsFormat == GraphicsFormat.R16G16B16A16_UNorm;
			}
			return true;
		}
		return false;
	}

	private void SaveBitmap(NativeArray<ushort> buffer, int width, int height)
	{
		using System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(File.OpenWrite("heightmapResult.raw"));
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				binaryWriter.Write(buffer[j + i * width]);
			}
		}
	}

	private void EnsureCPUHeights(int length)
	{
		if (m_CPUHeights.IsCreated)
		{
			if (m_CPUHeights.Length != length)
			{
				m_CPUHeights.Dispose();
				m_CPUHeights = new NativeArray<ushort>(length, Allocator.Persistent);
			}
		}
		else
		{
			m_CPUHeights = new NativeArray<ushort>(length, Allocator.Persistent);
		}
	}

	private void WriteCPUHeights(NativeArray<ushort> buffer)
	{
		EnsureCPUHeights(buffer.Length);
		m_CPUHeights.CopyFrom(buffer);
		m_GroundHeightSystem.AfterReadHeights();
	}

	private void WriteCPUHeights(NativeArray<ushort> buffer, int4 offsets)
	{
		for (int i = 0; i < offsets.w; i++)
		{
			int dstIndex = (offsets.y + i) * m_HeightmapCascade.width + offsets.x;
			NativeArray<ushort>.Copy(buffer, i * offsets.z, m_CPUHeights, dstIndex, offsets.z);
		}
		m_GroundHeightSystem.AfterReadHeights();
	}

	private void UpdateGPUReadback()
	{
		m_TerrainMinMax.Update();
		if (m_AsyncGPUReadback.isPending)
		{
			if (!m_AsyncGPUReadback.hasError)
			{
				if (m_AsyncGPUReadback.done)
				{
					NativeArray<ushort> data = m_AsyncGPUReadback.GetData<ushort>();
					WriteCPUHeights(data, m_LastRequest);
					if (m_UpdateOutOfDate)
					{
						m_UpdateOutOfDate = false;
						OnHeightsChanged();
					}
					else
					{
						m_HeightMapChanged = false;
					}
					m_FailCount = 0;
				}
				m_AsyncGPUReadback.IncrementFrame();
			}
			else if (++m_FailCount < 10)
			{
				m_GroundHeightSystem.BeforeReadHeights();
				m_AsyncGPUReadback.Request(m_HeightmapCascade, 0, m_LastRequest.x, m_LastRequest.z, m_LastRequest.y, m_LastRequest.w, baseLod, 1);
			}
			else
			{
				COSystemBase.baseLog.Error("m_AsyncGPUReadback.hasError");
				m_LastRequest = new int4(0, 0, m_HeightmapCascade.width, m_HeightmapCascade.height);
				m_GroundHeightSystem.BeforeReadHeights();
				m_AsyncGPUReadback.Request(m_HeightmapCascade, 0, 0, m_HeightmapCascade.width, 0, m_HeightmapCascade.height, baseLod, 1);
			}
		}
		else
		{
			m_HeightMapChanged = false;
		}
		if (HasBackdrop && m_AsyncDownscaledHeightsGPUReadback.isPending && !m_AsyncDownscaledHeightsGPUReadback.hasError)
		{
			if (m_AsyncDownscaledHeightsGPUReadback.done)
			{
				NativeArray<ushort>.Copy(m_AsyncDownscaledHeightsGPUReadback.GetData<ushort>(), m_CPUHeightsDownscaled);
			}
			m_AsyncDownscaledHeightsGPUReadback.IncrementFrame();
		}
	}

	public void TriggerAsyncChange()
	{
		m_UpdateOutOfDate = m_AsyncGPUReadback.isPending;
		m_HeightMapChanged = true;
		if (!m_UpdateOutOfDate)
		{
			OnHeightsChanged();
		}
	}

	public void HandleNewMap()
	{
		m_NewMap = false;
	}

	private void OnHeightsChanged()
	{
		m_LastRequest = m_LastWrite;
		m_LastWrite = int4.zero;
		if (m_LastRequest.z == 0 || m_LastRequest.w == 0)
		{
			m_LastRequest = new int4(0, 0, m_HeightmapCascade.width, m_HeightmapCascade.height);
		}
		m_GroundHeightSystem.BeforeReadHeights();
		m_AsyncGPUReadback.Request(m_HeightmapCascade, 0, m_LastRequest.x, m_LastRequest.z, m_LastRequest.y, m_LastRequest.w, baseLod, 1);
		DownSampleHeightMap();
		if (HasBackdrop)
		{
			m_AsyncDownscaledHeightsGPUReadback.Request(m_DownscaledHeightmap, 0, 0, m_DownscaledHeightmap.width, 0, m_DownscaledHeightmap.height, 0, 1);
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_NewMapThisFrame = m_NewMap;
		if (!(m_Heightmap == null))
		{
			m_CPUHeightReaders.Complete();
			m_CPUHeightReaders = default(JobHandle);
			m_CPUDownSampleHeightReaders.Complete();
			m_CPUDownSampleHeightReaders = default(JobHandle);
			if (!freezeCascadeUpdates)
			{
				UpdateCascades(m_Loaded, m_HeightsReadyAfterLoading);
				m_Loaded = false;
				m_HeightsReadyAfterLoading = false;
			}
			DownSampleHeightMap();
			UpdateGPUReadback();
			UpdateGPUTerrain();
		}
	}

	private void UpdateGPUTerrain()
	{
		TerrainSurface validSurface = TerrainSurface.GetValidSurface();
		if (!(validSurface != null))
		{
			return;
		}
		validSurface.UsesCascade = true;
		GetCascadeInfo(out var _, out validSurface.BaseLOD, out var areas, out var ranges, out var size);
		validSurface.CascadeArea = areas;
		validSurface.CascadeRanges = ranges;
		validSurface.CascadeSizes = size;
		validSurface.CascadeTexture = m_HeightmapCascade;
		validSurface.TerrainHeightOffset = heightScaleOffset.y;
		validSurface.TerrainHeightScale = heightScaleOffset.x;
		validSurface.ShadowUseStencilClip = TerrainShadowUseStencilClip;
		if (validSurface.RenderClipAreas != null)
		{
			return;
		}
		validSurface.RenderClipAreas = delegate(CommandBuffer cmd, HDCamera hdCamera)
		{
			Camera camera = hdCamera.camera;
			bool flag = false;
			float w = math.tan(math.radians(camera.fieldOfView) * 0.5f) * 0.002f;
			m_ClipMaterial.SetBuffer(ShaderID._RoadData, clipMapBuffer);
			m_ClipMaterial.SetVector(ShaderID._ClipOffset, new float4(camera.transform.position, w));
			if (clipMapInstances > 0)
			{
				cmd.DrawMeshInstancedProcedural(m_ClipMesh, 0, m_ClipMaterial, 0, clipMapInstances);
			}
			if (m_RenderingSystem.hideOverlay || m_ToolSystem.activeTool == null || (m_ToolSystem.activeTool.requireAreas & AreaTypeMask.Surfaces) == 0)
			{
				cmd.DrawMesh(areaClipMesh, Matrix4x4.identity, m_ClipMaterial, 0, 2);
			}
			cmd.DrawProcedural(Matrix4x4.identity, m_ClipMaterial, flag ? 4 : 3, MeshTopology.Triangles, 3, 1);
		};
	}

	private void ApplyToTerrain(RenderTexture target, RenderTexture source, float delta, TerraformingType type, Bounds2 area, Brush brush, Texture texture, bool worldMap)
	{
		if (target == null || !target.IsCreated())
		{
			return;
		}
		if (delta == 0f || brush.m_Strength == 0f)
		{
			if (worldMap && source != null && m_LastWorldPreviewWrite.z != 0)
			{
				m_CommandBuffer.Clear();
				m_CommandBuffer.CopyTexture(source, 0, 0, m_LastWorldPreviewWrite.x, m_LastWorldPreviewWrite.y, m_LastWorldPreviewWrite.z, m_LastWorldPreviewWrite.w, target, 0, 0, m_LastWorldPreviewWrite.x, m_LastWorldPreviewWrite.y);
				Graphics.ExecuteCommandBuffer(m_CommandBuffer);
				m_LastWorldPreviewWrite = Unity.Mathematics.int4.zero;
			}
			if (!worldMap && source != null && m_LastPreviewWrite.z != 0)
			{
				m_CommandBuffer.Clear();
				m_CommandBuffer.CopyTexture(source, 0, 0, m_LastPreviewWrite.x, m_LastPreviewWrite.y, m_LastPreviewWrite.z, m_LastPreviewWrite.w, target, 0, 0, m_LastPreviewWrite.x, m_LastPreviewWrite.y);
				Graphics.ExecuteCommandBuffer(m_CommandBuffer);
				m_LastPreviewWrite = Unity.Mathematics.int4.zero;
			}
			return;
		}
		float x = delta * brush.m_Strength * GetTerrainAdjustmentSpeed(type) / heightScaleOffset.x;
		float2 @float = (worldMap ? worldSize : playableArea);
		float2 float2 = (worldMap ? worldOffset : playableOffset);
		float num = math.max(@float.x, @float.y);
		float2 float3 = (brush.m_Position.xz - float2) / @float;
		m_GroundHeightSystem.GetUpdateBuffer().Add(in area);
		if (math.lengthsq(m_UpdateArea) > 0f)
		{
			m_UpdateArea.xy = math.min(m_UpdateArea.xy, area.min);
			m_UpdateArea.zw = math.max(m_UpdateArea.zw, area.max);
		}
		else
		{
			m_UpdateArea = new float4(area.min, area.max);
		}
		if (!m_TerrainChange)
		{
			m_TerrainChange = true;
			m_TerrainChangeArea = new float4(area.min, area.max);
		}
		else
		{
			m_TerrainChangeArea.xy = math.min(m_TerrainChangeArea.xy, area.min);
			m_TerrainChangeArea.zw = math.max(m_TerrainChangeArea.zw, area.max);
		}
		area.min -= float2;
		area.max -= float2;
		area.min /= @float;
		area.max /= @float;
		int4 @int = new int4((int)math.max(math.floor(area.min.x * (float)target.width), 0f), (int)math.max(math.floor(area.min.y * (float)target.height), 0f), (int)math.min(math.ceil(area.max.x * (float)target.width), target.width - 1), (int)math.min(math.ceil(area.max.y * (float)target.height), target.height - 1));
		Vector4 val = new Vector4(float3.x, float3.y, brush.m_Size / num * 0.5f, brush.m_Angle);
		int num2 = @int.z - @int.x + 1;
		int num3 = @int.w - @int.y + 1;
		int threadGroupsX = (num2 + 7) / 8;
		int threadGroupsY = (num3 + 7) / 8;
		m_CommandBuffer.Clear();
		int4 int2 = new int4(math.max(@int.x - 2, 0), math.max(@int.y - 2, 0), num2 + 4, num3 + 4);
		if (int2.x + int2.z < 0 || int2.x > target.width || int2.y + int2.w < 0 || int2.y > target.height || num2 <= 0 || num3 <= 0)
		{
			return;
		}
		if (int2.x + int2.z > target.width)
		{
			int2.z = target.width - int2.x;
		}
		if (int2.y + int2.w > target.height)
		{
			int2.w = target.height - int2.y;
		}
		if (source != null)
		{
			if (worldMap)
			{
				if (m_LastWorldPreviewWrite.z == 0)
				{
					m_CommandBuffer.CopyTexture(source, target);
				}
				else
				{
					m_CommandBuffer.CopyTexture(source, 0, 0, m_LastWorldPreviewWrite.x, m_LastWorldPreviewWrite.y, m_LastWorldPreviewWrite.z, m_LastWorldPreviewWrite.w, target, 0, 0, m_LastWorldPreviewWrite.x, m_LastWorldPreviewWrite.y);
				}
				m_LastWorldPreviewWrite = int2;
			}
			else
			{
				if (m_LastPreviewWrite.z == 0)
				{
					m_CommandBuffer.CopyTexture(source, target);
				}
				else
				{
					m_CommandBuffer.CopyTexture(source, 0, 0, m_LastPreviewWrite.x, m_LastPreviewWrite.y, m_LastPreviewWrite.z, m_LastPreviewWrite.w, target, 0, 0, m_LastPreviewWrite.x, m_LastPreviewWrite.y);
					float4 float4 = new float4((float)m_LastPreviewWrite.x * (1f / (float)target.width), (float)m_LastPreviewWrite.y * (1f / (float)target.width), (float)m_LastPreviewWrite.z * (1f / (float)target.width), (float)m_LastPreviewWrite.w * (1f / (float)target.width));
					float4 float5 = new float4(float2 + float4.xy * @float, float2 + (float4.xy + float4.zw) * @float);
					m_UpdateArea.xy = math.min(m_UpdateArea.xy, float5.xy);
					m_UpdateArea.zw = math.max(m_UpdateArea.zw, float5.zw);
				}
				m_LastPreviewWrite = int2;
			}
		}
		else if (worldMap)
		{
			if (m_LastWorldWrite.z == 0)
			{
				m_LastWorldWrite = int2;
			}
			else
			{
				int2 int3 = new int2(math.min(m_LastWorldWrite.x, int2.x), math.min(m_LastWorldWrite.y, int2.y));
				int2 int4 = new int2(math.max(m_LastWorldWrite.x + m_LastWorldWrite.z, int2.x + int2.z), math.max(m_LastWorldWrite.y + m_LastWorldWrite.w, int2.y + int2.w));
				m_LastWorldWrite.xy = int3;
				m_LastWorldWrite.zw = int4 - int3;
			}
		}
		else if (m_LastWrite.z == 0)
		{
			m_LastWrite = int2;
		}
		else
		{
			int2 int5 = new int2(math.min(m_LastWrite.x, int2.x), math.min(m_LastWrite.y, int2.y));
			int2 int6 = new int2(math.max(m_LastWrite.x + m_LastWrite.z, int2.x + int2.z), math.max(m_LastWrite.y + m_LastWrite.w, int2.y + int2.w));
			m_LastWrite.xy = int5;
			m_LastWrite.zw = int6 - int5;
		}
		m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._CenterSizeRotation, val);
		m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._Dims, new Vector4(num, target.width, target.height, 0f));
		int num4 = 0;
		Vector4 val2 = new Vector4(x, 0f, 0f, 0f);
		Vector4 val3 = Vector4.zero;
		switch (type)
		{
		case TerraformingType.Shift:
			num4 = m_ShiftTerrainKernal;
			break;
		case TerraformingType.Level:
			num4 = m_LevelTerrainKernal;
			val2.y = (brush.m_Target.y - positionOffset.y) / heightScaleOffset.x;
			break;
		case TerraformingType.Slope:
		{
			num4 = m_SlopeTerrainKernal;
			float3 float6 = brush.m_Target - brush.m_Start;
			val2.y = (brush.m_Target.y - positionOffset.y) / heightScaleOffset.x;
			val2.z = (brush.m_Start.y - positionOffset.y) / heightScaleOffset.x;
			val2.w = float6.y / heightScaleOffset.x;
			float4 zero = float4.zero;
			zero.xy = math.normalize(float6.xz);
			zero.z = 0f - math.dot((brush.m_Start.xz - float2) / @float, zero.xy);
			zero.w = math.length(float6.xz) / num;
			val3 = zero;
			break;
		}
		case TerraformingType.Soften:
		{
			RenderTextureDescriptor desc = new RenderTextureDescriptor
			{
				autoGenerateMips = false,
				bindMS = false,
				depthBufferBits = 0,
				dimension = TextureDimension.Tex2D,
				enableRandomWrite = true,
				graphicsFormat = GraphicsFormat.R16_UNorm,
				memoryless = RenderTextureMemoryless.None,
				height = num3 + 8,
				width = num2 + 8,
				volumeDepth = 1,
				mipCount = 1,
				msaaSamples = 1,
				sRGB = false,
				useDynamicScale = false,
				useMipMap = false
			};
			m_CommandBuffer.GetTemporaryRT(ShaderID._AvgTerrainHeightsTemp, desc);
			m_CommandBuffer.GetTemporaryRT(ShaderID._BlurTempHorz, desc);
			num4 = m_SmoothTerrainKernal;
			val2.y = desc.width;
			val2.z = desc.height;
			val3.x = 4f;
			val3.y = 4f;
			m_CommandBuffer.SetComputeTextureParam(m_AdjustTerrainCS, m_BlurHorzKernal, ShaderID._Heightmap, target);
			m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._BrushData, val2);
			m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._Range, new Vector4(@int.x - 4, @int.y - 4, @int.z + 4, @int.w + 4));
			int threadGroupsX2 = (num2 + 15) / 8;
			int threadGroupsY2 = num3 + 8;
			m_CommandBuffer.DispatchCompute(m_AdjustTerrainCS, m_BlurHorzKernal, threadGroupsX2, threadGroupsY2, 1);
			int threadGroupsX3 = num2 + 8;
			int threadGroupsY3 = (num3 + 15) / 8;
			m_CommandBuffer.DispatchCompute(m_AdjustTerrainCS, m_BlurVertKernal, threadGroupsX3, threadGroupsY3, 1);
			break;
		}
		default:
			num4 = m_ShiftTerrainKernal;
			break;
		}
		int num5 = 2;
		float4 float7 = ((worldHeightmap != null && !m_ToolSystem.actionMode.IsEditor()) ? new float4(num5, num5, target.width - num5, target.height - num5) : new float4(-1f, -1f, target.width + 1, target.height + 1));
		float val4 = 10f / heightScaleOffset.x;
		m_CommandBuffer.SetComputeTextureParam(m_AdjustTerrainCS, num4, ShaderID._Heightmap, target);
		m_CommandBuffer.SetComputeTextureParam(m_AdjustTerrainCS, num4, ShaderID._BrushTexture, texture);
		m_CommandBuffer.SetComputeTextureParam(m_AdjustTerrainCS, num4, ShaderID._WorldTexture, (worldHeightmap != null) ? worldHeightmap : Texture2D.whiteTexture);
		m_CommandBuffer.SetComputeTextureParam(m_AdjustTerrainCS, num4, ShaderID._WaterTexture, m_WaterSystem.WaterTexture);
		m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._HeightScaleOffset, new float4(heightScaleOffset.x, heightScaleOffset.y, 0f, 0f));
		m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._Range, new Vector4(@int.x, @int.y, @int.z, @int.w));
		m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._BrushData, val2);
		m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._BrushData2, val3);
		m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._ClampArea, float7);
		m_CommandBuffer.SetComputeVectorParam(m_AdjustTerrainCS, ShaderID._WorldOffsetScale, m_WorldOffsetScale);
		m_CommandBuffer.SetComputeFloatParam(m_AdjustTerrainCS, ShaderID._EdgeMaxDifference, val4);
		m_CommandBuffer.DispatchCompute(m_AdjustTerrainCS, num4, threadGroupsX, threadGroupsY, 1);
		if (type == TerraformingType.Soften)
		{
			m_CommandBuffer.ReleaseTemporaryRT(ShaderID._AvgTerrainHeightsTemp);
			m_CommandBuffer.ReleaseTemporaryRT(ShaderID._BlurTempHorz);
		}
		Graphics.ExecuteCommandBuffer(m_CommandBuffer);
	}

	public void PreviewBrush(TerraformingType type, Bounds2 area, Brush brush, Texture texture)
	{
	}

	public void ApplyBrush(TerraformingType type, Bounds2 area, Brush brush, Texture texture)
	{
		m_WaterSystem.TerrainWillChangeFromBrush(area);
		ApplyToTerrain(m_Heightmap, null, UnityEngine.Time.unscaledDeltaTime, type, area, brush, texture, worldMap: false);
		ApplyToTerrain(m_WorldMapEditable, null, UnityEngine.Time.unscaledDeltaTime, type, area, brush, texture, worldMap: true);
		UpdateMinMax(brush, area);
		TriggerAsyncChange();
	}

	public void UpdateMinMax(Brush brush, Bounds2 area)
	{
		if (worldHeightmap != null)
		{
			area.min -= worldOffset;
			area.max -= worldOffset;
			area.min /= worldSize;
			area.max /= worldSize;
		}
		else
		{
			area.min -= playableOffset;
			area.max -= playableOffset;
			area.min /= playableArea;
			area.max /= playableArea;
		}
		int4 area2 = new int4((int)math.max(math.floor(area.min.x * (float)m_Heightmap.width) - 1f, 0f), (int)math.max(math.floor(area.min.y * (float)m_Heightmap.height) - 1f, 0f), (int)math.min(math.ceil(area.max.x * (float)m_Heightmap.width) + 1f, m_Heightmap.width - 1), (int)math.min(math.ceil(area.max.y * (float)m_Heightmap.height) + 1f, m_Heightmap.height - 1));
		area2.zw -= area2.xy;
		area2.zw = math.clamp(area2.zw, new int2(m_Heightmap.width / m_TerrainMinMax.size, m_Heightmap.height / m_TerrainMinMax.size), new int2(m_Heightmap.width, m_Heightmap.height));
		m_TerrainMinMax.RequestUpdate(this, m_Heightmap, worldHeightmap, area2);
	}

	public void GetCascadeInfo(out int LODCount, out int baseLOD, out float4x4 areas, out float4 ranges, out float4 size)
	{
		LODCount = 4;
		baseLOD = baseLod;
		if (m_CascadeRanges != null)
		{
			areas = new float4x4(m_CascadeRanges[0].x, m_CascadeRanges[0].y, m_CascadeRanges[0].z, m_CascadeRanges[0].w, m_CascadeRanges[1].x, m_CascadeRanges[1].y, m_CascadeRanges[1].z, m_CascadeRanges[1].w, m_CascadeRanges[2].x, m_CascadeRanges[2].y, m_CascadeRanges[2].z, m_CascadeRanges[2].w, m_CascadeRanges[3].x, m_CascadeRanges[3].y, m_CascadeRanges[3].z, m_CascadeRanges[3].w);
			ranges = new float4(math.min(m_CascadeRanges[0].z - m_CascadeRanges[0].x, m_CascadeRanges[0].w - m_CascadeRanges[0].y) * 0.75f, math.min(m_CascadeRanges[1].z - m_CascadeRanges[1].x, m_CascadeRanges[1].w - m_CascadeRanges[1].y) * 0.75f, math.min(m_CascadeRanges[2].z - m_CascadeRanges[2].x, m_CascadeRanges[2].w - m_CascadeRanges[2].y) * 0.75f, math.min(m_CascadeRanges[3].z - m_CascadeRanges[3].x, m_CascadeRanges[3].w - m_CascadeRanges[3].y) * 0.75f);
			size = new float4(math.max(m_CascadeRanges[0].z - m_CascadeRanges[0].x, m_CascadeRanges[0].w - m_CascadeRanges[0].y), math.max(m_CascadeRanges[1].z - m_CascadeRanges[1].x, m_CascadeRanges[1].w - m_CascadeRanges[1].y), math.max(m_CascadeRanges[2].z - m_CascadeRanges[2].x, m_CascadeRanges[2].w - m_CascadeRanges[2].y), math.max(m_CascadeRanges[3].z - m_CascadeRanges[3].x, m_CascadeRanges[3].w - m_CascadeRanges[3].y));
		}
		else
		{
			areas = default(float4x4);
			ranges = default(float4);
			size = default(float4);
		}
	}

	public Texture GetCascadeTexture()
	{
		return m_HeightmapCascade;
	}

	public Texture GetObjectsLayerTexture()
	{
		return m_HeightmapObjectsLayer;
	}

	private bool Overlap(ref float4 A, ref float4 B)
	{
		if (A.x > B.z || B.x > A.z || A.z < B.x || B.z < A.x || A.y > B.w || B.y > A.w || A.w < B.y || B.w < A.y)
		{
			return false;
		}
		return true;
	}

	private float4 ClipViewport(float4 Viewport)
	{
		if (Viewport.x < 0f)
		{
			Viewport.z = math.max(Viewport.z + Viewport.x, 0f);
			Viewport.x = 0f;
		}
		else if (Viewport.x > 1f)
		{
			Viewport.x = 1f;
			Viewport.z = 0f;
		}
		if (Viewport.x + Viewport.z > 1f)
		{
			Viewport.z = math.max(1f - Viewport.x, 0f);
		}
		if (Viewport.y < 0f)
		{
			Viewport.w = math.max(Viewport.w + Viewport.y, 0f);
			Viewport.y = 0f;
		}
		else if (Viewport.y > 1f)
		{
			Viewport.y = 1f;
			Viewport.w = 0f;
		}
		if (Viewport.y + Viewport.w > 1f)
		{
			Viewport.w = math.max(1f - Viewport.y, 0f);
		}
		return Viewport;
	}

	private void UpdateCascades(bool isLoaded, bool heightsReadyAfterLoading)
	{
		float3 position = m_CameraUpdateSystem.position;
		float4 @float = new float4(0);
		float4 A = m_UpdateArea;
		heightMapRenderRequired = math.lengthsq(A) > 0f;
		m_UpdateArea = float4.zero;
		m_RoadUpdate = m_CascadeReset;
		m_AreaUpdate = m_CascadeReset;
		if (m_CascadeReset)
		{
			heightMapRenderRequired = true;
			A = m_CascadeRanges[baseLod];
		}
		NativeList<Bounds2> updateBuffer = m_GroundHeightSystem.GetUpdateBuffer();
		bool flag = isLoaded || !m_BuildingsChanged.IsEmptyIgnoreFilter;
		if (flag || (m_ToolSystem.actionMode.IsEditor() && !m_EditorLotQuery.IsEmpty))
		{
			ComponentTypeHandle<Game.Objects.Transform> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabRef> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Updated> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentLookup<ObjectGeometryData> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef);
			float4 area;
			if (flag)
			{
				m_BuildingUpgradeDependencies.Complete();
				m_BuildingUpgradeDependencies = default(JobHandle);
				NativeArray<ArchetypeChunk> nativeArray = (isLoaded ? m_BuildingGroup : m_BuildingsChanged).ToArchetypeChunkArray(Allocator.Temp);
				CompleteDependency();
				for (int i = 0; i < nativeArray.Length; i++)
				{
					NativeArray<Entity> nativeArray2 = nativeArray[i].GetNativeArray(entityTypeHandle);
					NativeArray<Game.Objects.Transform> nativeArray3 = nativeArray[i].GetNativeArray(ref typeHandle);
					NativeArray<PrefabRef> nativeArray4 = nativeArray[i].GetNativeArray(ref typeHandle2);
					bool flag2 = nativeArray[i].Has(ref typeHandle3);
					if (isLoaded)
					{
						heightMapRenderRequired = true;
						m_WaterSystem.TerrainWillChange();
						A = m_CascadeRanges[baseLod];
						break;
					}
					for (int j = 0; j < nativeArray3.Length; j++)
					{
						PrefabRef prefabRef = nativeArray4[j];
						if (CalculateBuildingCullArea(nativeArray3[j], prefabRef.m_Prefab, componentLookup, out area))
						{
							updateBuffer.Add(new Bounds2(area.xy, area.zw));
							m_WaterSystem.TerrainWillChange();
							if (!heightMapRenderRequired)
							{
								heightMapRenderRequired = true;
								A = area;
							}
							else
							{
								A.xy = math.min(A.xy, area.xy);
								A.zw = math.max(A.zw, area.zw);
							}
						}
						if (!flag2 || !m_BuildingUpgrade.TryGetValue(nativeArray2[j], out var item))
						{
							continue;
						}
						if (item != prefabRef.m_Prefab && CalculateBuildingCullArea(nativeArray3[j], item, componentLookup, out area))
						{
							if (!heightMapRenderRequired)
							{
								heightMapRenderRequired = true;
								A = area;
							}
							else
							{
								A.xy = math.min(A.xy, area.xy);
								A.zw = math.max(A.zw, area.zw);
							}
						}
						m_BuildingUpgrade.Remove(nativeArray2[j]);
					}
				}
				nativeArray.Dispose();
			}
			if (m_ToolSystem.actionMode.IsEditor() && !m_EditorLotQuery.IsEmpty)
			{
				NativeArray<ArchetypeChunk> nativeArray5 = m_EditorLotQuery.ToArchetypeChunkArray(Allocator.Temp);
				CompleteDependency();
				for (int k = 0; k < nativeArray5.Length; k++)
				{
					NativeArray<Game.Objects.Transform> nativeArray6 = nativeArray5[k].GetNativeArray(ref typeHandle);
					NativeArray<PrefabRef> nativeArray7 = nativeArray5[k].GetNativeArray(ref typeHandle2);
					for (int l = 0; l < nativeArray6.Length; l++)
					{
						PrefabRef prefabRef2 = nativeArray7[l];
						if (CalculateBuildingCullArea(nativeArray6[l], prefabRef2.m_Prefab, componentLookup, out area))
						{
							m_WaterSystem.TerrainWillChange();
							if (!heightMapRenderRequired)
							{
								heightMapRenderRequired = true;
								A = area;
							}
							else
							{
								A.xy = math.min(A.xy, area.xy);
								A.zw = math.max(A.zw, area.zw);
							}
						}
					}
				}
				nativeArray5.Dispose();
			}
			m_BuildingUpgrade.Clear();
		}
		if (isLoaded || !m_RoadsChanged.IsEmptyIgnoreFilter)
		{
			NativeArray<ArchetypeChunk> nativeArray8 = (isLoaded ? m_RoadsGroup : m_RoadsChanged).ToArchetypeChunkArray(Allocator.Temp);
			EntityTypeHandle entityTypeHandle2 = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabRef> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentLookup<NetData> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<NetGeometryData> componentLookup3 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<Composition> componentLookup4 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<Orphan> componentLookup5 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<NodeGeometry> componentLookup6 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<EdgeGeometry> componentLookup7 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<StartNodeGeometry> componentLookup8 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<EndNodeGeometry> componentLookup9 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef);
			CompleteDependency();
			for (int m = 0; m < nativeArray8.Length; m++)
			{
				NativeArray<Entity> nativeArray9 = nativeArray8[m].GetNativeArray(entityTypeHandle2);
				NativeArray<PrefabRef> nativeArray10 = nativeArray8[m].GetNativeArray(ref typeHandle4);
				if (isLoaded)
				{
					heightMapRenderRequired = true;
					A = m_CascadeRanges[baseLod];
					m_WaterSystem.TerrainWillChange();
					break;
				}
				for (int n = 0; n < nativeArray9.Length; n++)
				{
					Entity entity = nativeArray9[n];
					if (!componentLookup3.TryGetComponent(nativeArray10[n].m_Prefab, out var componentData) || (componentData.m_Flags & (Game.Net.GeometryFlags.FlattenTerrain | Game.Net.GeometryFlags.ClipTerrain)) == 0)
					{
						continue;
					}
					m_RoadUpdate = true;
					if ((componentData.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) == 0)
					{
						continue;
					}
					Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
					if (componentLookup4.HasComponent(entity))
					{
						EdgeGeometry edgeGeometry = componentLookup7[entity];
						StartNodeGeometry startNodeGeometry = componentLookup8[entity];
						EndNodeGeometry endNodeGeometry = componentLookup9[entity];
						if (math.any(edgeGeometry.m_Start.m_Length + edgeGeometry.m_End.m_Length > 0.1f))
						{
							bounds |= edgeGeometry.m_Bounds;
						}
						if (math.any(startNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(startNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
						{
							bounds |= startNodeGeometry.m_Geometry.m_Bounds;
						}
						if (math.any(endNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(endNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
						{
							bounds |= endNodeGeometry.m_Geometry.m_Bounds;
						}
					}
					else if (componentLookup5.HasComponent(entity))
					{
						bounds |= componentLookup6[entity].m_Bounds;
					}
					if (bounds.min.x <= bounds.max.x)
					{
						NetData netData = componentLookup2[nativeArray10[n].m_Prefab];
						bounds = MathUtils.Expand(bounds, NetUtils.GetTerrainSmoothingWidth(netData) - 8f);
						updateBuffer.Add(bounds.xz);
						m_WaterSystem.TerrainWillChange();
						if (!heightMapRenderRequired)
						{
							heightMapRenderRequired = true;
							A = new float4(bounds.min.xz, bounds.max.xz);
						}
						else
						{
							A.xy = math.min(A.xy, bounds.min.xz);
							A.zw = math.max(A.zw, bounds.max.xz);
						}
					}
				}
			}
			nativeArray8.Dispose();
		}
		bool num = isLoaded || !m_AreasChanged.IsEmptyIgnoreFilter;
		bool flag3 = isLoaded || heightsReadyAfterLoading;
		if (num)
		{
			NativeArray<ArchetypeChunk> nativeArray11 = (isLoaded ? m_AreasQuery : m_AreasChanged).ToArchetypeChunkArray(Allocator.Temp);
			ComponentTypeHandle<Game.Areas.Terrain> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Terrain_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Clip> typeHandle6 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Clip_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Geometry> typeHandle7 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			CompleteDependency();
			for (int num2 = 0; num2 < nativeArray11.Length; num2++)
			{
				flag3 |= nativeArray11[num2].Has(ref typeHandle6);
				if (!nativeArray11[num2].Has(ref typeHandle5))
				{
					continue;
				}
				m_AreaUpdate = true;
				NativeArray<Geometry> nativeArray12 = nativeArray11[num2].GetNativeArray(ref typeHandle7);
				if (isLoaded)
				{
					heightMapRenderRequired = true;
					A = m_CascadeRanges[baseLod];
					break;
				}
				for (int num3 = 0; num3 < nativeArray12.Length; num3++)
				{
					Bounds3 bounds2 = nativeArray12[num3].m_Bounds;
					if (bounds2.min.x <= bounds2.max.x)
					{
						updateBuffer.Add(bounds2.xz);
						if (!heightMapRenderRequired)
						{
							heightMapRenderRequired = true;
							A = new float4(bounds2.min.xz, bounds2.max.xz);
						}
						else
						{
							A.xy = math.min(A.xy, bounds2.min.xz);
							A.zw = math.max(A.zw, bounds2.max.xz);
						}
					}
				}
			}
			nativeArray11.Dispose();
		}
		if (heightMapRenderRequired)
		{
			A += new float4(-10f, -10f, 10f, 10f);
		}
		float4 area2 = A;
		for (int num4 = 0; num4 <= baseLod; num4++)
		{
			if (heightMapRenderRequired)
			{
				heightMapViewport[num4] = new float4(A.x - m_CascadeRanges[num4].x, A.y - m_CascadeRanges[num4].y, A.z - m_CascadeRanges[num4].x, A.w - m_CascadeRanges[num4].y);
				heightMapViewport[num4] /= new float4(m_CascadeRanges[num4].z - m_CascadeRanges[num4].x, m_CascadeRanges[num4].w - m_CascadeRanges[num4].y, m_CascadeRanges[num4].z - m_CascadeRanges[num4].x, m_CascadeRanges[num4].w - m_CascadeRanges[num4].y);
				heightMapViewport[num4].zw -= heightMapViewport[num4].xy;
				heightMapViewport[num4] = ClipViewport(heightMapViewport[num4]);
				heightMapSliceUpdated[num4] = heightMapViewport[num4].w > 0f && heightMapViewport[num4].z > 0f;
				area2.xy = math.min(area2.xy, m_CascadeRanges[num4].xy + heightMapViewport[num4].xy * (m_CascadeRanges[num4].zw - m_CascadeRanges[num4].xy));
				area2.zw = math.max(area2.zw, m_CascadeRanges[num4].xy + (heightMapViewport[num4].xy + heightMapViewport[num4].zw) * (m_CascadeRanges[num4].zw - m_CascadeRanges[num4].xy));
			}
			else
			{
				heightMapViewport[num4] = float4.zero;
				heightMapSliceUpdated[num4] = false;
			}
		}
		for (int num5 = baseLod + 1; num5 < 4; num5++)
		{
			float2 float2 = m_CascadeRanges[baseLod].zw - m_CascadeRanges[baseLod].xy;
			float2 /= math.pow(2f, num5 - baseLod);
			float num6 = math.min(float2.x, float2.y) / 4f;
			@float.xy = position.xz - float2 * 0.5f;
			@float.zw = position.xz + float2 * 0.5f;
			if (@float.x < m_CascadeRanges[0].x)
			{
				float num7 = m_CascadeRanges[0].x - @float.x;
				@float.x += num7;
				@float.z += num7;
			}
			if (@float.y < m_CascadeRanges[0].y)
			{
				float num8 = m_CascadeRanges[0].y - @float.y;
				@float.y += num8;
				@float.w += num8;
			}
			if (@float.z > m_CascadeRanges[0].z)
			{
				float num9 = m_CascadeRanges[0].z - @float.z;
				@float.x += num9;
				@float.z += num9;
			}
			if (@float.w > m_CascadeRanges[0].w)
			{
				float num10 = m_CascadeRanges[0].w - @float.w;
				@float.y += num10;
				@float.w += num10;
			}
			float2 float3 = math.abs(@float.xy - new float2(m_CascadeRanges[num5].x, m_CascadeRanges[num5].y));
			if (math.lengthsq(m_CascadeRanges[num5]) == 0f || float3.x > num6 || float3.y > num6)
			{
				heightMapSliceUpdated[num5] = true;
				heightMapViewport[num5] = new float4(0f, 0f, 1f, 1f);
				m_CascadeRanges[num5] = @float;
				if (heightMapRenderRequired)
				{
					A.xy = math.min(A.xy, m_CascadeRanges[num5].xy);
					A.zw = math.max(A.zw, m_CascadeRanges[num5].zw);
					area2.xy = math.min(area2.xy, m_CascadeRanges[num5].xy);
					area2.zw = math.max(area2.zw, m_CascadeRanges[num5].zw);
				}
				else
				{
					heightMapRenderRequired = true;
					A = m_CascadeRanges[num5];
					area2 = A;
				}
			}
			else if (math.lengthsq(A) > 0f && Overlap(ref A, ref m_CascadeRanges[num5]))
			{
				heightMapViewport[num5] = new float4(math.clamp(A.x, m_CascadeRanges[num5].x, m_CascadeRanges[num5].z) - m_CascadeRanges[num5].x, math.clamp(A.y, m_CascadeRanges[num5].y, m_CascadeRanges[num5].w) - m_CascadeRanges[num5].y, math.clamp(A.z, m_CascadeRanges[num5].x, m_CascadeRanges[num5].z) - m_CascadeRanges[num5].x, math.clamp(A.w, m_CascadeRanges[num5].y, m_CascadeRanges[num5].w) - m_CascadeRanges[num5].y);
				heightMapViewport[num5] /= new float4(m_CascadeRanges[num5].z - m_CascadeRanges[num5].x, m_CascadeRanges[num5].w - m_CascadeRanges[num5].y, m_CascadeRanges[num5].z - m_CascadeRanges[num5].x, m_CascadeRanges[num5].w - m_CascadeRanges[num5].y);
				heightMapViewport[num5].zw -= heightMapViewport[num5].xy;
				heightMapViewport[num5] = ClipViewport(heightMapViewport[num5]);
				heightMapSliceUpdated[num5] = heightMapViewport[num5].w > 0f && heightMapViewport[num5].z > 0f;
				area2.xy = math.min(area2.xy, m_CascadeRanges[num5].xy + heightMapViewport[num5].xy * (m_CascadeRanges[num5].zw - m_CascadeRanges[num5].xy));
				area2.zw = math.max(area2.zw, m_CascadeRanges[num5].xy + (heightMapViewport[num5].xy + heightMapViewport[num5].zw) * (m_CascadeRanges[num5].zw - m_CascadeRanges[num5].xy));
			}
			else
			{
				heightMapSliceUpdated[num5] = false;
				heightMapViewport[num5] = float4.zero;
			}
		}
		if (heightMapRenderRequired || m_RoadUpdate || flag3)
		{
			if (heightMapRenderRequired)
			{
				area2 += new float4(-10f, -10f, 10f, 10f);
				m_LastCullArea = area2;
				heightMapSliceUpdatedLast = heightMapSliceUpdated;
				heightMapViewportUpdated = heightMapViewport;
			}
			CullForCascades(area2, heightMapRenderRequired, m_RoadUpdate, m_AreaUpdate, flag3, out var laneCount);
			if (heightMapRenderRequired)
			{
				for (int num11 = 3; num11 >= baseLod; num11--)
				{
					if (heightMapSliceUpdated[num11])
					{
						CullCascade(num11, m_CascadeRanges[num11], heightMapViewport[num11], laneCount);
					}
					else
					{
						heightMapCullArea[num11] = float4.zero;
					}
				}
			}
			JobHandle.ScheduleBatchedJobs();
		}
		for (int num12 = 0; num12 < 4; num12++)
		{
			float4 float4 = m_CascadeRanges[num12];
			float4.zw = 1f / math.max(0.001f, float4.zw - float4.xy);
			float4.xy *= float4.zw;
			m_ShaderCascadeRanges[num12] = float4;
		}
		Shader.SetGlobalVectorArray(ShaderID._CascadeRangesID, m_ShaderCascadeRanges);
		m_WaterSystem.OnTerrainCascadesUpdated();
		m_CascadeReset = false;
	}

	public void RenderCascades()
	{
		if (heightMapRenderRequired)
		{
			m_GroundHeightSystem.BeforeUpdateHeights();
			m_CascadeCB.Clear();
			m_BuildingInstanceData.StartFrame();
			m_LaneInstanceData.StartFrame();
			m_LaneRaisedInstanceData.StartFrame();
			m_TriangleInstanceData.StartFrame();
			m_EdgeInstanceData.StartFrame();
			if (baseLod != 0)
			{
				Texture value = ((m_WorldMapEditable != null) ? m_WorldMapEditable : worldHeightmap);
				m_TerrainBlit.SetTexture("_WorldMap", value);
			}
			for (int num = 3; num >= baseLod; num--)
			{
				if (heightMapSliceUpdated[num])
				{
					RenderCascade(num, m_CascadeRanges[num], heightMapViewport[num], ref m_CascadeCB, m_HeightmapCascade);
				}
			}
			if (baseLod > 0 && heightMapSliceUpdated[0])
			{
				int4 lastWorldWrite = new int4((int)(heightMapViewport[0].x * (float)m_HeightmapCascade.width), (int)(heightMapViewport[0].y * (float)m_HeightmapCascade.height), (int)(heightMapViewport[0].z * (float)m_HeightmapCascade.width), (int)(heightMapViewport[0].w * (float)m_HeightmapCascade.height));
				if (m_LastWorldWrite.z == 0)
				{
					m_LastWorldWrite = lastWorldWrite;
				}
				else
				{
					int2 @int = new int2(math.min(m_LastWorldWrite.x, lastWorldWrite.x), math.min(m_LastWorldWrite.y, lastWorldWrite.y));
					int2 int2 = new int2(math.max(m_LastWorldWrite.x + m_LastWorldWrite.z, lastWorldWrite.x + lastWorldWrite.z), math.max(m_LastWorldWrite.y + m_LastWorldWrite.w, lastWorldWrite.y + lastWorldWrite.w));
					m_LastWorldWrite.xy = @int;
					m_LastWorldWrite.zw = int2 - @int;
				}
				RenderWorldMapToCascade(m_CascadeRanges[0], heightMapViewport[0], ref m_CascadeCB);
			}
			m_BuildingInstanceData.EndFrame();
			m_LaneInstanceData.EndFrame();
			m_LaneRaisedInstanceData.EndFrame();
			m_TriangleInstanceData.EndFrame();
			m_EdgeInstanceData.EndFrame();
			if (doCapture)
			{
				ExternalGPUProfiler.BeginGPUCapture();
			}
			Graphics.ExecuteCommandBuffer(m_CascadeCB);
			if (doCapture)
			{
				ExternalGPUProfiler.EndGPUCapture();
				doCapture = false;
			}
			if (heightMapSliceUpdated[baseLod])
			{
				int4 lastWrite = new int4((int)(heightMapViewport[baseLod].x * (float)m_HeightmapCascade.width), (int)(heightMapViewport[baseLod].y * (float)m_HeightmapCascade.height), (int)(heightMapViewport[baseLod].z * (float)m_HeightmapCascade.width), (int)(heightMapViewport[baseLod].w * (float)m_HeightmapCascade.height));
				if (m_LastWrite.z == 0)
				{
					m_LastWrite = lastWrite;
				}
				else
				{
					int2 int3 = new int2(math.min(m_LastWrite.x, lastWrite.x), math.min(m_LastWrite.y, lastWrite.y));
					int2 int4 = new int2(math.max(m_LastWrite.x + m_LastWrite.z, lastWrite.x + lastWrite.z), math.max(m_LastWrite.y + m_LastWrite.w, lastWrite.y + lastWrite.w));
					m_LastWrite.xy = int3;
					m_LastWrite.zw = int4 - int3;
				}
				TriggerAsyncChange();
			}
		}
		m_CascadeReset = false;
	}

	private void CullForCascades(float4 area, bool heightMapRenderRequired, bool roadsChanged, bool terrainAreasChanged, bool clipAreasChanged, out int laneCount)
	{
		m_CullFinished.Complete();
		if (roadsChanged)
		{
			m_ClipMapCull.Complete();
			m_LaneCullList.Clear();
			m_ClipMapList.Clear();
			laneCount = m_RoadsGroup.CalculateEntityCountWithoutFiltering() * 6;
			if (laneCount > m_LaneCullList.Capacity)
			{
				m_LaneCullList.Capacity = laneCount + math.max(laneCount / 4, 250);
				m_ClipMapList.Capacity = m_LaneCullList.Capacity;
			}
		}
		else
		{
			laneCount = m_LaneCullList.Length;
		}
		if (heightMapRenderRequired)
		{
			NativeQueue<BuildingUtils.LotInfo> queue = new NativeQueue<BuildingUtils.LotInfo>(Allocator.TempJob);
			CullBuildingLotsJob jobData = new CullBuildingLotsJob
			{
				m_LotHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Lot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ElevationHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_StackHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InstalledUpgradeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAssetStampData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OverrideTerraform = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AdditionalLots = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Area = area,
				Result = queue.AsParallelWriter()
			};
			DequeBuildingLotsJob jobData2 = new DequeBuildingLotsJob
			{
				m_Queue = queue,
				m_List = m_BuildingCullList
			};
			JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingGroup, base.Dependency);
			m_BuildingCull = IJobExtensions.Schedule(jobData2, dependsOn);
			m_CullFinished = m_BuildingCull;
			queue.Dispose(m_BuildingCull);
		}
		if (roadsChanged)
		{
			CullRoadsJob jobData3 = new CullRoadsJob
			{
				m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TerrainCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TerrainComposition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Area = m_CascadeRanges[baseLod],
				Result = m_LaneCullList.AsParallelWriter()
			};
			m_LaneCull = JobChunkExtensions.ScheduleParallel(jobData3, m_RoadsGroup, base.Dependency);
			m_CullFinished = JobHandle.CombineDependencies(m_CullFinished, m_LaneCull);
			GenerateClipDataJob jobData4 = new GenerateClipDataJob
			{
				m_RoadsToCull = m_LaneCullList,
				Result = m_ClipMapList.AsParallelWriter()
			};
			m_CurrentClipMap = null;
			m_ClipMapCull = jobData4.Schedule(m_LaneCullList, 128, m_LaneCull);
			m_CullFinished = JobHandle.CombineDependencies(m_CullFinished, m_ClipMapCull);
		}
		if (terrainAreasChanged)
		{
			NativeQueue<AreaTriangle> queue2 = new NativeQueue<AreaTriangle>(Allocator.TempJob);
			NativeQueue<AreaEdge> queue3 = new NativeQueue<AreaEdge>(Allocator.TempJob);
			CullAreasJob jobData5 = new CullAreasJob
			{
				m_ClipType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Clip_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_GeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_StorageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Storage_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PrefabTerrainAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TerrainAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabStorageAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Area = m_CascadeRanges[baseLod],
				m_Triangles = queue2.AsParallelWriter(),
				m_Edges = queue3.AsParallelWriter()
			};
			DequeTrianglesJob jobData6 = new DequeTrianglesJob
			{
				m_Queue = queue2,
				m_List = m_TriangleCullList
			};
			DequeEdgesJob jobData7 = new DequeEdgesJob
			{
				m_Queue = queue3,
				m_List = m_EdgeCullList
			};
			JobHandle dependsOn2 = JobChunkExtensions.ScheduleParallel(jobData5, m_AreasQuery, base.Dependency);
			JobHandle job = IJobExtensions.Schedule(jobData6, dependsOn2);
			JobHandle job2 = IJobExtensions.Schedule(jobData7, dependsOn2);
			m_AreaCull = JobHandle.CombineDependencies(job, job2);
			m_CullFinished = JobHandle.CombineDependencies(m_CullFinished, m_AreaCull);
			queue2.Dispose(m_AreaCull);
			queue3.Dispose(m_AreaCull);
		}
		if (clipAreasChanged)
		{
			if (!m_HasAreaClipMeshData)
			{
				m_HasAreaClipMeshData = true;
				m_AreaClipMeshData = Mesh.AllocateWritableMeshData(1);
			}
			JobHandle outJobHandle;
			GenerateAreaClipMeshJob jobData8 = new GenerateAreaClipMeshJob
			{
				m_Chunks = m_AreasQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_ClipType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Clip_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_MeshData = m_AreaClipMeshData
			};
			m_AreaClipMeshDataDeps = IJobExtensions.Schedule(jobData8, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
			jobData8.m_Chunks.Dispose(m_AreaClipMeshDataDeps);
			m_CullFinished = JobHandle.CombineDependencies(m_CullFinished, m_AreaClipMeshDataDeps);
		}
		base.Dependency = m_CullFinished;
	}

	public void CullClipMapForView(Viewer viewer)
	{
	}

	private void CullCascade(int cascadeIndex, float4 area, float4 viewport, int laneCount)
	{
		if (viewport.z == 0f || viewport.w == 0f)
		{
			UnityEngine.Debug.LogError("Invalid Viewport");
		}
		CascadeCullInfo cascadeCullInfo = m_CascadeCulling[cascadeIndex];
		cascadeCullInfo.m_BuildingHandle.Complete();
		cascadeCullInfo.m_BuildingRenderList = new NativeList<BuildingLotDraw>(Allocator.TempJob);
		float2 xy = area.xy;
		float2 @float = area.zw - area.xy;
		area = new float4(xy.x + @float.x * viewport.x, xy.y + @float.y * viewport.y, xy.x + @float.x * (viewport.x + viewport.z), xy.y + @float.y * (viewport.y + viewport.w));
		area += new float4(-10f, -10f, 10f, 10f);
		heightMapCullArea[cascadeIndex] = area;
		NativeQueue<BuildingLotDraw> queue = new NativeQueue<BuildingLotDraw>(Allocator.TempJob);
		CullBuildingsCascadeJob jobData = new CullBuildingsCascadeJob
		{
			m_LotsToCull = m_BuildingCullList,
			m_Area = area,
			Result = queue.AsParallelWriter()
		};
		DequeBuildingDrawsJob jobData2 = new DequeBuildingDrawsJob
		{
			m_Queue = queue,
			m_List = cascadeCullInfo.m_BuildingRenderList
		};
		JobHandle dependsOn = jobData.Schedule(m_BuildingCullList, 128, m_BuildingCull);
		cascadeCullInfo.m_BuildingHandle = IJobExtensions.Schedule(jobData2, dependsOn);
		queue.Dispose(cascadeCullInfo.m_BuildingHandle);
		cascadeCullInfo.m_LaneHandle.Complete();
		cascadeCullInfo.m_LaneRenderList = new NativeList<LaneDraw>(laneCount, Allocator.TempJob);
		cascadeCullInfo.m_LaneRaisedRenderList = new NativeList<LaneDraw>(laneCount, Allocator.TempJob);
		CullRoadsCacscadeJob jobData3 = new CullRoadsCacscadeJob
		{
			m_RoadsToCull = m_LaneCullList,
			m_Area = area,
			m_Scale = 1f / heightScaleOffset.x,
			Result = cascadeCullInfo.m_LaneRenderList.AsParallelWriter(),
			ResultRaised = cascadeCullInfo.m_LaneRaisedRenderList.AsParallelWriter(),
			m_addRaised = (cascadeIndex == 1 && !m_WaterSystem.UseLegacyWaterSources)
		};
		cascadeCullInfo.m_LaneHandle = jobData3.Schedule(m_LaneCullList, 128, m_LaneCull);
		cascadeCullInfo.m_AreaHandle.Complete();
		cascadeCullInfo.m_TriangleRenderList = new NativeList<AreaTriangle>(Allocator.TempJob);
		cascadeCullInfo.m_EdgeRenderList = new NativeList<AreaEdge>(Allocator.TempJob);
		CullTrianglesJob jobData4 = new CullTrianglesJob
		{
			m_Triangles = m_TriangleCullList,
			m_Area = area,
			Result = cascadeCullInfo.m_TriangleRenderList
		};
		CullEdgesJob jobData5 = new CullEdgesJob
		{
			m_Edges = m_EdgeCullList,
			m_Area = area,
			Result = cascadeCullInfo.m_EdgeRenderList
		};
		JobHandle job = IJobExtensions.Schedule(jobData4, m_AreaCull);
		JobHandle job2 = IJobExtensions.Schedule(jobData5, m_AreaCull);
		cascadeCullInfo.m_AreaHandle = JobHandle.CombineDependencies(job, job2);
		m_CullFinished = JobHandle.CombineDependencies(m_CullFinished, JobHandle.CombineDependencies(cascadeCullInfo.m_BuildingHandle, cascadeCullInfo.m_LaneHandle, cascadeCullInfo.m_AreaHandle));
	}

	private void DrawHeightLaneRaised(ref CommandBuffer cmdBuffer, int cascade, float4 area, float4 viewport, ref NativeArray<LaneDraw> lanes, ref Material laneMaterial)
	{
		float4 @float = new float4(-area.xy, 1f / (area.zw - area.xy));
		Rect rect = new Rect(viewport.x * (float)m_HeightmapCascade.width, viewport.y * (float)m_HeightmapCascade.height, viewport.z * (float)m_HeightmapCascade.width, viewport.w * (float)m_HeightmapCascade.height);
		cmdBuffer.SetRenderTarget(m_HeightmapObjectsLayer);
		cmdBuffer.EnableScissorRect(rect);
		cmdBuffer.SetViewport(rect);
		cmdBuffer.ClearRenderTarget(clearDepth: false, clearColor: true, UnityEngine.Color.black);
		cmdBuffer.SetViewport(new Rect(0f, 0f, m_HeightmapObjectsLayer.width, m_HeightmapObjectsLayer.height));
		if (lanes.Length > 0)
		{
			ComputeBuffer computeBuffer = m_LaneRaisedInstanceData.Request(lanes.Length);
			computeBuffer.SetData(lanes);
			computeBuffer.name = $"Lane Buffer Cascade{cascade}";
			laneMaterial.SetVector(ShaderID._TerrainScaleOffsetID, new Vector4(heightScaleOffset.x, heightScaleOffset.y, 0f, 0f));
			laneMaterial.SetVector(ShaderID._MapOffsetScaleID, m_MapOffsetScale);
			laneMaterial.SetVector(ShaderID._CascadeOffsetScale, @float);
			laneMaterial.SetTexture(ShaderID._HeightmapID, heightmap);
			laneMaterial.SetBuffer(ShaderID._LanesID, computeBuffer);
			cmdBuffer.DrawMeshInstancedProcedural(m_LaneMesh, 0, laneMaterial, 3, lanes.Length);
		}
		if (viewport.z != 1f && viewport.w != 1f)
		{
			m_WaterSystem.UpdateWaterArea(rect);
		}
	}

	private void DrawHeightAdjustments(ref CommandBuffer cmdBuffer, int cascade, float4 area, float4 viewport, RenderTargetBinding binding, ref NativeArray<BuildingLotDraw> lots, ref NativeArray<LaneDraw> lanes, ref NativeArray<AreaTriangle> triangles, ref NativeArray<AreaEdge> edges, ref Material lotMaterial, ref Material laneMaterial, ref Material areaMaterial)
	{
		float4 @float = new float4(-area.xy, 1f / (area.zw - area.xy));
		Rect scissor = new Rect(viewport.x * (float)m_HeightmapCascade.width, viewport.y * (float)m_HeightmapCascade.height, viewport.z * (float)m_HeightmapCascade.width, viewport.w * (float)m_HeightmapCascade.height);
		if (lots.Length > 0)
		{
			ComputeBuffer computeBuffer = m_BuildingInstanceData.Request(lots.Length);
			computeBuffer.SetData(lots);
			computeBuffer.name = $"BuildingLot Buffer Cascade{cascade}";
			lotMaterial.SetVector(ShaderID._TerrainScaleOffsetID, new Vector4(heightScaleOffset.x, heightScaleOffset.y, 0f, 0f));
			lotMaterial.SetVector(ShaderID._MapOffsetScaleID, m_MapOffsetScale);
			lotMaterial.SetVector(ShaderID._CascadeOffsetScale, @float);
			lotMaterial.SetTexture(ShaderID._HeightmapID, heightmap);
			lotMaterial.SetBuffer(ShaderID._BuildingLotID, computeBuffer);
		}
		if (lanes.Length > 0)
		{
			ComputeBuffer computeBuffer2 = m_LaneInstanceData.Request(lanes.Length);
			computeBuffer2.SetData(lanes);
			computeBuffer2.name = $"Lane Buffer Cascade{cascade}";
			laneMaterial.SetVector(ShaderID._TerrainScaleOffsetID, new Vector4(heightScaleOffset.x, heightScaleOffset.y, 0f, 0f));
			laneMaterial.SetVector(ShaderID._MapOffsetScaleID, m_MapOffsetScale);
			laneMaterial.SetVector(ShaderID._CascadeOffsetScale, @float);
			laneMaterial.SetTexture(ShaderID._HeightmapID, heightmap);
			laneMaterial.SetBuffer(ShaderID._LanesID, computeBuffer2);
		}
		if (triangles.Length > 0 || edges.Length > 0)
		{
			ComputeBuffer computeBuffer3 = m_TriangleInstanceData.Request(triangles.Length);
			computeBuffer3.SetData(triangles);
			computeBuffer3.name = $"Triangle Buffer Cascade{cascade}";
			ComputeBuffer computeBuffer4 = m_EdgeInstanceData.Request(edges.Length);
			computeBuffer4.SetData(edges);
			computeBuffer4.name = $"Edge Buffer Cascade{cascade}";
			areaMaterial.SetVector(ShaderID._TerrainScaleOffsetID, new Vector4(heightScaleOffset.x, heightScaleOffset.y, 0f, 0f));
			areaMaterial.SetVector(ShaderID._MapOffsetScaleID, m_MapOffsetScale);
			areaMaterial.SetVector(ShaderID._CascadeOffsetScale, @float);
			areaMaterial.SetTexture(ShaderID._HeightmapID, heightmap);
			areaMaterial.SetBuffer(ShaderID._TrianglesID, computeBuffer3);
			areaMaterial.SetBuffer(ShaderID._EdgesID, computeBuffer4);
		}
		if (lots.Length > 0)
		{
			cmdBuffer.DrawProcedural(Matrix4x4.identity, lotMaterial, 1, MeshTopology.Triangles, 6, lots.Length);
		}
		if (lanes.Length > 0)
		{
			cmdBuffer.DrawMeshInstancedProcedural(m_LaneMesh, 0, laneMaterial, 1, lanes.Length);
		}
		int num = Shader.PropertyToID("_CascadeMinHeights");
		cmdBuffer.GetTemporaryRT(num, m_HeightmapCascade.width, m_HeightmapCascade.height, 0, FilterMode.Point, m_HeightmapCascade.graphicsFormat);
		int num2 = math.max(0, Mathf.FloorToInt(scissor.xMin));
		int num3 = math.max(0, Mathf.FloorToInt(scissor.yMin));
		int srcWidth = math.min(m_HeightmapCascade.width, Mathf.CeilToInt(scissor.xMax)) - num2;
		int srcHeight = math.min(m_HeightmapCascade.height, Mathf.CeilToInt(scissor.yMax)) - num3;
		cmdBuffer.CopyTexture(m_HeightmapCascade, cascade, 0, num2, num3, srcWidth, srcHeight, num, 0, 0, num2, num3);
		cmdBuffer.SetRenderTarget(binding, 0, CubemapFace.Unknown, cascade);
		cmdBuffer.EnableScissorRect(scissor);
		if (triangles.Length > 0)
		{
			cmdBuffer.DrawProcedural(Matrix4x4.identity, areaMaterial, 0, MeshTopology.Triangles, 3, triangles.Length);
		}
		if (edges.Length > 0)
		{
			cmdBuffer.DrawProcedural(Matrix4x4.identity, areaMaterial, 1, MeshTopology.Triangles, 6, edges.Length);
		}
		if (lots.Length > 0)
		{
			cmdBuffer.DrawProcedural(Matrix4x4.identity, lotMaterial, 0, MeshTopology.Triangles, 6, lots.Length);
		}
		if (lanes.Length > 0)
		{
			cmdBuffer.DrawMeshInstancedProcedural(m_LaneMesh, 0, laneMaterial, 0, lanes.Length);
		}
		cmdBuffer.ReleaseTemporaryRT(num);
		if (lots.Length > 0)
		{
			cmdBuffer.DrawProcedural(Matrix4x4.identity, lotMaterial, 2, MeshTopology.Triangles, 6, lots.Length);
		}
		if (lanes.Length > 0)
		{
			cmdBuffer.DrawMeshInstancedProcedural(m_LaneMesh, 0, laneMaterial, 2, lanes.Length);
		}
	}

	private void RenderWorldMapToCascade(float4 area, float4 viewport, ref CommandBuffer cmdBuffer)
	{
		if (m_WorldMapEditable != null)
		{
			bool flag = viewport.x == 0f && viewport.y == 0f && viewport.z == 1f && viewport.w == 1f;
			Texture source = m_WorldMapEditable;
			Rect scissor = new Rect(viewport.x * (float)m_HeightmapCascade.width, viewport.y * (float)m_HeightmapCascade.height, viewport.z * (float)m_HeightmapCascade.width, viewport.w * (float)m_HeightmapCascade.height);
			RenderTargetBinding binding = new RenderTargetBinding(m_HeightmapCascade, flag ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, m_HeightmapDepth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
			cmdBuffer.SetRenderTarget(binding, 0, CubemapFace.Unknown, 0);
			cmdBuffer.ClearRenderTarget(clearDepth: true, clearColor: false, UnityEngine.Color.black, 1f);
			cmdBuffer.EnableScissorRect(scissor);
			Vector2 scale = new Vector2(1f, 1f);
			Vector2 offset = new Vector2
			{
				x = (area.x - worldOffset.x) / worldSize.x,
				y = (area.y - worldOffset.y) / worldSize.y
			};
			cmdBuffer.Blit(source, BuiltinRenderTextureType.CurrentActive, scale, offset);
		}
	}

	private void RenderCascade(int cascadeIndex, float4 area, float4 viewport, ref CommandBuffer cmdBuffer, RenderTexture rtTarget)
	{
		bool flag = true;
		bool num = viewport.x == 0f && viewport.y == 0f && viewport.z == 1f && viewport.w == 1f;
		Rect scissor = new Rect(viewport.x * (float)m_HeightmapCascade.width, viewport.y * (float)m_HeightmapCascade.height, viewport.z * (float)m_HeightmapCascade.width, viewport.w * (float)m_HeightmapCascade.height);
		RenderBufferLoadAction colorLoadAction = ((num && flag) ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load);
		RenderTargetBinding binding = new RenderTargetBinding(m_HeightmapCascade, colorLoadAction, RenderBufferStoreAction.Store, m_HeightmapDepth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
		cmdBuffer.SetRenderTarget(binding, 0, CubemapFace.Unknown, cascadeIndex);
		cmdBuffer.ClearRenderTarget(clearDepth: true, clearColor: false, UnityEngine.Color.black, 1f);
		cmdBuffer.EnableScissorRect(scissor);
		if (flag)
		{
			float num2 = ((cascadeIndex < baseLod) ? math.pow(2f, -(cascadeIndex - baseLod)) : (1f / math.pow(2f, cascadeIndex - baseLod)));
			float2 @float = new Vector2(num2, num2);
			float2 float2 = (area.xy - playableOffset) / playableArea;
			if (cascadeIndex == baseLod || baseLod == 0)
			{
				cmdBuffer.Blit(heightmap, BuiltinRenderTextureType.CurrentActive, @float, float2);
			}
			else
			{
				cmdBuffer.SetGlobalVector("_CascadeHeightmapOffsetScale", new float4(float2, @float));
				@float = (area.zw - area.xy) / worldSize;
				float2 = (area.xy - worldOffset) / worldSize;
				cmdBuffer.SetGlobalVector("_CascadeWorldOffsetScale", new float4(float2, @float));
				cmdBuffer.Blit(heightmap, BuiltinRenderTextureType.CurrentActive, m_TerrainBlit);
			}
		}
		Matrix4x4 proj = Matrix4x4.Ortho(area.x, area.z, area.w, area.y, heightScaleOffset.x + heightScaleOffset.y, heightScaleOffset.y);
		proj.m02 *= -1f;
		proj.m12 *= -1f;
		proj.m22 *= -1f;
		proj.m32 *= -1f;
		cmdBuffer.SetViewProjectionMatrices(GL.GetGPUProjectionMatrix(proj, renderIntoTexture: true), Matrix4x4.identity);
		CascadeCullInfo cascadeCullInfo = m_CascadeCulling[cascadeIndex];
		cascadeCullInfo.m_BuildingHandle.Complete();
		cascadeCullInfo.m_LaneHandle.Complete();
		cascadeCullInfo.m_AreaHandle.Complete();
		if (cascadeCullInfo.m_BuildingRenderList.IsCreated || cascadeCullInfo.m_LaneRenderList.IsCreated || cascadeCullInfo.m_TriangleRenderList.IsCreated || cascadeCullInfo.m_EdgeRenderList.IsCreated || cascadeCullInfo.m_LaneRaisedRenderList.IsCreated)
		{
			NativeArray<BuildingLotDraw> lots = default(NativeArray<BuildingLotDraw>);
			NativeArray<LaneDraw> lanes = default(NativeArray<LaneDraw>);
			NativeArray<LaneDraw> lanes2 = default(NativeArray<LaneDraw>);
			NativeArray<AreaTriangle> triangles = default(NativeArray<AreaTriangle>);
			NativeArray<AreaEdge> edges = default(NativeArray<AreaEdge>);
			if (cascadeCullInfo.m_BuildingRenderList.IsCreated)
			{
				lots = cascadeCullInfo.m_BuildingRenderList.AsArray();
			}
			if (cascadeCullInfo.m_LaneRenderList.IsCreated)
			{
				lanes = cascadeCullInfo.m_LaneRenderList.AsArray();
			}
			if (cascadeCullInfo.m_LaneRaisedRenderList.IsCreated)
			{
				lanes2 = cascadeCullInfo.m_LaneRaisedRenderList.AsArray();
			}
			if (cascadeCullInfo.m_TriangleRenderList.IsCreated)
			{
				triangles = cascadeCullInfo.m_TriangleRenderList.AsArray();
			}
			if (cascadeCullInfo.m_EdgeRenderList.IsCreated)
			{
				edges = cascadeCullInfo.m_EdgeRenderList.AsArray();
			}
			DrawHeightAdjustments(ref cmdBuffer, cascadeIndex, area, viewport, binding, ref lots, ref lanes, ref triangles, ref edges, ref cascadeCullInfo.m_LotMaterial, ref cascadeCullInfo.m_LaneMaterial, ref cascadeCullInfo.m_AreaMaterial);
			if (cascadeIndex == baseLod && !m_WaterSystem.UseLegacyWaterSources)
			{
				DrawHeightLaneRaised(ref cmdBuffer, cascadeIndex, area, viewport, ref lanes2, ref cascadeCullInfo.m_LaneRaisedMaterial);
			}
			if (cascadeCullInfo.m_BuildingRenderList.IsCreated)
			{
				cascadeCullInfo.m_BuildingRenderList.Dispose();
			}
			if (cascadeCullInfo.m_LaneRenderList.IsCreated)
			{
				cascadeCullInfo.m_LaneRenderList.Dispose();
			}
			if (cascadeCullInfo.m_LaneRaisedRenderList.IsCreated)
			{
				cascadeCullInfo.m_LaneRaisedRenderList.Dispose();
			}
			if (cascadeCullInfo.m_TriangleRenderList.IsCreated)
			{
				cascadeCullInfo.m_TriangleRenderList.Dispose();
			}
			if (cascadeCullInfo.m_EdgeRenderList.IsCreated)
			{
				cascadeCullInfo.m_EdgeRenderList.Dispose();
			}
		}
		cmdBuffer.DisableScissorRect();
	}

	private void CreateRoadMeshes()
	{
		m_LaneMesh = new Mesh
		{
			name = "Lane Mesh"
		};
		int num = 1;
		int num2 = 8;
		int num3 = (num + 1) * (num2 + 1);
		int num4 = num * num2 * 2 * 3;
		Vector3[] array = new Vector3[num3];
		Vector2[] array2 = new Vector2[num3];
		int[] array3 = new int[num4];
		for (int i = 0; i <= num2; i++)
		{
			for (int j = 0; j <= num; j++)
			{
				array[j + (num + 1) * i] = new Vector3((float)j / (float)num, 0f, (float)i / (float)num2);
				array2[j + (num + 1) * i] = new Vector2(array[j + (num + 1) * i].x, array[j + (num + 1) * i].z);
			}
		}
		int num5 = num + 1;
		int num6 = 0;
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < num; l++)
			{
				array3[num6++] = l + num5 * (k + 1);
				array3[num6++] = l + 1 + num5 * (k + 1);
				array3[num6++] = l + 1 + num5 * k;
				array3[num6++] = l + num5 * (k + 1);
				array3[num6++] = l + 1 + num5 * k;
				array3[num6++] = l + num5 * k;
			}
		}
		m_LaneMesh.vertices = array;
		m_LaneMesh.uv = array2;
		m_LaneMesh.subMeshCount = 1;
		m_LaneMesh.SetTriangles(array3, 0);
		m_LaneMesh.UploadMeshData(markNoLongerReadable: true);
		m_ClipMesh = new Mesh
		{
			name = "Clip Mesh"
		};
		int num7 = num3;
		num3 *= 2;
		num4 = num4 * 2 + num2 * 2 * 3 * 2 + num * 2 * 3 * 2;
		array = new Vector3[num3];
		array2 = new Vector2[num3];
		array3 = new int[num4];
		for (int m = 0; m <= num2; m++)
		{
			for (int n = 0; n <= num; n++)
			{
				array[n + (num + 1) * m] = new Vector3((float)n / (float)num, 1f, (float)m / (float)num2);
				array2[n + (num + 1) * m] = new Vector2(array[n + (num + 1) * m].x, array[n + (num + 1) * m].z);
				array[num7 + n + (num + 1) * m] = array[n + (num + 1) * m];
				array[num7 + n + (num + 1) * m].y = 0f;
				array2[num7 + n + (num + 1) * m] = array2[n + (num + 1) * m];
			}
		}
		num5 = num + 1;
		num6 = 0;
		for (int num8 = 0; num8 < num2; num8++)
		{
			for (int num9 = 0; num9 < num; num9++)
			{
				array3[num6++] = num9 + num5 * (num8 + 1);
				array3[num6++] = num9 + 1 + num5 * (num8 + 1);
				array3[num6++] = num9 + 1 + num5 * num8;
				array3[num6++] = num9 + num5 * (num8 + 1);
				array3[num6++] = num9 + 1 + num5 * num8;
				array3[num6++] = num9 + num5 * num8;
			}
		}
		for (int num10 = 0; num10 < num2; num10++)
		{
			for (int num11 = 0; num11 < num; num11++)
			{
				array3[num6++] = num7 + (num11 + 1 + num5 * (num10 + 1));
				array3[num6++] = num7 + (num11 + num5 * (num10 + 1));
				array3[num6++] = num7 + (num11 + 1 + num5 * num10);
				array3[num6++] = num7 + (num11 + 1 + num5 * num10);
				array3[num6++] = num7 + (num11 + num5 * (num10 + 1));
				array3[num6++] = num7 + (num11 + num5 * num10);
			}
		}
		int num12 = 0;
		for (int num13 = 0; num13 < num2; num13++)
		{
			array3[num6++] = num12 + num5 * (num13 + 1);
			array3[num6++] = num12 + num5 * num13;
			array3[num6++] = num7 + num12 + num5 * num13;
			array3[num6++] = num7 + num12 + num5 * num13;
			array3[num6++] = num7 + num12 + num5 * (num13 + 1);
			array3[num6++] = num12 + num5 * (num13 + 1);
		}
		num12 = num;
		for (int num14 = 0; num14 < num2; num14++)
		{
			array3[num6++] = num12 + num5 * num14;
			array3[num6++] = num12 + num5 * (num14 + 1);
			array3[num6++] = num7 + num12 + num5 * num14;
			array3[num6++] = num7 + num12 + num5 * (num14 + 1);
			array3[num6++] = num7 + num12 + num5 * num14;
			array3[num6++] = num12 + num5 * (num14 + 1);
		}
		for (int num15 = 0; num15 < num; num15++)
		{
			array3[num6++] = num15;
			array3[num6++] = num15 + num7;
			array3[num6++] = num15 + num7 + 1;
			array3[num6++] = num15 + num7 + 1;
			array3[num6++] = num15 + 1;
			array3[num6++] = num15;
		}
		for (int num16 = 1; num16 <= num; num16++)
		{
			array3[num6++] = num3 - num16;
			array3[num6++] = num3 - num16 - 1;
			array3[num6++] = num3 - num16 - num7 - 1;
			array3[num6++] = num3 - num16 - num7 - 1;
			array3[num6++] = num3 - num16 - num7;
			array3[num6++] = num3 - num16;
		}
		m_ClipMesh.vertices = array;
		m_ClipMesh.uv = array2;
		m_ClipMesh.subMeshCount = 1;
		m_ClipMesh.SetTriangles(array3, 0);
		m_ClipMesh.UploadMeshData(markNoLongerReadable: true);
	}

	public bool CalculateBuildingCullArea(Game.Objects.Transform transform, Entity prefab, ComponentLookup<ObjectGeometryData> geometryData, out float4 area)
	{
		area = float4.zero;
		if (geometryData.TryGetComponent(prefab, out var componentData))
		{
			Bounds3 bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, componentData);
			bounds = MathUtils.Expand(bounds, ObjectUtils.GetTerrainSmoothingWidth(componentData) - 8f);
			area.xy = bounds.min.xz;
			area.zw = bounds.max.xz;
			return true;
		}
		return false;
	}

	public void OnBuildingMoved(Entity entity)
	{
		ComponentLookup<Game.Objects.Transform> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PrefabRef> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ObjectGeometryData> componentLookup3 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef);
		CompleteDependency();
		float4 area = float4.zero;
		if (!componentLookup2.HasComponent(entity) || !componentLookup.HasComponent(entity))
		{
			return;
		}
		PrefabRef prefabRef = componentLookup2[entity];
		Game.Objects.Transform transform = componentLookup[entity];
		if (CalculateBuildingCullArea(transform, prefabRef.m_Prefab, componentLookup3, out area))
		{
			m_GroundHeightSystem.GetUpdateBuffer().Add(new Bounds2(area.xy, area.zw));
			if (math.lengthsq(m_UpdateArea) > 0f)
			{
				m_UpdateArea.xy = math.min(m_UpdateArea.xy, area.xy);
				m_UpdateArea.zw = math.max(m_UpdateArea.zw, area.zw);
			}
			else
			{
				m_UpdateArea = area;
			}
			m_UpdateArea += new float4(-10f, -10f, 10f, 10f);
		}
	}

	public void GetLastMinMaxUpdate(out float3 min, out float3 max)
	{
		int4 updateArea = m_TerrainMinMax.UpdateArea;
		float2 minMax = m_TerrainMinMax.GetMinMax(updateArea);
		float4 @float = new float4((float)updateArea.x / (float)m_TerrainMinMax.size, (float)updateArea.y / (float)m_TerrainMinMax.size, (float)(updateArea.x + updateArea.z) / (float)m_TerrainMinMax.size, (float)(updateArea.y + updateArea.w) / (float)m_TerrainMinMax.size);
		@float *= worldSize.xyxy;
		@float += worldOffset.xyxy;
		min = new float3(@float.x, minMax.x, @float.y);
		max = new float3(@float.z, minMax.y, @float.w);
	}

	public NativeParallelHashMap<Entity, Entity>.ParallelWriter GetBuildingUpgradeWriter(int ExpectedAmount)
	{
		m_BuildingUpgradeDependencies.Complete();
		if (ExpectedAmount > m_BuildingUpgrade.Capacity)
		{
			m_BuildingUpgrade.Capacity = ExpectedAmount;
		}
		return m_BuildingUpgrade.AsParallelWriter();
	}

	public void SetBuildingUpgradeWriterDependency(JobHandle handle)
	{
		m_BuildingUpgradeDependencies = handle;
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
	public TerrainSystem()
	{
	}
}
