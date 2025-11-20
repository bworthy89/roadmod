using Unity.Mathematics;
using UnityEngine;

namespace Game.Rendering.Debug;

public class DecalLayerSetter : MonoBehaviour
{
	private Renderer m_MeshRenderer;

	public DecalLayers m_LayerMask = DecalLayers.Terrain;

	private void Awake()
	{
		m_MeshRenderer = GetComponent<MeshRenderer>();
	}

	public void Update()
	{
		m_MeshRenderer.sharedMaterial.SetFloat(RenderPrefabRenderer.ShaderIDs._DecalLayerMask, math.asfloat((int)m_LayerMask));
	}
}
