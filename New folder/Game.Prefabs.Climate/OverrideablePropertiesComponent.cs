using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Prefabs.Climate;

public abstract class OverrideablePropertiesComponent : ComponentBase
{
	public enum InterpolationMode
	{
		RealTime,
		Cloudiness,
		Precipitation,
		RenderingTime,
		Aurora
	}

	public InterpolationMode m_InterpolationMode;

	public float m_InterpolationTime = 5f;

	public bool hasTimeBasedInterpolation
	{
		get
		{
			if (m_InterpolationMode != InterpolationMode.RealTime)
			{
				return m_InterpolationMode == InterpolationMode.RenderingTime;
			}
			return true;
		}
	}

	public ReadOnlyCollection<VolumeParameter> parameters { get; private set; }

	protected abstract void OnBindVolumeProperties(Volume volume);

	protected override void OnEnable()
	{
		CollectVolumeParameters();
		foreach (VolumeParameter parameter in parameters)
		{
			if (parameter != null)
			{
				parameter.OnEnable();
			}
			else
			{
				UnityEngine.Debug.LogWarning("OverrideablePropertiesComponent " + GetType().Name + " contains a null parameter");
			}
		}
	}

	public void Bind(Volume volume)
	{
		OnBindVolumeProperties(volume);
		CollectVolumeParameters();
	}

	public void CollectVolumeParameters()
	{
		List<VolumeParameter> list = new List<VolumeParameter>();
		FindParameters(this, list);
		parameters = list.AsReadOnly();
	}

	public FieldInfo[] GetFieldsInfo()
	{
		return (from x in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			orderby x.MetadataToken
			where x.FieldType.IsSubclassOf(typeof(VolumeParameter))
			select x).ToArray();
	}

	private static void FindParameters(object o, List<VolumeParameter> parameters, Func<FieldInfo, bool> filter = null)
	{
		if (o == null)
		{
			return;
		}
		foreach (FieldInfo item2 in from t in o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			orderby t.MetadataToken
			select t)
		{
			if (item2.FieldType.IsSubclassOf(typeof(VolumeParameter)))
			{
				if (filter == null || filter(item2))
				{
					VolumeParameter item = (VolumeParameter)item2.GetValue(o);
					parameters.Add(item);
				}
			}
			else if (!item2.FieldType.IsArray && item2.FieldType.IsClass)
			{
				FindParameters(item2.GetValue(o), parameters, filter);
			}
		}
	}

	private void SetOverridesTo(IEnumerable<VolumeParameter> enumerable, bool state)
	{
		foreach (VolumeParameter item in enumerable)
		{
			item.overrideState = state;
			Type type = item.GetType();
			if (VolumeParameter.IsObjectParameter(type))
			{
				ReadOnlyCollection<VolumeParameter> readOnlyCollection = (ReadOnlyCollection<VolumeParameter>)type.GetProperty("parameters", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item, null);
				if (readOnlyCollection != null)
				{
					SetOverridesTo(readOnlyCollection, state);
				}
			}
		}
	}

	public void SetAllOverridesTo(bool state)
	{
		SetOverridesTo(parameters, state);
	}

	public virtual void Override(OverrideablePropertiesComponent state, float interpFactor = 1f)
	{
		int count = parameters.Count;
		for (int i = 0; i < count; i++)
		{
			VolumeParameter volumeParameter = state.parameters[i];
			VolumeParameter volumeParameter2 = parameters[i];
			if (volumeParameter2.overrideState)
			{
				volumeParameter.overrideState = volumeParameter2.overrideState;
				volumeParameter.Interp(volumeParameter, volumeParameter2, interpFactor);
			}
		}
	}

	public virtual void Override(OverrideablePropertiesComponent previous, OverrideablePropertiesComponent to, float interpFactor = 1f)
	{
		int count = parameters.Count;
		m_InterpolationMode = to.m_InterpolationMode;
		m_InterpolationTime = to.m_InterpolationTime;
		for (int i = 0; i < count; i++)
		{
			VolumeParameter volumeParameter = previous.parameters[i];
			VolumeParameter volumeParameter2 = to.parameters[i];
			if (volumeParameter2.overrideState)
			{
				parameters[i].overrideState = volumeParameter2.overrideState;
				parameters[i].Interp(volumeParameter, volumeParameter2, interpFactor);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
