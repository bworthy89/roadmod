using Colossal.IO.AssetDatabase;
using Game.Audio;
using Game.Audio.Radio;
using UnityEngine;

namespace Game.Settings;

[FileLocation("Settings")]
public class RadioSettings : Setting
{
	private Radio m_Radio;

	[SettingsUIHidden]
	public bool enableSpectrum { get; set; }

	[SettingsUIHidden]
	public int spectrumNumSamples { get; set; }

	[SettingsUIHidden]
	public FFTWindow fftWindowType { get; set; }

	[SettingsUIHidden]
	public Radio.Spectrum.BandType bandType { get; set; }

	[SettingsUIHidden]
	public float equalizerBarSpacing { get; set; }

	[SettingsUIHidden]
	public float equalizerSidesPadding { get; set; }

	public RadioSettings()
	{
		SetDefaults();
	}

	public override void SetDefaults()
	{
		spectrumNumSamples = 1024;
		enableSpectrum = false;
		fftWindowType = FFTWindow.BlackmanHarris;
		bandType = Radio.Spectrum.BandType.TenBand;
		equalizerBarSpacing = 10.2f;
		equalizerSidesPadding = 4f;
	}

	public override void Apply()
	{
		base.Apply();
		if (m_Radio == null)
		{
			m_Radio = AudioManager.instance.radio;
		}
		if (m_Radio != null)
		{
			m_Radio.SetSpectrumSettings(enableSpectrum, spectrumNumSamples, fftWindowType, bandType, equalizerBarSpacing, equalizerSidesPadding);
		}
	}
}
