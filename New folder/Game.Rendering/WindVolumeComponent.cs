using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering;

public class WindVolumeComponent : VolumeComponent
{
	public ClampedFloatParameter windGlobalStrengthScale = new ClampedFloatParameter(1f, 0f, 3f);

	public ClampedFloatParameter windGlobalStrengthScale2 = new ClampedFloatParameter(1f, 0f, 3f);

	public ClampedFloatParameter windDirection = new ClampedFloatParameter(65f, 0f, 360f);

	public ClampedFloatParameter windDirectionVariance = new ClampedFloatParameter(25f, 0f, 90f);

	public ClampedFloatParameter windDirectionVariancePeriod = new ClampedFloatParameter(15f, 0.01f, 20f);

	public ClampedFloatParameter windParameterInterpolationDuration = new ClampedFloatParameter(0.5f, 0.0001f, 5f);

	[Header("Grass Base")]
	public ClampedFloatParameter windBaseStrength = new ClampedFloatParameter(15f, 0f, 75f);

	public ClampedFloatParameter windBaseStrengthOffset = new ClampedFloatParameter(0.25f, 0f, 3f);

	public ClampedFloatParameter windBaseStrengthPhase = new ClampedFloatParameter(3f, 0f, 10f);

	public ClampedFloatParameter windBaseStrengthPhase2 = new ClampedFloatParameter(0f, 0f, 10f);

	public ClampedFloatParameter windBaseStrengthVariancePeriod = new ClampedFloatParameter(10f, 0.01f, 20f);

	[Header("Grass Gust")]
	public ClampedFloatParameter windGustStrength = new ClampedFloatParameter(25f, 0f, 75f);

	public ClampedFloatParameter windGustStrengthOffset = new ClampedFloatParameter(1f, 0f, 5f);

	public ClampedFloatParameter windGustStrengthPhase = new ClampedFloatParameter(3f, 0f, 10f);

	public ClampedFloatParameter windGustStrengthPhase2 = new ClampedFloatParameter(3f, 0f, 10f);

	public ClampedFloatParameter windGustStrengthVariancePeriod = new ClampedFloatParameter(2f, 0.01f, 10f);

	public ClampedFloatParameter windGustInnerCosScale = new ClampedFloatParameter(2f, 0f, 5f);

	public AnimationCurveParameter windGustStrengthControl = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(10f, 1f)));

	[Header("Grass Flutter")]
	public ClampedFloatParameter windFlutterStrength = new ClampedFloatParameter(0.4f, 0f, 10f);

	public ClampedFloatParameter windFlutterGustStrength = new ClampedFloatParameter(0.2f, 0f, 10f);

	public ClampedFloatParameter windFlutterGustStrengthOffset = new ClampedFloatParameter(50f, 0f, 75f);

	public ClampedFloatParameter windFlutterGustStrengthScale = new ClampedFloatParameter(75f, 0f, 75f);

	public ClampedFloatParameter windFlutterGustVariancePeriod = new ClampedFloatParameter(0.25f, 0.01f, 2f);

	[Header("Tree Base")]
	public ClampedFloatParameter windTreeBaseStrength = new ClampedFloatParameter(0.25f, 0f, 10f);

	public ClampedFloatParameter windTreeBaseStrengthOffset = new ClampedFloatParameter(1f, 0f, 5f);

	public ClampedFloatParameter windTreeBaseStrengthPhase = new ClampedFloatParameter(0.5f, 0f, 2f);

	public ClampedFloatParameter windTreeBaseStrengthPhase2 = new ClampedFloatParameter(0f, 0f, 2f);

	public ClampedFloatParameter windTreeBaseStrengthVariancePeriod = new ClampedFloatParameter(6f, 0.01f, 20f);

	[Header("Tree Gust")]
	public ClampedFloatParameter windTreeGustStrength = new ClampedFloatParameter(1f, 0f, 10f);

	public ClampedFloatParameter windTreeGustStrengthOffset = new ClampedFloatParameter(1f, 0f, 5f);

	public ClampedFloatParameter windTreeGustStrengthPhase = new ClampedFloatParameter(2f, 0f, 10f);

	public ClampedFloatParameter windTreeGustStrengthPhase2 = new ClampedFloatParameter(3f, 0f, 10f);

	public ClampedFloatParameter windTreeGustStrengthVariancePeriod = new ClampedFloatParameter(4f, 0.01f, 10f);

	public ClampedFloatParameter windTreeGustInnerCosScale = new ClampedFloatParameter(2f, 0f, 5f);

	public AnimationCurveParameter windTreeGustStrengthControl = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(10f, 1f)));

	[Header("Tree Flutter")]
	public ClampedFloatParameter windTreeFlutterStrength = new ClampedFloatParameter(0.1f, 0f, 5f);

	public ClampedFloatParameter windTreeFlutterGustStrength = new ClampedFloatParameter(0.5f, 0f, 5f);

	public ClampedFloatParameter windTreeFlutterGustStrengthOffset = new ClampedFloatParameter(12.5f, 0f, 75f);

	public ClampedFloatParameter windTreeFlutterGustStrengthScale = new ClampedFloatParameter(25f, 0f, 75f);

	public ClampedFloatParameter windTreeFlutterGustVariancePeriod = new ClampedFloatParameter(0.1f, 0.01f, 2f);
}
