using System;
using System.Collections.Generic;
using Cinemachine;
using Colossal.Mathematics;
using Game.Audio;
using Game.Input;
using Game.Rendering;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using Game.UI.InGame;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game;

public class CameraController : MonoBehaviour, IGameCameraController
{
	[SerializeField]
	private float3 m_Pivot;

	[SerializeField]
	private float2 m_Angle;

	[SerializeField]
	private float m_Zoom;

	[SerializeField]
	private Bounds1 m_ZoomRange = new Bounds1(10f, 10000f);

	[SerializeField]
	private Bounds1 m_MapTileToolZoomRange = new Bounds1(10f, 20000f);

	[SerializeField]
	private bool m_MapTileToolViewEnabled;

	[SerializeField]
	private float m_MapTileToolFOV;

	[SerializeField]
	private float m_MapTileToolFarclip;

	[SerializeField]
	private float3 m_MapTileToolPivot;

	[SerializeField]
	private float2 m_MapTileToolAngle;

	[SerializeField]
	private float m_MapTileToolZoom;

	[SerializeField]
	private float m_MapTileToolTransitionTime;

	[SerializeField]
	private float m_MoveSmoothing = 1E-06f;

	[SerializeField]
	private float m_CollisionSmoothing = 0.001f;

	private ProxyActionMap m_CameraMap;

	private ProxyAction m_MoveAction;

	private ProxyAction m_MoveFastAction;

	private ProxyAction m_RotateAction;

	private ProxyAction m_ZoomAction;

	private CinemachineVirtualCamera m_VCam;

	private float m_InitialFarClip;

	private float m_InitialFov;

	private float m_LastGameViewZoom;

	private float2 m_LastGameViewAngle;

	private float3 m_LastGameViewPivot;

	private float m_LastMapViewZoom;

	private float2 m_LastMapViewAngle;

	private float3 m_LastMapViewPivot;

	private float m_MapViewTimer;

	private AudioManager m_AudioManager;

	private CameraUpdateSystem m_CameraSystem;

	private CameraCollisionSystem m_CollisionSystem;

	public IEnumerable<ProxyAction> inputActions
	{
		get
		{
			if (m_MoveAction != null)
			{
				yield return m_MoveAction;
			}
			if (m_MoveFastAction != null)
			{
				yield return m_MoveFastAction;
			}
			if (m_RotateAction != null)
			{
				yield return m_RotateAction;
			}
			if (m_ZoomAction != null)
			{
				yield return m_ZoomAction;
			}
		}
	}

	public Action<bool> EventCameraMovingChanged { get; set; }

	public bool moving { get; private set; }

	public ref LensSettings lens => ref m_VCam.m_Lens;

	public ICinemachineCamera virtualCamera => m_VCam;

	public Vector3 rotation
	{
		get
		{
			return new Vector3(m_Angle.y, m_Angle.x, 0f);
		}
		set
		{
			m_Angle = new float2(value.y, value.x);
		}
	}

	public TerrainSystem terrainSystem
	{
		get
		{
			if (World.DefaultGameObjectInjectionWorld != null)
			{
				return World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TerrainSystem>();
			}
			return null;
		}
	}

	public WaterSystem waterSystem
	{
		get
		{
			if (World.DefaultGameObjectInjectionWorld != null)
			{
				return World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WaterSystem>();
			}
			return null;
		}
	}

	public Vector3 pivot
	{
		get
		{
			return m_Pivot;
		}
		set
		{
			m_Pivot = value;
		}
	}

	public Vector3 position
	{
		get
		{
			return base.transform.position;
		}
		set
		{
		}
	}

	public float2 angle
	{
		get
		{
			return m_Angle;
		}
		set
		{
			m_Angle = value;
		}
	}

	public float zoom
	{
		get
		{
			return m_Zoom;
		}
		set
		{
			m_Zoom = value;
		}
	}

	public bool controllerEnabled
	{
		get
		{
			return base.isActiveAndEnabled;
		}
		set
		{
			base.gameObject.SetActive(value);
		}
	}

	public bool inputEnabled { get; set; } = true;

	public Bounds1 zoomRange
	{
		get
		{
			if (MapTilesUISystem.mapTileViewActive)
			{
				return m_MapTileToolZoomRange;
			}
			return m_ZoomRange;
		}
	}

	public float3 cameraPosition { get; private set; }

	public float velocity { get; private set; }

	public bool edgeScrolling { get; set; }

	public float edgeScrollingSensitivity { get; set; }

	public float clipDistance { get; set; }

	public void TryMatchPosition(IGameCameraController other)
	{
		if (TryGetTerrainHeight(other.position, out var terrainHeight))
		{
			float num = other.position.y - terrainHeight;
			float num2 = Mathf.Sin(MathF.PI / 180f * other.rotation.x);
			float num3 = 1f / (2f - 4f * num2);
			float num4 = (8f * zoomRange.min - 20f * num) * num2 + zoomRange.min - 2f * num;
			float num5 = num4 * num4 - (4f - 8f * num2) * (-4f * zoomRange.min * zoomRange.min + 18f * zoomRange.min * num - 20f * num * num);
			if (!(num5 < 0f))
			{
				zoom = Mathf.Clamp(Mathf.Abs(num3 * (Mathf.Sqrt(num5) + num4)), zoomRange.min, zoomRange.max);
				Quaternion quaternion = Quaternion.Euler(other.rotation.x, other.rotation.y, other.rotation.z);
				pivot = other.position + quaternion * new Vector3(0f, 0f, zoom);
				angle = new float2(other.rotation.y, (other.rotation.x > 90f) ? (other.rotation.x - 360f) : other.rotation.x);
				base.transform.rotation = quaternion;
				base.transform.position = other.position;
				cameraPosition = other.position;
			}
		}
	}

	private async void Awake()
	{
		if (!(await GameManager.instance.WaitForReadyState()))
		{
			return;
		}
		if (!Application.isEditor)
		{
			edgeScrolling = true;
			GameplaySettings gameplaySettings = SharedSettings.instance?.gameplay;
			if (gameplaySettings != null)
			{
				edgeScrolling = gameplaySettings.edgeScrolling;
				edgeScrollingSensitivity = gameplaySettings.edgeScrollingSensitivity;
			}
		}
		m_VCam = GetComponent<CinemachineVirtualCamera>();
		m_InitialFarClip = m_VCam.m_Lens.FarClipPlane;
		m_InitialFov = m_VCam.m_Lens.FieldOfView;
		clipDistance = float.MaxValue;
		m_CameraMap = InputManager.instance.FindActionMap("Camera");
		m_MoveAction = m_CameraMap.FindAction("Move");
		m_MoveFastAction = m_CameraMap.FindAction("Move Fast");
		m_RotateAction = m_CameraMap.FindAction("Rotate");
		m_ZoomAction = m_CameraMap.FindAction("Zoom");
		m_CameraSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_CameraSystem.gamePlayController = this;
		m_CollisionSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraCollisionSystem>();
	}

	public void UpdateCamera()
	{
		if (m_MapTileToolViewEnabled && HandleMapViewCamera())
		{
			return;
		}
		float2 @float = float2.zero;
		float2 float2 = float2.zero;
		float num = 0f;
		bool flag = false;
		if (m_CameraMap.enabled)
		{
			@float = MathUtils.MaxAbs(m_MoveAction.ReadValue<Vector2>(), m_MoveFastAction.ReadValue<Vector2>());
			float2 = m_RotateAction.ReadValue<Vector2>();
			num = m_ZoomAction.ReadValue<float>();
			if (edgeScrolling && InputManager.instance.activeControlScheme == InputManager.ControlScheme.KeyboardAndMouse && InputManager.instance.mouseOnScreen)
			{
				float num2 = edgeScrollingSensitivity;
				float2 xy = ((float3)InputManager.instance.mousePosition).xy;
				xy *= 2f / new float2(Screen.width, Screen.height);
				xy -= 1f;
				float num3 = 0.02f;
				float2 float3 = new float2((float)Screen.height / (float)Screen.width * num3, num3);
				num2 *= math.saturate(math.cmax((math.abs(xy) - (1f - float3)) / float3));
				num2 *= Time.deltaTime;
				@float += math.normalizesafe(xy) * num2;
			}
		}
		float num4 = m_Zoom;
		m_Zoom = MathUtils.Clamp(math.pow(m_Zoom, 1f + num), zoomRange);
		if (num4 != m_Zoom)
		{
			flag = true;
		}
		float2.y = 0f - float2.y;
		m_Angle += float2;
		m_Angle.y = math.clamp(m_Angle.y, -90f, 90f);
		if (m_Angle.x < -180f)
		{
			m_Angle.x += 360f;
		}
		if (m_Angle.x > 180f)
		{
			m_Angle.x -= 360f;
		}
		float2 float4 = math.radians(m_Angle);
		float3 float5 = default(float3);
		float5.x = 0f - math.sin(float4.x);
		float5.y = 0f;
		float5.z = 0f - math.cos(float4.x);
		float3 float6 = float5;
		float5 *= math.cos(float4.y);
		float5.y = math.sin(float4.y);
		float3 float7 = -float5;
		float5 *= m_Zoom;
		float3 float8 = math.cross(float6, new float3(0f, 1f, 0f));
		float3 up = math.cross(float7, float8);
		@float *= m_Zoom;
		m_Pivot += @float.x * float8;
		m_Pivot -= @float.y * float6;
		float3 cameraPos = GetCameraPos(float5);
		if (terrainSystem != null)
		{
			TerrainHeightData data = terrainSystem.GetHeightData();
			WaterSurfacesData surfacesData = default(WaterSurfacesData);
			WaterSurfaceData<half> maxHeightSurfaceData = default(WaterSurfaceData<half>);
			if (waterSystem != null && waterSystem.Loaded)
			{
				surfacesData = waterSystem.GetSurfacesData(out var deps);
				deps.Complete();
				maxHeightSurfaceData = waterSystem.GetMexHeightSurfaceData(out var deps2);
				deps2.Complete();
			}
			if (data.isCreated)
			{
				if (surfacesData.depths.isCreated)
				{
					float start = WaterUtils.SampleHeight(ref maxHeightSurfaceData, ref surfacesData, ref data, m_Pivot);
					m_Pivot.y = math.lerp(start, m_Pivot.y, m_MoveSmoothing);
				}
				else
				{
					m_Pivot.y = math.lerp(TerrainUtils.SampleHeight(ref data, m_Pivot), m_Pivot.y, m_MoveSmoothing);
				}
				m_Pivot = MathUtils.Clamp(bounds: GameManager.instance.gameMode.IsEditor() ? TerrainUtils.GetEditorCameraBounds(terrainSystem, ref data) : TerrainUtils.GetBounds(ref data), position: m_Pivot);
				cameraPos = GetCameraPos(float5);
				float num5 = ((!surfacesData.depths.isCreated) ? (TerrainUtils.SampleHeight(ref data, cameraPos) + zoomRange.min * 0.5f + (m_Zoom - zoomRange.min) * 0.1f) : (WaterUtils.SampleHeight(ref maxHeightSurfaceData, ref surfacesData, ref data, cameraPos) + zoomRange.min * 0.5f + (m_Zoom - zoomRange.min) * 0.1f));
				float num6 = (cameraPos.y - num5) / m_Zoom;
				num6 = (math.sqrt(num6 * num6 + 0.2f) - num6) * (0.5f * m_Zoom);
				cameraPos.y += num6;
			}
		}
		float3 float9 = cameraPosition;
		quaternion quaternion = quaternion.LookRotation(float7, up);
		if (m_CollisionSystem != null && m_CameraSystem != null && m_CameraSystem.activeCamera != null)
		{
			float nearClipPlane = m_CameraSystem.activeCamera.nearClipPlane;
			float2 fieldOfView = default(float2);
			fieldOfView.y = m_CameraSystem.activeCamera.fieldOfView;
			fieldOfView.x = Camera.VerticalToHorizontalFieldOfView(fieldOfView.y, m_CameraSystem.activeCamera.aspect);
			m_CollisionSystem.CheckCollisions(ref cameraPos, float9, quaternion, math.min(m_Zoom - zoomRange.min, 200f), math.min(zoomRange.max - m_Zoom, 200f), math.max(nearClipPlane * 2f, zoomRange.min * 0.5f), nearClipPlane, m_CollisionSmoothing, fieldOfView);
		}
		Quaternion localRotation = base.transform.localRotation;
		cameraPosition = cameraPos;
		base.transform.localPosition = cameraPos;
		base.transform.localRotation = quaternion;
		velocity = math.lengthsq(float9 - cameraPosition) / Time.deltaTime;
		if (!localRotation.Equals(base.transform.localRotation) || !float9.Equals(cameraPosition))
		{
			flag = true;
		}
		if (moving != flag)
		{
			EventCameraMovingChanged?.Invoke(flag);
			moving = flag;
		}
		AudioManager.instance?.UpdateAudioListener(base.transform.position, base.transform.rotation);
	}

	public void ResetCamera()
	{
		m_MapViewTimer = 0f;
	}

	private float3 GetCameraPos(float3 cameraOffset)
	{
		float3 result = m_Pivot + cameraOffset;
		result.y += zoomRange.min * 0.5f;
		return result;
	}

	private bool HandleMapViewCamera()
	{
		float end;
		float2 to;
		float3 @float;
		float num;
		if (!MapTilesUISystem.mapTileViewActive)
		{
			if (m_MapViewTimer == 0f)
			{
				return false;
			}
			m_Zoom = m_LastGameViewZoom;
			m_Angle = m_LastGameViewAngle;
			m_Pivot = m_LastGameViewPivot;
			end = m_LastMapViewZoom;
			to = m_LastMapViewAngle;
			@float = m_LastMapViewPivot;
			m_MapViewTimer = math.max(m_MapViewTimer - Time.deltaTime, 0f);
			num = ((m_MapTileToolTransitionTime > 0f) ? (m_MapViewTimer / m_MapTileToolTransitionTime) : 0f);
		}
		else
		{
			if (m_MapViewTimer == 0f)
			{
				m_LastGameViewZoom = m_Zoom;
				m_LastGameViewAngle = m_Angle;
				m_LastGameViewPivot = m_Pivot;
			}
			m_LastMapViewAngle = m_Angle;
			m_LastMapViewZoom = m_Zoom;
			m_LastMapViewPivot = m_Pivot;
			if (Mathf.Abs(m_MapViewTimer - m_MapTileToolTransitionTime) < Mathf.Epsilon)
			{
				return false;
			}
			m_MapViewTimer = math.min(m_MapViewTimer + Time.deltaTime, m_MapTileToolTransitionTime);
			num = ((m_MapTileToolTransitionTime > 0f) ? (m_MapViewTimer / m_MapTileToolTransitionTime) : 1f);
			if (Mathf.Abs(num - 1f) < Mathf.Epsilon)
			{
				m_Zoom = m_MapTileToolZoom;
				m_Angle = new float2(Mathf.Round(m_Angle.x / 90f) * 90f, m_MapTileToolAngle.y);
				m_Pivot = m_MapTileToolPivot;
			}
			end = m_MapTileToolZoom;
			to = new float2(Mathf.Round(m_Angle.x / 90f) * 90f, m_MapTileToolAngle.y);
			@float = m_MapTileToolPivot;
		}
		if (TryGetTerrainHeight(@float, out var terrainHeight))
		{
			@float.y = terrainHeight;
		}
		num = Mathf.SmoothStep(0f, 1f, num);
		float3 float2 = math.lerp(m_Pivot, @float, num);
		float2 float3 = LerpAngle(m_Angle, to, num);
		float num2 = math.lerp(m_Zoom, end, num);
		if (MapTilesUISystem.mapTileViewActive)
		{
			m_LastMapViewPivot = float2;
			m_LastMapViewAngle = float3;
			m_LastMapViewZoom = num2;
		}
		m_VCam.m_Lens.FarClipPlane = math.lerp(m_InitialFarClip, m_MapTileToolFarclip, num);
		m_VCam.m_Lens.FieldOfView = math.lerp(m_InitialFov, m_MapTileToolFOV, num);
		float2 float4 = math.radians(float3);
		float3 float5 = default(float3);
		float5.x = 0f - math.sin(float4.x);
		float5.y = 0f;
		float5.z = 0f - math.cos(float4.x);
		float3 x = float5;
		float5 *= math.cos(float4.y);
		float5.y = math.sin(float4.y);
		float3 float6 = -float5;
		float5 *= num2;
		float3 float7 = float2 + float5;
		float7.y += zoomRange.min * 0.5f;
		float3 y = math.cross(x, new float3(0f, 1f, 0f));
		float3 up = math.cross(float6, y);
		if (terrainSystem != null)
		{
			TerrainHeightData data = terrainSystem.GetHeightData();
			WaterSurfaceData<SurfaceWater> data2 = default(WaterSurfaceData<SurfaceWater>);
			if (waterSystem != null)
			{
				data2 = waterSystem.GetSurfaceData(out var deps);
				deps.Complete();
			}
			if (data.isCreated)
			{
				float num3 = ((!data2.isCreated) ? (TerrainUtils.SampleHeight(ref data, float7) + zoomRange.min * 0.5f + (num2 - zoomRange.min) * 0.1f) : (WaterUtils.SampleHeight(ref data2, ref data, float7) + zoomRange.min * 0.5f + (num2 - zoomRange.min) * 0.1f));
				float num4 = (float7.y - num3) / num2;
				num4 = (math.sqrt(num4 * num4 + 0.2f) - num4) * (0.5f * num2);
				float7.y += num4;
			}
		}
		base.transform.localPosition = float7;
		base.transform.localRotation = quaternion.LookRotation(float6, up);
		return true;
	}

	public static float2 LerpAngle(float2 from, float2 to, float t)
	{
		float num = ((to.x - from.x) % 360f + 540f) % 360f - 180f;
		return new float2(from.x + num * t % 360f, math.lerp(from.y, to.y, t));
	}

	private bool TryGetTerrainHeight(Vector3 pos, out float terrainHeight)
	{
		if (terrainSystem != null)
		{
			TerrainHeightData data = terrainSystem.GetHeightData();
			WaterSurfaceData<SurfaceWater> data2 = default(WaterSurfaceData<SurfaceWater>);
			if (waterSystem != null)
			{
				data2 = waterSystem.GetSurfaceData(out var deps);
				deps.Complete();
			}
			if (data.isCreated)
			{
				if (data2.isCreated)
				{
					terrainHeight = WaterUtils.SampleHeight(ref data2, ref data, pos);
				}
				else
				{
					terrainHeight = TerrainUtils.SampleHeight(ref data, pos);
				}
				return true;
			}
		}
		terrainHeight = 0f;
		return false;
	}

	public static bool TryGet(out CameraController cameraController)
	{
		GameObject gameObject = GameObject.FindGameObjectWithTag("GameplayCamera");
		if (gameObject != null)
		{
			cameraController = gameObject.GetComponent<CameraController>();
			return cameraController != null;
		}
		cameraController = null;
		return false;
	}
}
