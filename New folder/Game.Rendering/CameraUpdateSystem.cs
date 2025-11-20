#define UNITY_ASSERTIONS
using System.Linq;
using System.Runtime.CompilerServices;
using Cinemachine;
using Colossal;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Input;
using Game.Settings;
using Game.Simulation;
using Unity.Assertions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class CameraUpdateSystem : GameSystemBase
{
	private RaycastSystem m_RaycastSystem;

	private WaterSystem m_WaterSystem;

	private Volume m_Volume;

	private DepthOfField m_DepthOfField;

	private HDShadowSettings m_ShadowSettings;

	private float4 m_StoredShadowSplitsAndDistance;

	private float4 m_StoredShadowBorders;

	public bool enableDebugGizmos;

	private InputActivator[] m_CameraActionActivators;

	private InputBarrier[] m_CameraActionBarriers;

	public Viewer activeViewer { get; private set; }

	public CameraController gamePlayController { get; set; }

	public CinematicCameraController cinematicCameraController { get; set; }

	public OrbitCameraController orbitCameraController { get; set; }

	public Camera activeCamera
	{
		get
		{
			return activeViewer?.camera;
		}
		set
		{
			if (value == null)
			{
				if (activeViewer != null)
				{
					activeViewer = null;
					COSystemBase.baseLog.DebugFormat("Resetting activeViewer to null");
				}
			}
			else
			{
				activeViewer = new Viewer(value, m_WaterSystem);
				COSystemBase.baseLog.DebugFormat("Setting activeViewer with {0}", value.name);
			}
		}
	}

	public float nearClipPlane { get; private set; }

	public float3 position { get; private set; }

	public float3 direction { get; private set; }

	public float zoom { get; private set; }

	public IGameCameraController activeCameraController
	{
		get
		{
			if (gamePlayController != null && gamePlayController.controllerEnabled)
			{
				Assert.IsFalse(cinematicCameraController != null && cinematicCameraController.controllerEnabled);
				Assert.IsFalse(orbitCameraController != null && orbitCameraController.controllerEnabled);
				return gamePlayController;
			}
			if (cinematicCameraController != null && cinematicCameraController.controllerEnabled)
			{
				Assert.IsFalse(gamePlayController != null && gamePlayController.controllerEnabled);
				Assert.IsFalse(orbitCameraController != null && orbitCameraController.controllerEnabled);
				return cinematicCameraController;
			}
			if (orbitCameraController != null && orbitCameraController.controllerEnabled)
			{
				Assert.IsFalse(gamePlayController != null && gamePlayController.controllerEnabled);
				Assert.IsFalse(cinematicCameraController != null && cinematicCameraController.controllerEnabled);
				return orbitCameraController;
			}
			return null;
		}
		set
		{
			if (gamePlayController != null && value != gamePlayController)
			{
				gamePlayController.controllerEnabled = false;
			}
			if (cinematicCameraController != null && value != cinematicCameraController)
			{
				cinematicCameraController.controllerEnabled = false;
			}
			if (orbitCameraController != null && value != orbitCameraController)
			{
				orbitCameraController.controllerEnabled = false;
			}
			if (value != null)
			{
				value.controllerEnabled = true;
			}
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		activeCamera = Camera.main;
	}

	public CameraBlend GetBlendWeight(out float weight)
	{
		if (CinemachineCore.Instance.BrainCount > 0)
		{
			CinemachineBrain activeBrain = CinemachineCore.Instance.GetActiveBrain(0);
			if (activeBrain != null && activeBrain.IsBlending)
			{
				CinemachineBlend activeBlend = activeBrain.ActiveBlend;
				if (activeBlend.IsValid && !activeBlend.IsComplete)
				{
					weight = activeBlend.BlendWeight;
					if (activeBlend.CamB == cinematicCameraController.virtualCamera)
					{
						return CameraBlend.ToCinematicCamera;
					}
					if (activeBlend.CamA == cinematicCameraController.virtualCamera)
					{
						return CameraBlend.FromCinematicCamera;
					}
				}
			}
		}
		weight = 1f;
		return CameraBlend.None;
	}

	public bool TryGetViewer(out Viewer viewer)
	{
		viewer = activeViewer;
		return activeViewer != null;
	}

	public bool TryGetLODParameters(out LODParameters lodParameters)
	{
		if (activeViewer != null)
		{
			return activeViewer.TryGetLODParameters(out lodParameters);
		}
		lodParameters = default(LODParameters);
		return false;
	}

	private bool CheckOrCacheViewer()
	{
		if (activeViewer != null && activeViewer.camera != null)
		{
			nearClipPlane = activeViewer.nearClipPlane;
			position = activeViewer.position;
			direction = activeViewer.forward;
			zoom = activeCameraController?.zoom ?? zoom;
			activeViewer.Raycast(m_RaycastSystem, enableDebugGizmos);
			return true;
		}
		nearClipPlane = 0f;
		position = float3.zero;
		direction = new float3(0f, 0f, 1f);
		activeCamera = null;
		zoom = 0f;
		return false;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RaycastSystem = base.World.GetOrCreateSystemManaged<RaycastSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_Volume = VolumeHelper.CreateVolume("CameraControllerVolume", 51);
		VolumeHelper.GetOrCreateVolumeComponent(m_Volume, ref m_DepthOfField);
		VolumeHelper.GetOrCreateVolumeComponent(m_Volume, ref m_ShadowSettings);
		ProxyActionMap proxyActionMap = InputManager.instance.FindActionMap("Camera");
		m_CameraActionActivators = proxyActionMap.actions.Values.Select((ProxyAction a) => new InputActivator(ignoreIsBuiltIn: true, "CameraUpdateSystem(" + a.name + ")", a)).ToArray();
		m_CameraActionBarriers = proxyActionMap.actions.Values.Select((ProxyAction a) => new InputBarrier("CameraUpdateSystem(" + a.name + ")", a, InputManager.DeviceType.Mouse)).ToArray();
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		activeCameraController?.ResetCamera();
	}

	private void UpdateDepthOfField(float distance)
	{
		Game.Settings.GraphicsSettings graphicsSettings = SharedSettings.instance?.graphics;
		if (graphicsSettings != null)
		{
			if (graphicsSettings.depthOfFieldMode == Game.Settings.GraphicsSettings.DepthOfFieldMode.TiltShift)
			{
				m_DepthOfField.focusMode.Override(DepthOfFieldMode.Manual);
				m_DepthOfField.nearFocusStart.Override(distance - distance * graphicsSettings.tiltShiftNearStart);
				m_DepthOfField.nearFocusEnd.Override(distance - distance * graphicsSettings.tiltShiftNearEnd);
				m_DepthOfField.farFocusStart.Override(distance + distance * graphicsSettings.tiltShiftFarStart);
				m_DepthOfField.farFocusEnd.Override(distance + distance * graphicsSettings.tiltShiftFarEnd);
			}
			else if (graphicsSettings.depthOfFieldMode == Game.Settings.GraphicsSettings.DepthOfFieldMode.Physical)
			{
				m_DepthOfField.focusMode.Override(DepthOfFieldMode.UsePhysicalCamera);
				m_DepthOfField.focusDistanceMode.Override(FocusDistanceMode.Volume);
				m_DepthOfField.focusDistance.Override(distance);
			}
			else
			{
				m_DepthOfField.focusMode.Override(DepthOfFieldMode.Off);
			}
		}
	}

	private void UpdateShadows(Viewer viewer)
	{
		Camera camera = viewer.camera;
		if (!camera)
		{
			return;
		}
		if (math.lengthsq(m_StoredShadowSplitsAndDistance) == 0f)
		{
			HDCamera orCreate = HDCamera.GetOrCreate(camera);
			if (orCreate != null)
			{
				HDShadowSettings component = orCreate.volumeStack.GetComponent<HDShadowSettings>();
				float value = component.maxShadowDistance.value;
				float[] cascadeShadowSplits = component.cascadeShadowSplits;
				float[] cascadeShadowBorders = component.cascadeShadowBorders;
				m_StoredShadowSplitsAndDistance = new float4(cascadeShadowSplits[0] * value, cascadeShadowSplits[1] * value, cascadeShadowSplits[2] * value, value);
				m_StoredShadowBorders = new float4(cascadeShadowBorders[0], cascadeShadowBorders[1], cascadeShadowBorders[2], cascadeShadowBorders[3]);
			}
		}
		if (!viewer.shadowsAdjustFarDistance)
		{
			m_ShadowSettings.maxShadowDistance.overrideState = false;
			m_ShadowSettings.cascadeShadowSplit0.overrideState = false;
			m_ShadowSettings.cascadeShadowSplit1.overrideState = false;
			m_ShadowSettings.cascadeShadowSplit2.overrideState = false;
			m_ShadowSettings.cascadeShadowBorder0.overrideState = false;
			m_ShadowSettings.cascadeShadowBorder1.overrideState = false;
			m_ShadowSettings.cascadeShadowBorder2.overrideState = false;
			m_ShadowSettings.cascadeShadowBorder3.overrideState = false;
			return;
		}
		float w = m_StoredShadowSplitsAndDistance.w;
		float y = math.lerp(viewer.viewerDistances.farthestSurface, viewer.viewerDistances.maxDistanceToSeaLevel, 0.2f) * 1.1f;
		w = math.min(w, y);
		float x = m_StoredShadowSplitsAndDistance.x;
		float y2 = m_StoredShadowSplitsAndDistance.y;
		float z = m_StoredShadowSplitsAndDistance.z;
		x = math.clamp(x, 15f, w * 0.15f);
		y2 = math.clamp(y2, 45f, w * 0.3f);
		z = math.clamp(z, 135f, w * 0.6f);
		float ground = viewer.viewerDistances.ground;
		x = math.min(x, ground * 5f);
		y2 = math.min(y2, ground * 30f);
		z = math.min(z, ground * 200f);
		w = math.max(w, z * 1.2f);
		m_ShadowSettings.maxShadowDistance.Override(w);
		m_ShadowSettings.cascadeShadowSplit0.Override(x / w);
		m_ShadowSettings.cascadeShadowSplit1.Override(y2 / w);
		m_ShadowSettings.cascadeShadowSplit2.Override(z / w);
		m_ShadowSettings.cascadeShadowBorder0.Override(m_StoredShadowBorders.x);
		m_ShadowSettings.cascadeShadowBorder1.Override(m_StoredShadowBorders.y);
		m_ShadowSettings.cascadeShadowBorder2.Override(m_StoredShadowBorders.z);
		m_ShadowSettings.cascadeShadowBorder3.Override(m_StoredShadowBorders.w);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		VolumeHelper.DestroyVolume(m_Volume);
		if (gamePlayController != null)
		{
			gamePlayController.controllerEnabled = false;
		}
		if (cinematicCameraController != null)
		{
			cinematicCameraController.controllerEnabled = false;
		}
		if (orbitCameraController != null)
		{
			orbitCameraController.controllerEnabled = false;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool num = activeViewer != null && activeViewer.camera != null;
		float distance = 0f;
		if (num)
		{
			activeViewer.UpdateRaycast(base.EntityManager, m_RaycastSystem, base.CheckedStateRef.WorldUnmanaged.Time.DeltaTime);
			distance = activeViewer.viewerDistances.focus;
			UpdateShadows(activeViewer);
		}
		UpdateDepthOfField(distance);
		activeCameraController?.UpdateCamera();
		for (int i = 0; i < CinemachineCore.Instance.BrainCount; i++)
		{
			CinemachineCore.Instance.GetActiveBrain(i).ManualUpdate();
		}
		CheckOrCacheViewer();
		if (enableDebugGizmos)
		{
			Colossal.Gizmos.batcher.DrawWireSphere(activeViewer.position + activeViewer.forward * activeViewer.viewerDistances.focus, 0.1f, Color.red);
			Colossal.Gizmos.batcher.DrawWireSphere(activeViewer.position + activeViewer.forward * activeViewer.viewerDistances.focus, 1f, Color.blue);
		}
		RefreshInput();
	}

	private void RefreshInput()
	{
		InputActivator[] array = m_CameraActionActivators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = activeCameraController != null;
		}
		InputBarrier[] array2 = m_CameraActionBarriers;
		foreach (InputBarrier inputBarrier in array2)
		{
			if (activeCameraController == null)
			{
				inputBarrier.blocked = false;
			}
			else if (!InputManager.instance.mouseOverUI)
			{
				inputBarrier.blocked = false;
			}
			else if (inputBarrier.actions.All((ProxyAction a) => !a.IsInProgress()))
			{
				inputBarrier.blocked = true;
			}
		}
	}

	[Preserve]
	public CameraUpdateSystem()
	{
	}
}
