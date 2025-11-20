using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Simulation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Rendering;

[FormerlySerializedAs("Colossal.Terrain.TerrainRenderSystem, Game")]
public class TerrainRenderSystem : GameSystemBase
{
	public class ShaderID
	{
		public static readonly int _COTerrainTextureArrayLODArea = Shader.PropertyToID("colossal_TerrainTextureArrayLODArea");

		public static readonly int _COTerrainTextureArrayLODRange = Shader.PropertyToID("colossal_TerrainTextureArrayLODRange");

		public static readonly int _COTerrainTextureArrayBaseLod = Shader.PropertyToID("colossal_TerrainTextureArrayBaseLod");

		public static readonly int _COTerrainHeightScaleOffset = Shader.PropertyToID("colossal_TerrainHeightScaleOffset");

		public static readonly int _LODArea = Shader.PropertyToID("_LODArea");

		public static readonly int _LODRange = Shader.PropertyToID("_LODRange");

		public static readonly int _TerrainScaleOffset = Shader.PropertyToID("_TerrainScaleOffset");

		public static readonly int _VTScaleOffset = Shader.PropertyToID("_VTScaleOffset");

		public static readonly int _HeightMap = Shader.PropertyToID("_HeightMap");

		public static readonly int _SplatMap = Shader.PropertyToID("_SplatMap");

		public static readonly int _HeightMapArray = Shader.PropertyToID("_HeightMapArray");

		public static readonly int _BaseColorMap = Shader.PropertyToID("_BaseColorMap");

		public static readonly int _OverlayExtra = Shader.PropertyToID("_OverlayExtra");

		public static readonly int _SnowMap = Shader.PropertyToID("_SnowMap");

		public static readonly int _OverlayArrowMask = Shader.PropertyToID("_OverlayArrowMask");

		public static readonly int _OverlayArrowSource = Shader.PropertyToID("_OverlayArrowSource");

		public static readonly int _OverlayPollutionMask = Shader.PropertyToID("_OverlayPollutionMask");

		public static readonly int _CODecalLayerMask = Shader.PropertyToID("colossal_DecalLayerMask");
	}

	private TerrainSystem m_TerrainSystem;

	private TerrainMaterialSystem m_TerrainMaterialSystem;

	private OverlayInfomodeSystem m_OverlayInfomodeSystem;

	private SnowSystem m_SnowSystem;

	private Material m_CachedMaterial;

	public Texture overrideOverlaymap { get; set; }

	public Texture overlayExtramap { get; set; }

	public float4 overlayArrowMask { get; set; }

	private Material material
	{
		get
		{
			return m_CachedMaterial;
		}
		set
		{
			if (m_CachedMaterial != null)
			{
				Object.DestroyImmediate(m_CachedMaterial);
			}
			m_CachedMaterial = new Material(value);
			m_CachedMaterial.SetFloat(ShaderID._CODecalLayerMask, math.asuint(1));
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		RequireForUpdate<TerrainPropertiesData>();
		material = AssetDatabase.global.resources.terrain.renderMaterial;
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_TerrainMaterialSystem = base.World.GetOrCreateSystemManaged<TerrainMaterialSystem>();
		m_OverlayInfomodeSystem = base.World.GetOrCreateSystemManaged<OverlayInfomodeSystem>();
		m_SnowSystem = base.World.GetOrCreateSystemManaged<SnowSystem>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		CoreUtils.Destroy(m_CachedMaterial);
	}

	private void UpdateMaterial()
	{
		TerrainSurface validSurface = TerrainSurface.GetValidSurface();
		m_TerrainSystem.GetCascadeInfo(out var _, out var baseLOD, out var areas, out var ranges, out var _);
		Shader.SetGlobalMatrix(ShaderID._COTerrainTextureArrayLODArea, areas);
		Shader.SetGlobalVector(ShaderID._COTerrainTextureArrayLODRange, ranges);
		Shader.SetGlobalInt(ShaderID._COTerrainTextureArrayBaseLod, baseLOD);
		Shader.SetGlobalVector(ShaderID._COTerrainHeightScaleOffset, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.heightScaleOffset.y, 0f, 0f));
		if (validSurface == null)
		{
			return;
		}
		Material material = ((this.material == null) ? validSurface.material : this.material);
		if (!(material == null))
		{
			SetKeywords(material);
			material.SetMatrix(ShaderID._LODArea, areas);
			material.SetVector(ShaderID._LODRange, ranges);
			material.SetVector(ShaderID._TerrainScaleOffset, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.heightScaleOffset.y, 0f, 0f));
			material.SetVector(ShaderID._VTScaleOffset, m_TerrainSystem.VTScaleOffset);
			Texture heightmap = m_TerrainSystem.heightmap;
			Texture texture = overrideOverlaymap;
			Texture snowDepth = m_SnowSystem.SnowDepth;
			Texture cascadeTexture = m_TerrainSystem.GetCascadeTexture();
			Texture splatmap = m_TerrainMaterialSystem.splatmap;
			if (heightmap != null)
			{
				material.SetTexture(ShaderID._HeightMap, heightmap);
			}
			if (splatmap != null)
			{
				material.SetTexture(ShaderID._SplatMap, splatmap);
			}
			if (cascadeTexture != null)
			{
				material.SetTexture(ShaderID._HeightMapArray, cascadeTexture);
			}
			if (texture != null)
			{
				material.SetTexture(ShaderID._BaseColorMap, texture);
			}
			if (overlayExtramap != null)
			{
				material.SetTexture(ShaderID._OverlayExtra, overlayExtramap);
			}
			if (snowDepth != null)
			{
				material.SetTexture(ShaderID._SnowMap, snowDepth);
			}
			material.SetVector(ShaderID._OverlayArrowMask, overlayArrowMask);
			m_TerrainMaterialSystem.UpdateMaterial(material);
			validSurface.material = material;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateMaterial();
		if (m_TerrainSystem.heightMapRenderRequired)
		{
			m_TerrainSystem.RenderCascades();
		}
	}

	private void SetKeywords(Material materialToUpdate)
	{
		if (overlayExtramap != null)
		{
			if (overrideOverlaymap == null)
			{
				overrideOverlaymap = Texture2D.whiteTexture;
			}
			materialToUpdate.EnableKeyword("OVERRIDE_OVERLAY_EXTRA");
			materialToUpdate.DisableKeyword("OVERRIDE_OVERLAY_SIMPLE");
		}
		else if (overrideOverlaymap != null)
		{
			materialToUpdate.DisableKeyword("OVERRIDE_OVERLAY_EXTRA");
			materialToUpdate.EnableKeyword("OVERRIDE_OVERLAY_SIMPLE");
		}
		else
		{
			materialToUpdate.DisableKeyword("OVERRIDE_OVERLAY_EXTRA");
			materialToUpdate.DisableKeyword("OVERRIDE_OVERLAY_SIMPLE");
		}
		if (TerrainSystem.baseLod == 0)
		{
			materialToUpdate.DisableKeyword("_PLAYABLEWORLDSELECT");
		}
		else
		{
			materialToUpdate.EnableKeyword("_PLAYABLEWORLDSELECT");
		}
	}

	public Bounds GetCascadeRegion(int index)
	{
		Bounds result = default(Bounds);
		if (index >= 0 && index < m_TerrainSystem.heightMapSliceArea.Length)
		{
			float3 @float = new float3(m_TerrainSystem.heightMapSliceArea[index].x, m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.heightMapSliceArea[index].y);
			result.SetMinMax(max: new float3(m_TerrainSystem.heightMapSliceArea[index].z, 0f, m_TerrainSystem.heightMapSliceArea[index].w), min: @float);
		}
		return result;
	}

	public Bounds GetCascadeViewport(int index)
	{
		Bounds result = default(Bounds);
		if (index >= 0 && index < m_TerrainSystem.heightMapViewportUpdated.Length)
		{
			float2 xy = m_TerrainSystem.heightMapSliceArea[index].xy;
			float2 @float = m_TerrainSystem.heightMapSliceArea[index].zw - m_TerrainSystem.heightMapSliceArea[index].xy;
			float3 zero = float3.zero;
			float3 zero2 = float3.zero;
			zero.xz = xy + @float * m_TerrainSystem.heightMapViewportUpdated[index].xy;
			zero2.xz = xy + @float * (m_TerrainSystem.heightMapViewportUpdated[index].xy + m_TerrainSystem.heightMapViewportUpdated[index].zw);
			zero.y = 0f;
			zero2.y = m_TerrainSystem.heightScaleOffset.x;
			result.SetMinMax(zero, zero2);
		}
		return result;
	}

	public Bounds GetCascadeCullArea(int index)
	{
		Bounds result = default(Bounds);
		if (index >= 0 && index < m_TerrainSystem.heightMapCullArea.Length)
		{
			float3 @float = new float3(m_TerrainSystem.heightMapCullArea[index].x, float.MaxValue, m_TerrainSystem.heightMapCullArea[index].y);
			result.SetMinMax(max: new float3(m_TerrainSystem.heightMapCullArea[index].z, float.MinValue, m_TerrainSystem.heightMapCullArea[index].w), min: @float);
		}
		return result;
	}

	public Bounds GetLastCullArea()
	{
		Bounds result = default(Bounds);
		float3 @float = new float3(m_TerrainSystem.lastCullArea.x, float.MaxValue, m_TerrainSystem.lastCullArea.y);
		result.SetMinMax(max: new float3(m_TerrainSystem.lastCullArea.z, float.MinValue, m_TerrainSystem.lastCullArea.w), min: @float);
		return result;
	}

	[Preserve]
	public TerrainRenderSystem()
	{
	}
}
