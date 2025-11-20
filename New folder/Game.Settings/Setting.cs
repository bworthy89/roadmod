using System;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Logging;
using Colossal.Reflection;
using Game.SceneFlow;
using Game.UI.Menu;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Settings;

public abstract class Setting : IEquatable<Setting>
{
	protected static ILog log = LogManager.GetLogger("SceneFlow");

	protected static SharedSettings settings => GameManager.instance?.settings;

	[Exclude]
	protected internal virtual bool builtIn => true;

	public event OnSettingsAppliedHandler onSettingsApplied;

	public bool Equals(Setting obj)
	{
		return Equals((object)obj);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		Type type = obj.GetType();
		if (!type.IsAssignableFrom(GetType()))
		{
			return false;
		}
		PropertyInfo property = type.GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
		if (property != null && !(bool)property.GetValue(this) && object.Equals(property.GetValue(this), property.GetValue(obj)))
		{
			return true;
		}
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (ReflectionUtils.GetAttribute<IgnoreEqualsAttribute>(propertyInfo.GetCustomAttributes(inherit: false)) == null && propertyInfo.CanRead && !object.Equals(propertyInfo.GetValue(this), propertyInfo.GetValue(obj)))
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		int num = 0;
		PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			num = (num * 937) ^ propertyInfo.GetValue(this).GetHashCode();
		}
		return num;
	}

	protected bool TryGetGameplayCameraController(ref CameraController controller)
	{
		if (controller != null)
		{
			return true;
		}
		GameObject gameObject = GameObject.FindGameObjectWithTag("GameplayCamera");
		if (gameObject != null)
		{
			controller = gameObject.GetComponent<CameraController>();
			return true;
		}
		controller = null;
		return false;
	}

	protected bool TryGetGameplayCamera(ref HDAdditionalCameraData cameraData)
	{
		if (cameraData != null)
		{
			return true;
		}
		Camera main = Camera.main;
		if (main != null)
		{
			cameraData = main.GetComponent<HDAdditionalCameraData>();
			return true;
		}
		cameraData = null;
		return false;
	}

	protected bool TryGetGameplayCamera(ref Camera camera)
	{
		if (camera != null)
		{
			return true;
		}
		camera = Camera.main;
		if (camera != null)
		{
			return true;
		}
		return false;
	}

	protected bool TryGetSunLight(ref Light sunLight)
	{
		if (sunLight != null)
		{
			return true;
		}
		GameObject gameObject = GameObject.FindGameObjectWithTag("SunLight");
		if (gameObject != null)
		{
			sunLight = gameObject.GetComponent<Light>();
			return true;
		}
		sunLight = null;
		return false;
	}

	protected bool TryGetSunLightData(ref HDAdditionalLightData sunLightData)
	{
		if (sunLightData != null)
		{
			return true;
		}
		GameObject gameObject = GameObject.FindGameObjectWithTag("SunLight");
		if (gameObject != null)
		{
			sunLightData = gameObject.GetComponent<HDAdditionalLightData>();
			return true;
		}
		sunLightData = null;
		return false;
	}

	public async void ApplyAndSave()
	{
		Apply();
		await AssetDatabase.global.SaveSettings();
	}

	public virtual void Apply()
	{
		log.VerboseFormat("Applying settings for {0}", GetType());
		this.onSettingsApplied?.Invoke(this);
	}

	public abstract void SetDefaults();

	public virtual AutomaticSettings.SettingPageData GetPageData(string id, bool addPrefix)
	{
		return AutomaticSettings.FillSettingsPage(this, id, addPrefix);
	}

	internal void RegisterInOptionsUI(string name, bool addPrefix = false)
	{
		RegisterInOptionsUI(this, name, addPrefix);
	}

	internal static bool RegisterInOptionsUI(Setting instance, string name, bool addPrefix)
	{
		OptionsUISystem optionsUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<OptionsUISystem>();
		if (optionsUISystem != null)
		{
			optionsUISystem.RegisterSetting(instance, name, addPrefix);
			return true;
		}
		return false;
	}

	internal static bool UnregisterInOptionsUI(string name)
	{
		OptionsUISystem optionsUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<OptionsUISystem>();
		if (optionsUISystem != null)
		{
			optionsUISystem.UnregisterSettings(name);
			return true;
		}
		return false;
	}
}
