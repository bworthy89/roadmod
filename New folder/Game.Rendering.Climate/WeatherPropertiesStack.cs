using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Mathematics;
using Game.Prefabs.Climate;
using Game.Simulation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering.Climate;

internal class WeatherPropertiesStack : IDisposable
{
	public class InterpolatedProperties
	{
		public float time;

		public Bounds1 remapLimits;

		public readonly OverrideablePropertiesComponent current;

		public readonly OverrideablePropertiesComponent previous;

		public readonly OverrideablePropertiesComponent target;

		public readonly OverrideablePropertiesComponent from;

		public readonly OverrideablePropertiesComponent to;

		internal OverrideablePropertiesComponent source;

		public InterpolatedProperties(OverrideablePropertiesComponent current, OverrideablePropertiesComponent previous, OverrideablePropertiesComponent target, OverrideablePropertiesComponent from, OverrideablePropertiesComponent to)
		{
			this.current = current;
			this.previous = previous;
			this.target = target;
			this.from = from;
			this.to = to;
		}

		private static float Remap(float value, float from1, float to1, float from2, float to2)
		{
			return math.saturate((value - from1) / (to1 - from1) * (to2 - from2) + from2);
		}

		public void SetTarget(OverrideablePropertiesComponent newTarget)
		{
			newTarget.Override(target);
			source = newTarget;
			target.m_InterpolationMode = source.m_InterpolationMode;
			target.m_InterpolationTime = source.m_InterpolationTime;
			time = 0f;
		}

		public void SetPrevious(OverrideablePropertiesComponent newSource)
		{
			newSource.Override(previous);
			time = 0f;
		}

		public void SetTo(OverrideablePropertiesComponent newTo)
		{
			newTo.Override(to);
			source = newTo;
			to.m_InterpolationMode = source.m_InterpolationMode;
			to.m_InterpolationTime = source.m_InterpolationTime;
			time = 0f;
		}

		public void SetFrom(OverrideablePropertiesComponent newTo)
		{
			newTo.Override(from);
			time = 0f;
		}

		public void Advance(float deltaTime, float renderingDeltaTime)
		{
			OverrideablePropertiesComponent.InterpolationMode interpolationMode = target.m_InterpolationMode;
			if (interpolationMode == OverrideablePropertiesComponent.InterpolationMode.RealTime || interpolationMode != OverrideablePropertiesComponent.InterpolationMode.RenderingTime)
			{
				time = math.saturate(time + deltaTime / target.m_InterpolationTime);
			}
			else
			{
				time = math.saturate(time + renderingDeltaTime / target.m_InterpolationTime);
			}
		}

		public float GetLerp(ClimateSystem.ClimateSample sample)
		{
			return to.m_InterpolationMode switch
			{
				OverrideablePropertiesComponent.InterpolationMode.Cloudiness => Remap(sample.cloudiness, remapLimits.min, remapLimits.max, 0f, 1f), 
				OverrideablePropertiesComponent.InterpolationMode.Precipitation => sample.precipitation, 
				OverrideablePropertiesComponent.InterpolationMode.Aurora => sample.aurora, 
				_ => 1f, 
			};
		}
	}

	private Volume m_Volume;

	public readonly Dictionary<Type, InterpolatedProperties> components = new Dictionary<Type, InterpolatedProperties>();

	public WeatherPropertiesStack(Volume volume = null)
	{
		m_Volume = volume;
		CreateInterpolatedRepresentation();
	}

	private void CreateInterpolatedRepresentation()
	{
		foreach (Type item in from t in CoreUtils.GetAllTypesDerivedFrom<OverrideablePropertiesComponent>()
			where !t.IsAbstract
			select t)
		{
			OverrideablePropertiesComponent overrideablePropertiesComponent = (OverrideablePropertiesComponent)ScriptableObject.CreateInstance(item);
			OverrideablePropertiesComponent previous = (OverrideablePropertiesComponent)ScriptableObject.CreateInstance(item);
			OverrideablePropertiesComponent target = (OverrideablePropertiesComponent)ScriptableObject.CreateInstance(item);
			OverrideablePropertiesComponent overrideablePropertiesComponent2 = (OverrideablePropertiesComponent)ScriptableObject.CreateInstance(item);
			OverrideablePropertiesComponent to = (OverrideablePropertiesComponent)ScriptableObject.CreateInstance(item);
			overrideablePropertiesComponent.Bind(m_Volume);
			components.Add(item, new InterpolatedProperties(overrideablePropertiesComponent, previous, target, overrideablePropertiesComponent2, to));
		}
	}

	public void SetTarget(Type type, OverrideablePropertiesComponent target)
	{
		components.TryGetValue(type, out var value);
		value.SetTarget(target);
		value.SetPrevious(value.current);
	}

	public void SetTo(Type type, OverrideablePropertiesComponent to, bool setLimits, Bounds1 limits)
	{
		components.TryGetValue(type, out var value);
		if (setLimits)
		{
			value.remapLimits = limits;
		}
		value.SetTo(to);
		value.SetPrevious(value.current);
	}

	public void SetFrom(Type type, OverrideablePropertiesComponent from)
	{
		components.TryGetValue(type, out var value);
		value.SetFrom(from);
		value.SetPrevious(value.current);
	}

	public void InterpolateOverrideData(float deltaTime, float renderingDeltaTime, ClimateSystem.ClimateSample sample, bool editMode)
	{
		foreach (KeyValuePair<Type, InterpolatedProperties> component in components)
		{
			if (component.Value.target.active)
			{
				if (!component.Value.to.hasTimeBasedInterpolation)
				{
					float lerp = component.Value.GetLerp(sample);
					component.Value.target.Override(component.Value.from, component.Value.to, lerp);
				}
				if (editMode)
				{
					component.Value.source.Override(component.Value.target);
				}
				component.Value.current.Override(component.Value.previous, component.Value.target, component.Value.time);
				component.Value.Advance(deltaTime, renderingDeltaTime);
			}
		}
	}

	public void Dispose()
	{
		foreach (KeyValuePair<Type, InterpolatedProperties> component in components)
		{
			CoreUtils.Destroy(component.Value.current);
			CoreUtils.Destroy(component.Value.previous);
			CoreUtils.Destroy(component.Value.target);
			CoreUtils.Destroy(component.Value.from);
			CoreUtils.Destroy(component.Value.to);
		}
		components.Clear();
	}
}
