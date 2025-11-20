using System;
using Cinemachine;
using Colossal.Mathematics;
using Game.Audio;
using Game.Rendering;
using Game.SceneFlow;
using Game.UI.InGame;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game;

public class OrbitCameraController : MonoBehaviour, IGameCameraController
{
	public enum Mode
	{
		Follow,
		PhotoMode,
		Editor
	}

	private static readonly float kPivotVerticalOffset = 10f;

	public float2 m_ZoomRange = new float2(10f, 10000f);

	public float m_FollowSmoothing = 0.01f;

	private Entity m_Entity;

	private float m_FollowTimer;

	private float2 m_Rotation;

	private GameObject m_Anchor;

	private CinemachineVirtualCamera m_VCam;

	private CinemachineOrbitalTransposer m_Transposer;

	private CinemachineRestrictToTerrain m_Collider;

	private CameraInput m_CameraInput;

	private CameraUpdateSystem m_CameraUpdateSystem;

	public Entity followedEntity
	{
		get
		{
			if (!base.isActiveAndEnabled)
			{
				return Entity.Null;
			}
			return m_Entity;
		}
		set
		{
			if (m_Entity != value)
			{
				m_Entity = value;
				xOffset = 0f;
				yOffset = 0f;
				m_FollowTimer = 0f;
				if (base.isActiveAndEnabled)
				{
					RefreshAudioFollow(value != Entity.Null);
				}
			}
		}
	}

	public Mode mode { get; set; }

	public Vector3 pivot
	{
		get
		{
			return m_Anchor.transform.position;
		}
		set
		{
			m_Anchor.transform.position = value;
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
			base.transform.position = value;
			m_Anchor.transform.position = value + m_Anchor.transform.rotation * new Vector3(0f, 0f, zoom);
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

	public Vector3 rotation
	{
		get
		{
			return m_Anchor.transform.rotation.eulerAngles;
		}
		set
		{
			m_Rotation = new float2(value.y, value.x);
		}
	}

	public float zoom { get; set; }

	public float yOffset { get; set; }

	public float xOffset { get; set; }

	public ICinemachineCamera virtualCamera => m_VCam;

	public ref LensSettings lens => ref m_VCam.m_Lens;

	public bool collisionsEnabled
	{
		get
		{
			return m_Collider.enableObjectCollisions;
		}
		set
		{
			m_Collider.enableObjectCollisions = value;
		}
	}

	public Action EventCameraMove { get; set; }

	private async void Awake()
	{
		if (await GameManager.instance.WaitForReadyState())
		{
			m_Anchor = new GameObject("OrbitCameraAnchor");
			Transform transform = m_Anchor.transform;
			m_VCam = GetComponent<CinemachineVirtualCamera>();
			m_Transposer = m_VCam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
			m_Collider = GetComponent<CinemachineRestrictToTerrain>();
			if (m_VCam != null)
			{
				m_VCam.LookAt = transform;
				m_VCam.Follow = transform;
			}
			base.gameObject.SetActive(value: false);
			m_CameraInput = GetComponent<CameraInput>();
			if (m_CameraInput != null)
			{
				m_CameraInput.Initialize();
			}
			m_CameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraUpdateSystem>();
			m_CameraUpdateSystem.orbitCameraController = this;
			zoom = Mathf.Clamp(zoom, m_ZoomRange.x, m_ZoomRange.y);
		}
	}

	private void OnEnable()
	{
		RefreshAudioFollow(active: true);
	}

	private void OnDisable()
	{
		RefreshAudioFollow(active: false);
	}

	private void RefreshAudioFollow(bool active)
	{
		if (AudioManager.instance != null)
		{
			AudioManager.instance.followed = (active ? m_Entity : Entity.Null);
		}
	}

	private void OnDestroy()
	{
		if (m_Anchor != null)
		{
			UnityEngine.Object.Destroy(m_Anchor);
		}
		if (m_CameraUpdateSystem != null)
		{
			m_CameraUpdateSystem.cinematicCameraController = null;
		}
	}

	public void TryMatchPosition(IGameCameraController other)
	{
		rotation = other.rotation;
		if (other is CinematicCameraController)
		{
			m_Collider.ClampToTerrain(other.position, restrictToMapArea: false, out var terrainHeight);
			float num = other.position.y - terrainHeight - kPivotVerticalOffset;
			zoom = Mathf.Clamp(num / Mathf.Sin(MathF.PI / 180f * Mathf.Abs(other.rotation.x)), m_ZoomRange.x, m_ZoomRange.y);
			pivot = new Vector3(other.position.x, num, other.position.z) + Quaternion.Euler(other.rotation) * new Vector3(0f, 0f, zoom);
		}
		else
		{
			zoom = Mathf.Clamp(other.zoom, m_ZoomRange.x, m_ZoomRange.y);
			pivot = other.pivot;
		}
	}

	public void UpdateCamera()
	{
		m_Collider.Refresh();
		m_CameraInput.Refresh();
		if (inputEnabled && m_CameraInput != null)
		{
			Vector2 rotate = m_CameraInput.rotate;
			m_Rotation.x = (m_Rotation.x + rotate.x) % 360f;
			m_Rotation.y = Mathf.Clamp((m_Rotation.y + 90f) % 360f - rotate.y, 0f, 180f) - 90f;
			float num = m_CameraInput.zoom;
			zoom = Mathf.Clamp(math.pow(zoom, 1f + num), m_ZoomRange.x, m_ZoomRange.y);
			if (followedEntity == Entity.Null)
			{
				Vector2 move = m_CameraInput.move;
				Vector3 vector = m_Anchor.transform.position;
				vector = m_Collider.ClampToTerrain(vector, restrictToMapArea: true, out var _);
				Vector2 vector2 = move * zoom;
				Vector3 vector3 = vector + (Vector3)math.mul(quaternion.AxisAngle(new float3(0f, 1f, 0f), math.radians(m_Anchor.transform.rotation.eulerAngles.y)), new float3(vector2.x, 0f, vector2.y));
				vector3 = m_Collider.ClampToTerrain(vector3, restrictToMapArea: true, out var terrainHeight2);
				vector3.y = terrainHeight2 + kPivotVerticalOffset;
				m_Anchor.transform.position = vector3;
			}
			if (TryGetPosition(followedEntity, World.DefaultGameObjectInjectionWorld.EntityManager, out var @float, out var quaternion, out var radius))
			{
				if (mode == Mode.PhotoMode)
				{
					float num2 = math.length(@float - (float3)base.transform.position);
					float num3 = 0.8f;
					float num4 = 1f - math.saturate((num2 - 40f) / 40f);
					float num5 = 15f;
					float num6 = 2.5f * (num5 / math.max(num2, 0.01f)) * (1f - num3 + num3 * num4);
					Quaternion a = m_Anchor.transform.rotation;
					Quaternion b = quaternion.Euler(math.radians(m_Rotation.y), math.radians(m_Rotation.x + ((Quaternion)quaternion).eulerAngles.y), 0f);
					m_Anchor.transform.rotation = Quaternion.Slerp(a, b, Time.deltaTime * num6);
				}
				else
				{
					m_Anchor.transform.rotation = quaternion.Euler(math.radians(m_Rotation.y), math.radians(m_Rotation.x), 0f);
				}
				float3 float2 = (float3)pivot - @float;
				m_FollowTimer += Time.deltaTime;
				float num7 = math.pow(m_FollowSmoothing, Time.deltaTime) * math.smoothstep(0.5f, 0f, m_FollowTimer);
				float2 *= num7;
				m_Anchor.transform.position = @float + float2 + math.mul(m_Anchor.transform.rotation, new float3(xOffset, yOffset, 0f));
			}
			else
			{
				m_Anchor.transform.rotation = quaternion.Euler(math.radians(m_Rotation.y), math.radians(m_Rotation.x), 0f);
			}
			m_Transposer.m_FollowOffset.z = 0f - zoom - radius;
		}
		Transform transform = base.transform;
		AudioManager.instance?.UpdateAudioListener(transform.position, transform.rotation);
		if (m_CameraInput.isMoving || MapTilesUISystem.mapTileViewActive)
		{
			EventCameraMove?.Invoke();
		}
	}

	public void ResetCamera()
	{
	}

	private static bool TryGetPosition(Entity e, EntityManager entityManager, out float3 position, out quaternion rotation, out float radius)
	{
		int elementIndex = -1;
		if (e != Entity.Null && SelectedInfoUISystem.TryGetPosition(e, entityManager, ref elementIndex, out var _, out position, out var bounds, out rotation, reinterpolate: true))
		{
			position.y = MathUtils.Center(bounds.y);
			float3 @float = (bounds.max - bounds.min) / 2f;
			radius = Mathf.Min(@float.x, @float.y, @float.z);
			return true;
		}
		position = float3.zero;
		rotation = quaternion.identity;
		radius = 0f;
		return false;
	}
}
