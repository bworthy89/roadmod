using System;
using System.Collections.Generic;
using Colossal;
using Game.Reflection;
using Game.Rendering;
using Game.UI;
using Game.UI.Widgets;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

namespace Game.Prefabs.Effects;

[ComponentMenu("Effects/", new Type[] { typeof(EffectPrefab) })]
public class LightEffect : ComponentBase
{
	private class ColorTemperatureSliderFieldFactory : IFieldBuilderFactory
	{
		private IconValuePairs iconValuePairs;

		private string GetIconSource(float value)
		{
			return iconValuePairs.GetIconFromValue(value);
		}

		public FieldBuilder TryCreate(Type memberType, object[] attributes)
		{
			iconValuePairs = new IconValuePairs(new IconValuePairs.IconValuePair[7]
			{
				new IconValuePairs.IconValuePair("Media/Editor/Temperature01.svg", 2500f),
				new IconValuePairs.IconValuePair("Media/Editor/Temperature02.svg", 3500f),
				new IconValuePairs.IconValuePair("Media/Editor/Temperature03.svg", 4500f),
				new IconValuePairs.IconValuePair("Media/Editor/Temperature04.svg", 6000f),
				new IconValuePairs.IconValuePair("Media/Editor/Temperature05.svg", 7000f),
				new IconValuePairs.IconValuePair("Media/Editor/Temperature06.svg", 10000f),
				new IconValuePairs.IconValuePair("Media/Editor/Temperature07.svg", 20000f)
			});
			return (IValueAccessor accessor) => new GradientSliderField
			{
				accessor = new CastAccessor<float>(accessor),
				displayName = "Color temperature",
				gradient = (ColorGradient)ColorUtils.GetTemperatureGradient(),
				min = 1500f,
				max = 20000f,
				iconSrc = () => GetIconSource((float)accessor.GetValue())
			};
		}
	}

	public Game.Rendering.LightType m_Type;

	public Game.Rendering.SpotLightShape m_SpotShape;

	public Game.Rendering.AreaLightShape m_AreaShape;

	public float m_Range = 25f;

	[FormerlySerializedAs("m_LuxIntensity")]
	[HideInInspector]
	public float m_Intensity = 10f;

	public LightIntensity m_LightIntensity;

	[FormerlySerializedAs("m_LuxDistance")]
	public float m_LuxAtDistance = 5f;

	[HideInInspector]
	public Game.Rendering.LightUnit m_LightUnit = Game.Rendering.LightUnit.Lux;

	public bool m_EnableSpotReflector = true;

	[Range(1f, 179f)]
	public float m_SpotAngle = 150f;

	[Range(0f, 100f)]
	public float m_InnerSpotPercentage = 50f;

	public float m_ShapeRadius = 0.025f;

	[Range(0.05f, 20f)]
	public float m_AspectRatio = 1f;

	public float m_ShapeWidth = 0.5f;

	public float m_ShapeHeight = 0.5f;

	public bool m_UseColorTemperature = true;

	public Color m_Color = new Color(1f, 0.86f, 0.65f, 1f);

	[CustomField(typeof(ColorTemperatureSliderFieldFactory))]
	public float m_ColorTemperature = 6570f;

	public Texture m_Cookie;

	public bool m_AffectDiffuse = true;

	public bool m_AffectSpecular = true;

	public bool m_ApplyRangeAttenuation = true;

	[Range(0f, 16f)]
	public float m_LightDimmer = 1f;

	public float m_LodBias;

	[HideInInspector]
	public float m_BarnDoorAngle;

	[HideInInspector]
	public float m_BarnDoorLength;

	public bool m_UseVolumetric = true;

	[Range(0f, 16f)]
	public float m_VolumetricDimmer = 1f;

	public float m_VolumetricFadeDistance = 10000f;

	public void RecalculateIntensity(Game.Rendering.LightUnit oldUnit, Game.Rendering.LightUnit newUnit)
	{
		m_Intensity = Game.Rendering.LightUtils.ConvertLightIntensity(oldUnit, newUnit, this, m_Intensity);
		m_LightIntensity.m_Intensity = m_Intensity;
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<LightEffectData>());
		components.Add(ComponentType.ReadWrite<EffectColorData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		int num = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(m_Range)), m_LodBias);
		float num2 = RenderingUtils.CalculateDistanceFactor(num);
		float invDistanceFactor = 1f / num2;
		if (m_LightIntensity != null)
		{
			if (m_LightUnit != m_LightIntensity.m_LightUnit)
			{
				RecalculateIntensity(m_LightUnit, m_LightIntensity.m_LightUnit);
			}
			m_Intensity = m_LightIntensity.m_Intensity;
			m_LightUnit = m_LightIntensity.m_LightUnit;
		}
		else
		{
			m_LightIntensity = new LightIntensity
			{
				m_Intensity = m_Intensity,
				m_LightUnit = m_LightUnit
			};
		}
		LightEffectData componentData = new LightEffectData
		{
			m_Range = m_Range,
			m_DistanceFactor = num2,
			m_InvDistanceFactor = invDistanceFactor,
			m_MinLod = num
		};
		entityManager.SetComponentData(entity, componentData);
		EffectColorData componentData2 = entityManager.GetComponentData<EffectColorData>(entity);
		componentData2.m_Color = ComputeLightFinalColor();
		entityManager.SetComponentData(entity, componentData2);
	}

	public HDLightTypeAndShape GetLightTypeAndShape()
	{
		return m_Type switch
		{
			Game.Rendering.LightType.Spot => m_SpotShape switch
			{
				Game.Rendering.SpotLightShape.Cone => HDLightTypeAndShape.ConeSpot, 
				Game.Rendering.SpotLightShape.Box => HDLightTypeAndShape.BoxSpot, 
				Game.Rendering.SpotLightShape.Pyramid => HDLightTypeAndShape.PyramidSpot, 
				_ => throw new NotImplementedException($"Spot shape not implemented {m_SpotShape}"), 
			}, 
			Game.Rendering.LightType.Point => HDLightTypeAndShape.Point, 
			Game.Rendering.LightType.Area => m_AreaShape switch
			{
				Game.Rendering.AreaLightShape.Rectangle => HDLightTypeAndShape.RectangleArea, 
				Game.Rendering.AreaLightShape.Tube => HDLightTypeAndShape.TubeArea, 
				_ => throw new NotImplementedException($"Area shape not implemented {m_AreaShape}"), 
			}, 
			_ => throw new NotImplementedException($"Light type not implemented {m_Type}"), 
		};
	}

	public Color GetEmissionColor()
	{
		Color color = m_Color.linear * m_LightIntensity.m_Intensity;
		if (m_UseColorTemperature)
		{
			color *= Mathf.CorrelatedColorTemperatureToRGB(m_ColorTemperature);
		}
		return color * m_LightDimmer;
	}

	private float CalculateLightIntensityPunctual(float intensity)
	{
		switch (m_Type)
		{
		case Game.Rendering.LightType.Point:
			if (m_LightUnit == Game.Rendering.LightUnit.Candela)
			{
				return intensity;
			}
			return Game.Rendering.LightUtils.ConvertPointLightLumenToCandela(intensity);
		case Game.Rendering.LightType.Spot:
			if (m_LightUnit == Game.Rendering.LightUnit.Candela)
			{
				return intensity;
			}
			if (m_EnableSpotReflector)
			{
				if (m_SpotShape == Game.Rendering.SpotLightShape.Cone)
				{
					return Game.Rendering.LightUtils.ConvertSpotLightLumenToCandela(intensity, m_SpotAngle * (MathF.PI / 180f), exact: true);
				}
				if (m_SpotShape == Game.Rendering.SpotLightShape.Pyramid)
				{
					Game.Rendering.LightUtils.CalculateAnglesForPyramid(m_AspectRatio, m_SpotAngle * (MathF.PI / 180f), out var angleA, out var angleB);
					return Game.Rendering.LightUtils.ConvertFrustrumLightLumenToCandela(intensity, angleA, angleB);
				}
				return Game.Rendering.LightUtils.ConvertPointLightLumenToCandela(intensity);
			}
			return Game.Rendering.LightUtils.ConvertPointLightLumenToCandela(intensity);
		default:
			return intensity;
		}
	}

	private float ComputeLightIntensity()
	{
		if (m_LightUnit == Game.Rendering.LightUnit.Lumen)
		{
			if (m_Type == Game.Rendering.LightType.Spot || m_Type == Game.Rendering.LightType.Point)
			{
				return CalculateLightIntensityPunctual(m_LightIntensity.m_Intensity);
			}
			return Game.Rendering.LightUtils.ConvertAreaLightLumenToLuminance(m_AreaShape, m_LightIntensity.m_Intensity, m_ShapeWidth, m_ShapeHeight);
		}
		if (m_LightUnit == Game.Rendering.LightUnit.Ev100)
		{
			return Game.Rendering.LightUtils.ConvertEvToLuminance(m_LightIntensity.m_Intensity);
		}
		if ((m_Type == Game.Rendering.LightType.Spot || m_Type == Game.Rendering.LightType.Point) && m_LightUnit == Game.Rendering.LightUnit.Lux)
		{
			if (m_Type == Game.Rendering.LightType.Spot && m_SpotShape == Game.Rendering.SpotLightShape.Box)
			{
				return m_LightIntensity.m_Intensity;
			}
			return Game.Rendering.LightUtils.ConvertLuxToCandela(m_LightIntensity.m_Intensity, m_LuxAtDistance);
		}
		return m_LightIntensity.m_Intensity;
	}

	public Color ComputeLightFinalColor()
	{
		Color color = m_Color.linear * ComputeLightIntensity();
		if (m_UseColorTemperature)
		{
			color *= Mathf.CorrelatedColorTemperatureToRGB(m_ColorTemperature);
		}
		return color * m_LightDimmer;
	}
}
