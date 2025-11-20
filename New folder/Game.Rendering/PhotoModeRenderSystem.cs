using System.Collections.Generic;
using Colossal.Collections.Generic;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Rendering.CinematicCamera;
using Game.Simulation;
using Game.UI.InGame;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Rendering;

public class PhotoModeRenderSystem : GameSystemBase
{
	private static readonly string[] kApertureFormatNames = new string[14]
	{
		"8mm", "Super 8mm", "16mm", "Super 16mm", "35mm 2-perf", "35mm Academy", "Super-35", "35mm TV Projection", "35mm Full Aperture", "35mm 1.85 Projection",
		"35mm Anamorphic", "65mm ALEXA", "70mm", "70mm IMAX"
	};

	private static readonly Vector2[] kApertureFormatValues = new Vector2[14]
	{
		new Vector2(4.8f, 3.5f),
		new Vector2(5.79f, 4.01f),
		new Vector2(10.26f, 7.49f),
		new Vector2(12.522f, 7.417f),
		new Vector2(21.95f, 9.35f),
		new Vector2(21.946f, 16.002f),
		new Vector2(24.89f, 18.66f),
		new Vector2(20.726f, 15.545f),
		new Vector2(24.892f, 18.669f),
		new Vector2(20.955f, 11.328f),
		new Vector2(21.946f, 18.593f),
		new Vector2(54.12f, 25.59f),
		new Vector2(52.476f, 23.012f),
		new Vector2(70.41f, 52.63f)
	};

	private const string kSensorTypePreset = "SensorTypePreset";

	private const string kCameraApertureShape = "CameraApertureShape";

	private const string kCameraBody = "CameraBody";

	private const string kCameraLens = "CameraLens";

	private const string kCamera = "Camera";

	private const string kColorGrading = "Color";

	private const string kLens = "Lens";

	private const string kWeather = "Weather";

	private const string kEnvironment = "Environment";

	private Volume m_CameraControlVolume;

	private ColorAdjustments m_ColorAdjustments;

	private WhiteBalance m_WhiteBalance;

	private PaniniProjection m_PaniniProjection;

	private Vignette m_Vignette;

	private FilmGrain m_FilmGrain;

	private ShadowsMidtonesHighlights m_ShadowsMidtonesHighlights;

	private Bloom m_Bloom;

	private MotionBlur m_MotionBlur;

	private DepthOfField m_DepthOfField;

	private CloudLayer m_DistanceClouds;

	private VolumetricClouds m_VolumetricClouds;

	private Fog m_Fog;

	private PhysicallyBasedSky m_Sky;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private PlanetarySystem m_PlanetarySystem;

	private ClimateSystem m_ClimateSystem;

	private SimulationSystem m_SimulationSystem;

	private OverridableLensProperty<float> focalLength;

	private OverridableLensProperty<Vector2> sensorSize;

	private OverridableLensProperty<float> aperture;

	private OverridableLensProperty<int> iso;

	private OverridableLensProperty<float> shutterSpeed;

	private OverridableLensProperty<Camera.GateFitMode> gateFitMode;

	private OverridableLensProperty<int> bladeCount;

	private OverridableLensProperty<Vector2> curvature;

	private OverridableLensProperty<float> barrelClipping;

	private OverridableLensProperty<float> anamorphism;

	private OverridableLensProperty<float> focusDistance;

	private OverridableLensProperty<Vector2> lensShift;

	private bool m_Active;

	private List<PhotoModeUIPreset> m_Presets = new List<PhotoModeUIPreset>();

	public OrderedDictionary<string, PhotoModeProperty> photoModeProperties { get; private set; }

	public IReadOnlyCollection<PhotoModeUIPreset> presets => m_Presets;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		base.Enabled = false;
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_CameraControlVolume = VolumeHelper.CreateVolume("CinematicControlVolume", 2000);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_ColorAdjustments);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_WhiteBalance);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_PaniniProjection);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_ShadowsMidtonesHighlights);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_Vignette);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_Bloom);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_MotionBlur);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_DepthOfField);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_DistanceClouds);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_VolumetricClouds);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_Fog);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_Sky);
		m_Vignette.mode.Override(VignetteMode.Procedural);
		VolumeHelper.GetOrCreateVolumeComponent(m_CameraControlVolume, ref m_FilmGrain);
		m_CameraControlVolume.weight = 0f;
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PlanetarySystem = base.World.GetOrCreateSystemManaged<PlanetarySystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		focalLength = new OverridableLensProperty<float>(m_CameraUpdateSystem, delegate(IGameCameraController c, float v)
		{
			c.lens.FieldOfView = v;
		}, (IGameCameraController c) => c.lens.FieldOfView);
		sensorSize = new OverridableLensProperty<Vector2>(m_CameraUpdateSystem, delegate(IGameCameraController c, Vector2 v)
		{
			c.lens.SensorSize = v;
		}, (IGameCameraController c) => c.lens.SensorSize);
		aperture = new OverridableLensProperty<float>(m_CameraUpdateSystem, delegate(IGameCameraController c, float v)
		{
			c.lens.Aperture = v;
		}, (IGameCameraController c) => c.lens.Aperture);
		iso = new OverridableLensProperty<int>(m_CameraUpdateSystem, delegate(IGameCameraController c, int v)
		{
			c.lens.Iso = v;
		}, (IGameCameraController c) => c.lens.Iso);
		shutterSpeed = new OverridableLensProperty<float>(m_CameraUpdateSystem, delegate(IGameCameraController c, float v)
		{
			c.lens.ShutterSpeed = v;
		}, (IGameCameraController c) => c.lens.ShutterSpeed);
		gateFitMode = new OverridableLensProperty<Camera.GateFitMode>(m_CameraUpdateSystem, delegate(IGameCameraController c, Camera.GateFitMode v)
		{
			c.lens.GateFit = v;
		}, (IGameCameraController c) => c.lens.GateFit);
		bladeCount = new OverridableLensProperty<int>(m_CameraUpdateSystem, delegate(IGameCameraController c, int v)
		{
			c.lens.BladeCount = v;
		}, (IGameCameraController c) => c.lens.BladeCount);
		curvature = new OverridableLensProperty<Vector2>(m_CameraUpdateSystem, delegate(IGameCameraController c, Vector2 v)
		{
			c.lens.Curvature = v;
		}, (IGameCameraController c) => c.lens.Curvature);
		barrelClipping = new OverridableLensProperty<float>(m_CameraUpdateSystem, delegate(IGameCameraController c, float v)
		{
			c.lens.BarrelClipping = v;
		}, (IGameCameraController c) => c.lens.BarrelClipping);
		anamorphism = new OverridableLensProperty<float>(m_CameraUpdateSystem, delegate(IGameCameraController c, float v)
		{
			c.lens.Anamorphism = v;
		}, (IGameCameraController c) => c.lens.Anamorphism);
		focusDistance = new OverridableLensProperty<float>(m_CameraUpdateSystem, delegate(IGameCameraController c, float v)
		{
			c.lens.FocusDistance = v;
		}, (IGameCameraController c) => c.lens.FocusDistance);
		lensShift = new OverridableLensProperty<Vector2>(m_CameraUpdateSystem, delegate(IGameCameraController c, Vector2 v)
		{
			c.lens.LensShift = v;
		}, (IGameCameraController c) => c.lens.LensShift);
		InitializeProperties();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		focalLength.Sync();
		sensorSize.Sync();
		aperture.Sync();
		iso.Sync();
		shutterSpeed.Sync();
		gateFitMode.Sync();
		bladeCount.Sync();
		curvature.Sync();
		barrelClipping.Sync();
		anamorphism.Sync();
		focusDistance.Sync();
		lensShift.Sync();
	}

	public void Enable(bool enabled)
	{
		m_Active = enabled;
		if (m_Active)
		{
			base.Enabled = enabled;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		float weight;
		CameraBlend blendWeight = m_CameraUpdateSystem.GetBlendWeight(out weight);
		if (blendWeight == CameraBlend.ToCinematicCamera)
		{
			m_CameraControlVolume.weight = weight;
		}
		if (blendWeight == CameraBlend.FromCinematicCamera)
		{
			m_CameraControlVolume.weight = 1f - weight;
		}
		if (blendWeight == CameraBlend.None)
		{
			if (m_Active)
			{
				m_CameraControlVolume.weight = 1f;
				return;
			}
			m_CameraControlVolume.weight = 0f;
			base.Enabled = false;
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		VolumeHelper.DestroyVolume(m_CameraControlVolume);
		base.OnDestroy();
	}

	public void AddProperty(PhotoModeProperty property)
	{
		if (property != null)
		{
			if (!photoModeProperties.ContainsKey(property.id))
			{
				photoModeProperties.Add(property.id, property);
			}
			else
			{
				COSystemBase.baseLog.WarnFormat("PhotoModeProperty id {0} already exists", property.id);
			}
		}
	}

	public void AddProperty(PhotoModeProperty[] property)
	{
		foreach (PhotoModeProperty property2 in property)
		{
			AddProperty(property2);
		}
	}

	private void InitializeProperties()
	{
		photoModeProperties = new OrderedDictionary<string, PhotoModeProperty>();
		AddProperty(PhotoModeUtils.GroupTitle("Camera", "CameraBody"));
		PhotoModeProperty[] array;
		AddProperty(array = PhotoModeUtils.BindProperty("Camera", () => sensorSize, 0.1f, 1000f));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => iso, 200));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => shutterSpeed, 0.000125f, 30f));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => gateFitMode));
		AddProperty(new PhotoModeProperty
		{
			id = "Camera collision",
			group = "Camera",
			setValue = delegate(float value)
			{
				bool collisionsEnabled = PhotoModeUtils.FloatToBoolean(value);
				if (m_CameraUpdateSystem.cinematicCameraController != null)
				{
					m_CameraUpdateSystem.cinematicCameraController.collisionsEnabled = collisionsEnabled;
				}
				if (m_CameraUpdateSystem.orbitCameraController != null)
				{
					m_CameraUpdateSystem.orbitCameraController.collisionsEnabled = collisionsEnabled;
				}
			},
			getValue = delegate
			{
				bool flag = false;
				if (m_CameraUpdateSystem.cinematicCameraController != null)
				{
					flag |= m_CameraUpdateSystem.cinematicCameraController.collisionsEnabled;
				}
				if (m_CameraUpdateSystem.orbitCameraController != null)
				{
					flag |= m_CameraUpdateSystem.orbitCameraController.collisionsEnabled;
				}
				return PhotoModeUtils.BooleanToFloat(flag);
			},
			reset = delegate
			{
				if (m_CameraUpdateSystem.cinematicCameraController != null)
				{
					m_CameraUpdateSystem.cinematicCameraController.collisionsEnabled = false;
				}
				if (m_CameraUpdateSystem.orbitCameraController != null)
				{
					m_CameraUpdateSystem.orbitCameraController.collisionsEnabled = false;
				}
			},
			overrideControl = PhotoModeProperty.OverrideControl.Checkbox
		});
		AddProperty(PhotoModeUtils.GroupTitle("Camera", "CameraLens"));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => focalLength, MinFocalLength, MaxFocalLength, FieldOfViewToFocalLength, FocalLengthToFieldOfView));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => lensShift));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => aperture, 0.7f, 32f));
		AddProperty(PhotoModeUtils.GroupTitle("Camera", "CameraApertureShape"));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => bladeCount, 3, 11));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => curvature, 0.7f, 32f));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => barrelClipping, 0f, 1f));
		AddProperty(PhotoModeUtils.BindProperty("Camera", () => anamorphism, -1f, 1f));
		AddProperty(new PhotoModeProperty
		{
			id = "Roll",
			group = "Camera",
			setValue = delegate(float value)
			{
				if (m_CameraUpdateSystem.cinematicCameraController != null)
				{
					m_CameraUpdateSystem.cinematicCameraController.dutch = value;
				}
			},
			getValue = () => (m_CameraUpdateSystem.cinematicCameraController != null) ? m_CameraUpdateSystem.cinematicCameraController.dutch : 0f,
			min = () => -45f,
			max = () => 45f,
			reset = delegate
			{
				if (m_CameraUpdateSystem.cinematicCameraController != null)
				{
					m_CameraUpdateSystem.cinematicCameraController.dutch = 0f;
				}
			}
		});
		AddProperty(PhotoModeUtils.GroupTitle("Lens", m_DepthOfField.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_DepthOfField.focusMode));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_DepthOfField.focusDistance, () => m_DepthOfField.IsActive() && m_DepthOfField.focusMode == DepthOfFieldMode.UsePhysicalCamera));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_DepthOfField.nearFocusStart, () => m_DepthOfField.IsActive() && m_DepthOfField.focusMode == DepthOfFieldMode.Manual));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_DepthOfField.nearFocusEnd, () => m_DepthOfField.IsActive() && m_DepthOfField.focusMode == DepthOfFieldMode.Manual));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_DepthOfField.farFocusStart, () => m_DepthOfField.IsActive() && m_DepthOfField.focusMode == DepthOfFieldMode.Manual));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_DepthOfField.farFocusEnd, () => m_DepthOfField.IsActive() && m_DepthOfField.focusMode == DepthOfFieldMode.Manual));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_DepthOfField.m_NearMaxBlur, () => m_DepthOfField.IsActive() && m_DepthOfField.focusMode != DepthOfFieldMode.Off));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_DepthOfField.m_FarMaxBlur, () => m_DepthOfField.IsActive() && m_DepthOfField.focusMode != DepthOfFieldMode.Off));
		AddProperty(PhotoModeUtils.GroupTitle("Lens", m_MotionBlur.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_MotionBlur.intensity));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_MotionBlur.minimumVelocity));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_MotionBlur.maximumVelocity));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_MotionBlur.depthComparisonExtent));
		AddProperty(PhotoModeUtils.GroupTitle("Lens", m_Bloom.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_Bloom.threshold));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_Bloom.intensity));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_Bloom.scatter));
		AddProperty(PhotoModeUtils.GroupTitle("Lens", m_Vignette.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_Vignette.intensity));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_Vignette.color));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_Vignette.center, 0f, 1f));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_Vignette.smoothness));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_Vignette.roundness));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_Vignette.rounded));
		AddProperty(PhotoModeUtils.GroupTitle("Lens", m_FilmGrain.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_FilmGrain.type));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_FilmGrain.intensity));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_FilmGrain.response));
		AddProperty(PhotoModeUtils.GroupTitle("Lens", m_PaniniProjection.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_PaniniProjection.distance));
		AddProperty(PhotoModeUtils.BindProperty("Lens", () => m_PaniniProjection.cropToFit));
		AddProperty(PhotoModeUtils.GroupTitle("Color", m_ColorAdjustments.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Color", () => m_ColorAdjustments.postExposure));
		AddProperty(PhotoModeUtils.BindProperty("Color", () => m_ColorAdjustments.contrast));
		AddProperty(PhotoModeUtils.BindProperty("Color", () => m_ColorAdjustments.colorFilter));
		AddProperty(PhotoModeUtils.BindProperty("Color", () => m_ColorAdjustments.hueShift));
		AddProperty(PhotoModeUtils.BindProperty("Color", () => m_ColorAdjustments.saturation));
		AddProperty(PhotoModeUtils.GroupTitle("Color", m_WhiteBalance.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Color", () => m_WhiteBalance.temperature));
		AddProperty(PhotoModeUtils.BindProperty("Color", () => m_WhiteBalance.tint));
		AddProperty(PhotoModeUtils.GroupTitle("Color", m_ShadowsMidtonesHighlights.GetType().Name));
		AddProperty(PhotoModeUtils.BindPropertyW("Color", () => m_ShadowsMidtonesHighlights.shadows, -1f, 1f));
		AddProperty(PhotoModeUtils.BindPropertyW("Color", () => m_ShadowsMidtonesHighlights.midtones, -1f, 1f));
		AddProperty(PhotoModeUtils.BindPropertyW("Color", () => m_ShadowsMidtonesHighlights.highlights, -1f, 1f));
		AddProperty(PhotoModeUtils.GroupTitle("Weather", m_DistanceClouds.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.opacity));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.layerA.altitude));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.layerA.tint));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.layerA.exposure));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.layerA.rotation));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.layerA.thickness));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.layerA.opacityR));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.layerA.opacityG));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.layerA.opacityB));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_DistanceClouds.layerA.opacityA));
		AddProperty(PhotoModeUtils.GroupTitle("Weather", m_VolumetricClouds.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.densityMultiplier));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.shapeFactor));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.shapeScale));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.shapeOffset));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.erosionFactor));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.erosionScale));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.erosionNoiseType));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.bottomAltitude));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.altitudeRange));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.numPrimarySteps));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.numLightSteps));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.sunLightDimmer));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.erosionOcclusion));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.scatteringTint));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.powderEffectIntensity));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.multiScattering));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_VolumetricClouds.shadowOpacity));
		AddProperty(PhotoModeUtils.GroupTitle("Weather", m_Fog.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Fog.meanFreePath));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Fog.baseHeight));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Fog.maximumHeight));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Fog.maxFogDistance));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Fog.tint));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Fog.albedo));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Fog.depthExtent));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Fog.anisotropy));
		AddProperty(PhotoModeUtils.GroupTitle("Weather", m_Sky.GetType().Name));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.auroraBorealisEmissionMultiplier));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.airMaximumAltitude));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.airDensityR));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.airDensityG));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.airDensityB));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.airTint));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.aerosolMaximumAltitude));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.aerosolDensity));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.aerosolTint));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.aerosolAnisotropy));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.colorSaturation));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.alphaSaturation));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.alphaMultiplier));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.horizonTint));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.horizonZenithShift));
		AddProperty(PhotoModeUtils.BindProperty("Weather", () => m_Sky.zenithTint));
		AddProperty(new PhotoModeProperty
		{
			id = "Time of Day",
			group = "Environment",
			setValue = delegate(float value)
			{
				m_PlanetarySystem.time = value;
				m_PlanetarySystem.overrideTime = true;
				m_PlanetarySystem.Update();
			},
			getValue = () => m_PlanetarySystem.time,
			min = () => 0f,
			max = () => 24f,
			setEnabled = delegate(bool enabled)
			{
				m_PlanetarySystem.overrideTime = enabled;
				m_PlanetarySystem.Update();
			},
			isEnabled = () => m_PlanetarySystem.overrideTime
		});
		AddProperty(new PhotoModeProperty
		{
			id = "Simulation Speed",
			group = "Environment",
			setValue = delegate(float value)
			{
				m_SimulationSystem.selectedSpeed = value;
			},
			getValue = () => m_SimulationSystem.selectedSpeed,
			min = () => 0f,
			max = () => 8f,
			reset = delegate
			{
				m_SimulationSystem.selectedSpeed = 0f;
			}
		});
		AddPreset(PhotoModeUtils.CreatePreset("SensorTypePreset", array[0], array, kApertureFormatNames, kApertureFormatValues));
		float FieldOfViewToFocalLength(float v)
		{
			return Camera.FieldOfViewToFocalLength(v, sensorSize.currentValue.y);
		}
		float FocalLengthToFieldOfView(float v)
		{
			return Mathf.Clamp(Camera.FocalLengthToFieldOfView(Mathf.Max(v, 0.0001f), sensorSize.currentValue.y), 1f, 179f);
		}
		float MaxFieldOfViewToFocalLength()
		{
			return FieldOfViewToFocalLength(179f);
		}
		float MaxFocalLength()
		{
			if (!(MinFieldOfViewToFocalLength() > MaxFieldOfViewToFocalLength()))
			{
				return MaxFieldOfViewToFocalLength();
			}
			return MinFieldOfViewToFocalLength();
		}
		float MinFieldOfViewToFocalLength()
		{
			return FieldOfViewToFocalLength(1f);
		}
		float MinFocalLength()
		{
			if (!(MinFieldOfViewToFocalLength() > MaxFieldOfViewToFocalLength()))
			{
				return MinFieldOfViewToFocalLength();
			}
			return MaxFieldOfViewToFocalLength();
		}
	}

	private void AddPreset(PhotoModeUIPreset preset)
	{
		m_Presets.Add(preset);
	}

	public void DisableAllCameraProperties()
	{
		foreach (KeyValuePair<string, PhotoModeProperty> photoModeProperty in photoModeProperties)
		{
			photoModeProperty.Value.setEnabled?.Invoke(obj: false);
		}
	}

	[Preserve]
	public PhotoModeRenderSystem()
	{
	}
}
