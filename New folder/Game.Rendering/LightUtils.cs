using System;
using Game.Prefabs.Effects;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering;

public static class LightUtils
{
	private static float s_LuminanceToEvFactor => Mathf.Log(100f / ColorUtils.s_LightMeterCalibrationConstant, 2f);

	private static float s_EvToLuminanceFactor => 0f - Mathf.Log(100f / ColorUtils.s_LightMeterCalibrationConstant, 2f);

	public static float ConvertSpotLightLumenToCandela(float intensity, float angle, bool exact)
	{
		if (!exact)
		{
			return intensity / MathF.PI;
		}
		return intensity / (2f * (1f - Mathf.Cos(angle / 2f)) * MathF.PI);
	}

	public static float ConvertFrustrumLightLumenToCandela(float intensity, float angleA, float angleB)
	{
		return intensity / (4f * Mathf.Asin(Mathf.Sin(angleA / 2f) * Mathf.Sin(angleB / 2f)));
	}

	public static float ConvertPunctualLightLumenToCandela(LightType lightType, float lumen, float initialIntensity, bool enableSpotReflector)
	{
		if (lightType == LightType.Spot && enableSpotReflector)
		{
			return initialIntensity;
		}
		return ConvertPointLightLumenToCandela(lumen);
	}

	public static float ConvertPointLightLumenToCandela(float intensity)
	{
		return intensity / (MathF.PI * 4f);
	}

	public static float ConvertPunctualLightLumenToLux(LightType lightType, float lumen, float initialIntensity, bool enableSpotReflector, float distance)
	{
		return ConvertCandelaToLux(ConvertPunctualLightLumenToCandela(lightType, lumen, initialIntensity, enableSpotReflector), distance);
	}

	public static float ConvertCandelaToLux(float candela, float distance)
	{
		return candela / (distance * distance);
	}

	public static float ConvertPunctualLightLumenToEv(LightType lightType, float lumen, float initialIntensity, bool enableSpotReflector)
	{
		return ConvertCandelaToEv(ConvertPunctualLightLumenToCandela(lightType, lumen, initialIntensity, enableSpotReflector));
	}

	public static float ConvertCandelaToEv(float candela)
	{
		return ConvertLuminanceToEv(candela);
	}

	public static float ConvertLuminanceToEv(float luminance)
	{
		return Mathf.Log(luminance, 2f) + s_LuminanceToEvFactor;
	}

	public static float ConvertPunctualLightCandelaToLumen(LightType lightType, SpotLightShape spotLightShape, float candela, bool enableSpotReflector, float spotAngle, float aspectRatio)
	{
		if (lightType == LightType.Spot && enableSpotReflector)
		{
			switch (spotLightShape)
			{
			case SpotLightShape.Cone:
				return ConvertSpotLightCandelaToLumen(candela, spotAngle * (MathF.PI / 180f), exact: true);
			case SpotLightShape.Pyramid:
			{
				CalculateAnglesForPyramid(aspectRatio, spotAngle * (MathF.PI / 180f), out var angleA, out var angleB);
				return ConvertFrustrumLightCandelaToLumen(candela, angleA, angleB);
			}
			default:
				return ConvertPointLightCandelaToLumen(candela);
			}
		}
		return ConvertPointLightCandelaToLumen(candela);
	}

	public static float ConvertSpotLightCandelaToLumen(float intensity, float angle, bool exact)
	{
		if (!exact)
		{
			return intensity * MathF.PI;
		}
		return intensity * (2f * (1f - Mathf.Cos(angle / 2f)) * MathF.PI);
	}

	public static float ConvertFrustrumLightCandelaToLumen(float intensity, float angleA, float angleB)
	{
		return intensity * (4f * Mathf.Asin(Mathf.Sin(angleA / 2f) * Mathf.Sin(angleB / 2f)));
	}

	public static float ConvertPointLightCandelaToLumen(float intensity)
	{
		return intensity * (MathF.PI * 4f);
	}

	public static void CalculateAnglesForPyramid(float aspectRatio, float spotAngle, out float angleA, out float angleB)
	{
		if (aspectRatio < 1f)
		{
			aspectRatio = 1f / aspectRatio;
		}
		angleA = spotAngle;
		float f = angleA * 0.5f;
		f = Mathf.Atan(Mathf.Tan(f) * aspectRatio);
		angleB = f * 2f;
	}

	public static float ConvertPunctualLightLuxToLumen(LightType lightType, SpotLightShape spotLightShape, float lux, bool enableSpotReflector, float spotAngle, float aspectRatio, float distance)
	{
		float candela = ConvertLuxToCandela(lux, distance);
		return ConvertPunctualLightCandelaToLumen(lightType, spotLightShape, candela, enableSpotReflector, spotAngle, aspectRatio);
	}

	public static float ConvertLuxToCandela(float lux, float distance)
	{
		return lux * distance * distance;
	}

	public static float ConvertLuxToEv(float lux, float distance)
	{
		return ConvertLuminanceToEv(ConvertLuxToCandela(lux, distance));
	}

	public static float ConvertPunctualLightEvToLumen(LightType lightType, SpotLightShape spotLightShape, float ev, bool enableSpotReflector, float spotAngle, float aspectRatio)
	{
		float candela = ConvertEvToCandela(ev);
		return ConvertPunctualLightCandelaToLumen(lightType, spotLightShape, candela, enableSpotReflector, spotAngle, aspectRatio);
	}

	public static float ConvertEvToCandela(float ev)
	{
		return ConvertEvToLuminance(ev);
	}

	public static float ConvertEvToLuminance(float ev)
	{
		return Mathf.Pow(2f, ev + s_EvToLuminanceFactor);
	}

	public static float ConvertEvToLux(float ev, float distance)
	{
		return ConvertCandelaToLux(ConvertEvToLuminance(ev), distance);
	}

	public static float ConvertAreaLightLumenToLuminance(AreaLightShape areaLightShape, float lumen, float width, float height = 0f)
	{
		return areaLightShape switch
		{
			AreaLightShape.Tube => CalculateLineLightLumenToLuminance(lumen, width), 
			AreaLightShape.Rectangle => ConvertRectLightLumenToLuminance(lumen, width, height), 
			_ => lumen, 
		};
	}

	public static float ConvertRectLightLumenToLuminance(float intensity, float width, float height)
	{
		return intensity / (width * height * MathF.PI);
	}

	public static float CalculateLineLightLumenToLuminance(float intensity, float lineWidth)
	{
		return intensity / (MathF.PI * 4f * lineWidth);
	}

	public static float ConvertAreaLightLuminanceToLumen(AreaLightShape areaLightShape, float luminance, float width, float height = 0f)
	{
		return areaLightShape switch
		{
			AreaLightShape.Tube => CalculateLineLightLuminanceToLumen(luminance, width), 
			AreaLightShape.Rectangle => ConvertRectLightLuminanceToLumen(luminance, width, height), 
			_ => luminance, 
		};
	}

	public static float CalculateLineLightLuminanceToLumen(float intensity, float lineWidth)
	{
		return intensity * (MathF.PI * 4f * lineWidth);
	}

	public static float ConvertRectLightLuminanceToLumen(float intensity, float width, float height)
	{
		return intensity * (width * height * MathF.PI);
	}

	public static float ConvertAreaLightEvToLumen(AreaLightShape AreaLightShape, float ev, float width, float height)
	{
		float luminance = ConvertEvToLuminance(ev);
		return ConvertAreaLightLuminanceToLumen(AreaLightShape, luminance, width, height);
	}

	public static float ConvertAreaLightLumenToEv(AreaLightShape AreaLightShape, float lumen, float width, float height)
	{
		return ConvertLuminanceToEv(ConvertAreaLightLumenToLuminance(AreaLightShape, lumen, width, height));
	}

	public static float ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit, LightEffect editor, float intensity)
	{
		LightType type = editor.m_Type;
		switch (type)
		{
		case LightType.Spot:
		case LightType.Point:
			if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Candela)
			{
				intensity = ConvertPunctualLightLumenToCandela(type, intensity, intensity, editor.m_EnableSpotReflector);
			}
			else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Lux)
			{
				intensity = ConvertPunctualLightLumenToLux(type, intensity, intensity, editor.m_EnableSpotReflector, editor.m_LuxAtDistance);
			}
			else if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
			{
				intensity = ConvertPunctualLightLumenToEv(type, intensity, intensity, editor.m_EnableSpotReflector);
			}
			else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lumen)
			{
				intensity = ConvertPunctualLightCandelaToLumen(type, editor.m_SpotShape, intensity, editor.m_EnableSpotReflector, editor.m_SpotAngle, editor.m_AspectRatio);
			}
			else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lux)
			{
				intensity = ConvertCandelaToLux(intensity, editor.m_LuxAtDistance);
			}
			else if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Ev100)
			{
				intensity = ConvertCandelaToEv(intensity);
			}
			else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Lumen)
			{
				intensity = ConvertPunctualLightLuxToLumen(type, editor.m_SpotShape, intensity, editor.m_EnableSpotReflector, editor.m_SpotAngle, editor.m_AspectRatio, editor.m_LuxAtDistance);
			}
			else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Candela)
			{
				intensity = ConvertLuxToCandela(intensity, editor.m_LuxAtDistance);
			}
			else if (oldLightUnit == LightUnit.Lux && newLightUnit == LightUnit.Ev100)
			{
				intensity = ConvertLuxToEv(intensity, editor.m_LuxAtDistance);
			}
			else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
			{
				intensity = ConvertPunctualLightEvToLumen(type, editor.m_SpotShape, intensity, editor.m_EnableSpotReflector, editor.m_SpotAngle, editor.m_AspectRatio);
			}
			else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Candela)
			{
				intensity = ConvertEvToCandela(intensity);
			}
			else if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lux)
			{
				intensity = ConvertEvToLux(intensity, editor.m_LuxAtDistance);
			}
			break;
		case LightType.Area:
			if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Nits)
			{
				intensity = ConvertAreaLightLumenToLuminance(editor.m_AreaShape, intensity, editor.m_ShapeWidth, editor.m_ShapeHeight);
			}
			if (oldLightUnit == LightUnit.Nits && newLightUnit == LightUnit.Lumen)
			{
				intensity = ConvertAreaLightLuminanceToLumen(editor.m_AreaShape, intensity, editor.m_ShapeWidth, editor.m_ShapeHeight);
			}
			if (oldLightUnit == LightUnit.Nits && newLightUnit == LightUnit.Ev100)
			{
				intensity = ConvertLuminanceToEv(intensity);
			}
			if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Nits)
			{
				intensity = ConvertEvToLuminance(intensity);
			}
			if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
			{
				intensity = ConvertAreaLightEvToLumen(editor.m_AreaShape, intensity, editor.m_ShapeWidth, editor.m_ShapeHeight);
			}
			if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
			{
				intensity = ConvertAreaLightLumenToEv(editor.m_AreaShape, intensity, editor.m_ShapeWidth, editor.m_ShapeHeight);
			}
			break;
		}
		return intensity;
	}
}
