using UnityEngine;

namespace Game.Rendering.Debug;

public class LodParametersSetter : MonoBehaviour
{
	public Vector4 m_LODParameter = new Vector4(100000f, 1f, 0f, 0f);

	public void Update()
	{
		Shader.SetGlobalVector(RenderPrefabRenderer.ShaderIDs._LodParameters, m_LODParameter);
	}
}
