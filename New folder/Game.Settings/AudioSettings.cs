using Colossal.IO.AssetDatabase;
using Game.Audio;
using Game.Audio.Radio;

namespace Game.Settings;

[FileLocation("Settings")]
[SettingsUIGroupOrder(new string[] { "Main", "Radio", "Advanced" })]
public class AudioSettings : Setting
{
	public const string kName = "Audio";

	private AudioManager m_AudioManager;

	private Radio m_Radio;

	public const string kMainGroup = "Main";

	public const string kRadioGroup = "Radio";

	public const string kAdvancedGroup = "Advanced";

	[SettingsUISection("Main")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float masterVolume { get; set; }

	[SettingsUISection("Main")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float uiVolume { get; set; }

	[SettingsUISection("Main")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float menuVolume { get; set; }

	[SettingsUISection("Main")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float ingameVolume { get; set; }

	[SettingsUISection("Radio")]
	public bool radioActive { get; set; }

	[SettingsUISection("Radio")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float radioVolume { get; set; }

	[SettingsUIAdvanced]
	[SettingsUISection("Advanced")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float ambienceVolume { get; set; }

	[SettingsUIAdvanced]
	[SettingsUISection("Advanced")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float disastersVolume { get; set; }

	[SettingsUIAdvanced]
	[SettingsUISection("Advanced")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float worldVolume { get; set; }

	[SettingsUIAdvanced]
	[SettingsUISection("Advanced")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float audioGroupsVolume { get; set; }

	[SettingsUIAdvanced]
	[SettingsUISection("Advanced")]
	[SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = "percentage", scaleDragVolume = true, scalarMultiplier = 100f)]
	public float serviceBuildingsVolume { get; set; }

	[SettingsUIAdvanced]
	[SettingsUISection("Advanced")]
	[SettingsUISlider(min = 64f, max = 512f, step = 32f, unit = "dataMegabytes")]
	public int clipMemoryBudget { get; set; }

	public AudioSettings()
	{
		SetDefaults();
	}

	public override void SetDefaults()
	{
		masterVolume = 1f;
		uiVolume = 1f;
		menuVolume = 1f;
		ingameVolume = 1f;
		radioActive = true;
		radioVolume = 1f;
		ambienceVolume = 1f;
		disastersVolume = 1f;
		worldVolume = 1f;
		audioGroupsVolume = 1f;
		serviceBuildingsVolume = 1f;
		clipMemoryBudget = 256;
	}

	public override void Apply()
	{
		base.Apply();
		if (m_AudioManager == null)
		{
			m_AudioManager = AudioManager.instance;
		}
		if (m_Radio == null)
		{
			m_Radio = AudioManager.instance.radio;
		}
		if (m_AudioManager != null)
		{
			m_AudioManager.masterVolume = masterVolume;
			m_AudioManager.radioVolume = radioVolume;
			m_AudioManager.uiVolume = uiVolume;
			m_AudioManager.menuVolume = menuVolume;
			m_AudioManager.ingameVolume = ingameVolume;
			m_AudioManager.ambienceVolume = ambienceVolume;
			m_AudioManager.disastersVolume = disastersVolume;
			m_AudioManager.worldVolume = worldVolume;
			m_AudioManager.audioGroupsVolume = audioGroupsVolume;
			m_AudioManager.serviceBuildingsVolume = serviceBuildingsVolume;
		}
		if (m_Radio != null)
		{
			m_Radio.isActive = radioActive;
		}
		AudioManager.AudioSourcePool.memoryBudget = clipMemoryBudget * 1048576;
	}
}
