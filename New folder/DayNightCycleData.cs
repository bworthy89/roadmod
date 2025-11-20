using UnityEngine;

[CreateAssetMenu]
public class DayNightCycleData : ScriptableObject
{
	[Tooltip("Angle of the sun for the start of dawn (degrees)")]
	[Range(-10f, 45f)]
	public float DawnStartAngle = -0.1f;

	[Tooltip("Angle of the sun for the mid point of sunrise (degrees)")]
	[Range(0f, 45f)]
	public float SunriseMidpointAngle = 2f;

	[Tooltip("Angle of the sun for the end point of sunrise (degrees)")]
	[Range(0f, 45f)]
	public float SunriseEndAngle = 5f;

	[Tooltip("Angle of the sun for the start of sunset (degrees)")]
	[Range(0f, 45f)]
	public float SunsetStartAngle = 5f;

	[Tooltip("Angle of the sun for the mid point of sunset (degrees)")]
	[Range(0f, 45f)]
	public float SunsetMidpointAngle = 2f;

	[Tooltip("Angle of the sun for the end of dusk (degrees)")]
	[Range(-10f, 45f)]
	public float DuskEndAngle = -0.1f;

	[Tooltip("Maximum Exposure limit for the AutoExposure during the day")]
	public float DayExposureMax = 14f;

	[Tooltip("Minimum Exposure limit for the AutoExposure during the day")]
	public float DayExposureMin = 4f;

	[Tooltip("Maximum Exposure limit for the AutoExposure during the night")]
	public float NightExposureMax = 14f;

	[Tooltip("Minimum Exposure limit for the AutoExposure during the night close to the ground")]
	public float NightExposureLowMin = 2.5f;

	[Tooltip("Minimum Exposure limit for the AutoExposure during the night up in the sky (not implemented)")]
	public float NightExposureHighMin = 2f;

	[Tooltip("Height range for the AutoExposure to transisiton to Low to High (not implemented)")]
	public float NightExposureHeightRange = 10f;

	[Tooltip("Color of the Moon Light")]
	public Color MoonLightColor = new Color(0.7909992f, 0.8107967f, 91f / 106f);

	[Tooltip("Tints the shadows from the Moon")]
	public Color MoonShadowTint = new Color(0.1386911f, 0.1453638f, 0.1603774f);

	[Tooltip("Intensity of the Moon Light")]
	public float MoonIntensity = 10f;

	[Tooltip("Intensity of the Night Light (non shadowing and never gets below a certain point ot make sure theirs alway light even when the moon is out of view)")]
	public float NightLightIntensity = 1f;

	[Tooltip("Boost to the Nightlight for when the Moon is obscured")]
	public float NightLightObscuredIntensity = 4.5f;

	[Tooltip("Range the boost is applied")]
	public float NightLightObscuredRange = 0.2f;

	[Tooltip("Physical Sky, Zenith Tint for Day")]
	public Color DayZenithTint = new Color(1f, 1f, 1f);

	[Tooltip("Physical Sky, Horizon Tint for Day")]
	public Color DayHorizonTint = new Color(1f, 1f, 1f);

	[Tooltip("Physical Sky, Zenith Tint for Night")]
	public Color NightZenithTint = new Color(0.3867925f, 0.3867925f, 0.3867925f);

	[Tooltip("Physical Sky, Horizon Tint for Night")]
	public Color NightHorizonTint = new Color(43f / 106f, 43f / 106f, 43f / 106f);

	[Tooltip("Physical Sky, Exposure for sky during the Day (affects lighting)")]
	public float DaySkyExposure;

	[Tooltip("Physical Sky, Exposure for sky during the Night (affects lighting)")]
	public float NightSkyExposure = 1f;

	[Tooltip("Color Grading, enable the use of color filters")]
	public bool UseFilters = true;

	[Tooltip("Color Grading, Sunrise filter")]
	public Color SunriseColorFilter = new Color(1f, 1f, 1f);

	[Tooltip("Color Grading, Day filter")]
	public Color DayColorFilter = new Color(1f, 1f, 1f);

	[Tooltip("Color Grading, Sunset filter")]
	public Color SunsetColorFilter = new Color(1f, 1f, 1f);

	[Tooltip("Color Grading, Night filter")]
	public Color NightColorFilter = new Color(0.8470588f, 0.8392157f, 1f);

	[Tooltip("Color Grading, Day Contrast")]
	public float DayContrast;

	[Tooltip("Color Grading, Night Contrast")]
	public float NightContrast;

	[Tooltip("Color Grading, Sunrise or Sunset Contrast")]
	public float SunriseAndSunsetContrast;

	[Tooltip("Indirect Lighting, Night Diffuse Boost")]
	public float NightIndirectDiffuseMultiplier = 3f;

	[Tooltip("Indirect Lighting, Night Reflkective Boost")]
	public float NightIndirectReflectiveMultiplier = 1f;

	[Tooltip("Indirect Lighting, Night (Moon Obscured) Diffuse Boost")]
	public float NightObscuredIndirectDiffuseMultiplier = 5f;

	[Tooltip("Indirect Lighting, Night (Moon Obscured) Reflective Boost")]
	public float NightObscuredIndirectReflectiveMultiplier = 1f;

	[Tooltip("Tonemapping, Use LUTS?")]
	public bool UseLUT;

	[Tooltip("Tonemapping, Day LUT - If empty will just use the standard values")]
	public Texture3D DayLUT;

	[Tooltip("Tonemapping, Night LUT - If empty will just use the standard values")]
	public Texture3D NightLUT;

	[Tooltip("Tonemapping, Sunrise or Sunset LUT - If empty will just use the standard values")]
	public Texture3D SunriseAndSunsetLUT;

	[Tooltip("Tonemapping, contributuon the LUT applys to the game (all LUTS)")]
	public float LutContribution = 0.5f;
}
