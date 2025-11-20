using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Rendering;

public struct OverlayAtlasElement
{
	public float top;

	public float bot;

	public float right;

	public float left;

	public float2 scale;

	public float2 transformation;

	public static int SizeOf => Marshal.SizeOf(typeof(OverlayAtlasElement));

	public OverlayAtlasElement(Rect rectSource, Rect rectTarget)
	{
		top = rectSource.yMax;
		bot = rectSource.yMin;
		right = rectSource.xMax;
		left = rectSource.xMin;
		scale.x = ((right - left == 0f) ? 1f : ((rectTarget.xMax - rectTarget.xMin) / (right - left)));
		scale.y = ((top - bot == 0f) ? 1f : ((rectTarget.yMax - rectTarget.yMin) / (top - bot)));
		transformation = new float2(rectTarget.xMin - left * scale.x, rectTarget.yMin - bot * scale.y);
	}

	public float2 TransformUV(float2 source)
	{
		float2 result = default(float2);
		result.x = Mathf.Clamp(source.x, left, right) * scale.x + transformation.x;
		result.y = Mathf.Clamp(source.y, bot, top) * scale.y + transformation.y;
		return result;
	}
}
