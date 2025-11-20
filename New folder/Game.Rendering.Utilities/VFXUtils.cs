using UnityEngine;
using UnityEngine.VFX;

namespace Game.Rendering.Utilities;

public static class VFXUtils
{
	public static bool SetCheckedFloat(this VisualEffect effect, int id, float v)
	{
		if (effect.HasFloat(id))
		{
			effect.SetFloat(id, v);
			return true;
		}
		return false;
	}

	public static bool SetCheckedVector3(this VisualEffect effect, int id, Vector3 v)
	{
		if (effect.HasVector3(id))
		{
			effect.SetVector3(id, v);
			return true;
		}
		return false;
	}

	public static bool SetCheckedVector4(this VisualEffect effect, int id, Vector4 v)
	{
		if (effect.HasVector4(id))
		{
			effect.SetVector4(id, v);
			return true;
		}
		return false;
	}

	public static bool SetCheckedTexture(this VisualEffect effect, int id, Texture v)
	{
		if (effect.HasTexture(id))
		{
			effect.SetTexture(id, v);
			return true;
		}
		return false;
	}

	public static bool SetCheckedInt(this VisualEffect effect, int id, int v)
	{
		if (effect.HasInt(id))
		{
			effect.SetInt(id, v);
			return true;
		}
		return false;
	}
}
