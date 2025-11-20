using Game.Rendering;
using Unity.Entities;

namespace Game.Settings;

[SettingsUIAdvanced]
[SettingsUISection("LevelOfDetailQualitySettings")]
[SettingsUIDisableByCondition(typeof(LevelOfDetailQualitySettings), "IsOptionsDisabled")]
public class LevelOfDetailQualitySettings : QualitySetting<LevelOfDetailQualitySettings>
{
	[SettingsUISlider(min = 10f, max = 100f, step = 1f, unit = "percentage", scalarMultiplier = 100f)]
	public float levelOfDetail { get; set; }

	public bool lodCrossFade { get; set; }

	[SettingsUISlider(min = 512f, max = 16384f, step = 256f, unit = "integer")]
	public int maxLightCount { get; set; }

	[SettingsUISlider(min = 128f, max = 4096f, step = 64f, unit = "dataMegabytes")]
	public int meshMemoryBudget { get; set; }

	public bool strictMeshMemory { get; set; }

	private static LevelOfDetailQualitySettings highQuality => new LevelOfDetailQualitySettings
	{
		levelOfDetail = 0.7f,
		lodCrossFade = true,
		maxLightCount = 8192,
		meshMemoryBudget = 2048,
		strictMeshMemory = false
	};

	private static LevelOfDetailQualitySettings mediumQuality => new LevelOfDetailQualitySettings
	{
		levelOfDetail = 0.5f,
		lodCrossFade = true,
		maxLightCount = 4096,
		meshMemoryBudget = 1024,
		strictMeshMemory = false
	};

	private static LevelOfDetailQualitySettings lowQuality => new LevelOfDetailQualitySettings
	{
		levelOfDetail = 0.35f,
		lodCrossFade = true,
		maxLightCount = 2048,
		meshMemoryBudget = 512,
		strictMeshMemory = false
	};

	private static LevelOfDetailQualitySettings veryLowQuality => new LevelOfDetailQualitySettings
	{
		levelOfDetail = 0.25f,
		lodCrossFade = false,
		maxLightCount = 1024,
		meshMemoryBudget = 256,
		strictMeshMemory = true
	};

	static LevelOfDetailQualitySettings()
	{
		QualitySetting<LevelOfDetailQualitySettings>.RegisterSetting(Level.VeryLow, veryLowQuality);
		QualitySetting<LevelOfDetailQualitySettings>.RegisterSetting(Level.Low, lowQuality);
		QualitySetting<LevelOfDetailQualitySettings>.RegisterSetting(Level.Medium, mediumQuality);
		QualitySetting<LevelOfDetailQualitySettings>.RegisterSetting(Level.High, highQuality);
	}

	public LevelOfDetailQualitySettings()
	{
	}

	public LevelOfDetailQualitySettings(Level quality)
	{
		SetLevel(quality, apply: false);
	}

	public override void Apply()
	{
		base.Apply();
		RenderingSystem renderingSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<RenderingSystem>();
		if (renderingSystem != null)
		{
			renderingSystem.levelOfDetail = levelOfDetail;
			renderingSystem.lodCrossFade = lodCrossFade;
			renderingSystem.maxLightCount = maxLightCount;
		}
		BatchMeshSystem batchMeshSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<BatchMeshSystem>();
		if (batchMeshSystem != null)
		{
			batchMeshSystem.memoryBudget = (ulong)meshMemoryBudget * 1048576uL;
			batchMeshSystem.strictMemoryBudget = strictMeshMemory;
		}
	}
}
