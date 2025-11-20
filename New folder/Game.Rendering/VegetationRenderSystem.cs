using Colossal.Entities;
using Game.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.VFX;

namespace Game.Rendering;

public class VegetationRenderSystem : GameSystemBase
{
	private TerrainSystem m_TerrainSystem;

	private TerrainMaterialSystem m_TerrainMaterialSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private static VisualEffectAsset s_FoliageVFXAsset;

	private VisualEffect m_FoliageVFX;

	[Preserve]
	protected override void OnCreate()
	{
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_TerrainMaterialSystem = base.World.GetOrCreateSystemManaged<TerrainMaterialSystem>();
		s_FoliageVFXAsset = Resources.Load<VisualEffectAsset>("Vegetation/FoliageVFX");
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		base.OnStopRunning();
		CoreUtils.Destroy(m_FoliageVFX);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_CameraUpdateSystem.activeViewer != null)
		{
			CreateDynamicVFXIfNeeded();
			UpdateEffect();
		}
	}

	private void UpdateEffect()
	{
		Bounds terrainBounds = m_TerrainSystem.GetTerrainBounds();
		m_FoliageVFX.SetVector3("TerrainBounds_center", terrainBounds.center);
		m_FoliageVFX.SetVector3("TerrainBounds_size", terrainBounds.size);
		m_FoliageVFX.SetTexture("Terrain HeightMap", m_TerrainSystem.heightmap);
		m_FoliageVFX.SetTexture("Terrain SplatMap", m_TerrainMaterialSystem.splatmap);
		Vector4 globalVector = Shader.GetGlobalVector("colossal_TerrainScale");
		Vector4 globalVector2 = Shader.GetGlobalVector("colossal_TerrainOffset");
		m_FoliageVFX.SetVector4("Terrain Offset Scale", new Vector4(globalVector.x, globalVector.z, globalVector2.x, globalVector2.z));
		m_FoliageVFX.SetVector3("CameraPosition", m_CameraUpdateSystem.position);
		m_FoliageVFX.SetVector3("CameraDirection", m_CameraUpdateSystem.direction);
	}

	private void CreateDynamicVFXIfNeeded()
	{
		if (s_FoliageVFXAsset != null && m_FoliageVFX == null)
		{
			COSystemBase.baseLog.DebugFormat("Creating FoliageVFX");
			m_FoliageVFX = new GameObject("FoliageVFX").AddComponent<VisualEffect>();
			m_FoliageVFX.visualEffectAsset = s_FoliageVFXAsset;
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	[Preserve]
	public VegetationRenderSystem()
	{
	}
}
