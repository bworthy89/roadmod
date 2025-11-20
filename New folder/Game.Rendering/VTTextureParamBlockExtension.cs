using Colossal.IO.AssetDatabase.VirtualTexturing;
using UnityEngine;

namespace Game.Rendering;

public static class VTTextureParamBlockExtension
{
	public static void SetTextureParamBlock(this MaterialPropertyBlock material, (int transform, int textureInfo) nameId, VTTextureParamBlock block)
	{
		material.SetVector(nameId.transform, block.transform);
		material.SetVector(nameId.textureInfo, block.textureInfo);
	}
}
