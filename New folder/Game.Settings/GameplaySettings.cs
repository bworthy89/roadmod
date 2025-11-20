using Colossal.IO.AssetDatabase;

namespace Game.Settings;

[FileLocation("Settings")]
public class GameplaySettings : Setting
{
	public const string kName = "Gameplay";

	private CameraController m_CameraController;

	public bool edgeScrolling { get; set; }

	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(GameplaySettings), "edgeScrolling", true)]
	public float edgeScrollingSensitivity { get; set; }

	public bool dayNightVisual { get; set; }

	public bool pausedAfterLoading { get; set; }

	public bool showTutorials { get; set; }

	[SettingsUIButton]
	[SettingsUIConfirmation(null, null)]
	public bool resetTutorials
	{
		set
		{
			SharedSettings.instance.userState.ResetTutorials();
		}
	}

	public GameplaySettings()
	{
		SetDefaults();
	}

	public override void SetDefaults()
	{
		edgeScrolling = true;
		edgeScrollingSensitivity = 1f;
		dayNightVisual = true;
		pausedAfterLoading = false;
		showTutorials = true;
	}

	public override void Apply()
	{
		base.Apply();
		if (m_CameraController == null)
		{
			TryGetGameplayCameraController(ref m_CameraController);
		}
		if (m_CameraController != null)
		{
			m_CameraController.edgeScrolling = edgeScrolling;
			m_CameraController.edgeScrollingSensitivity = edgeScrollingSensitivity;
		}
	}
}
