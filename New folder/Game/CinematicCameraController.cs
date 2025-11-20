using System;
using Cinemachine;
using Game.Audio;
using Game.Rendering;
using Game.SceneFlow;
using Unity.Entities;
using UnityEngine;

namespace Game;

public class CinematicCameraController : MonoBehaviour, IGameCameraController
{
	[SerializeField]
	private float m_MinMoveSpeed = 5f;

	[SerializeField]
	private float m_MaxMoveSpeed = 1000f;

	[SerializeField]
	private float m_MinZoomSpeed = 10f;

	[SerializeField]
	private float m_MaxZoomSpeed = 4000f;

	[SerializeField]
	private float m_RotateSpeed = 0.5f;

	[SerializeField]
	private float m_MaxHeight = 5000f;

	[SerializeField]
	private float m_MaxMovementSpeedHeight = 1000f;

	private Transform m_Anchor;

	private CinemachineVirtualCamera m_VCam;

	private CinemachineRestrictToTerrain m_RestrictToTerrain;

	private CameraInput m_CameraInput;

	private CameraUpdateSystem m_CameraUpdateSystem;

	public ICinemachineCamera virtualCamera => m_VCam;

	public float zoom
	{
		get
		{
			return m_Anchor.position.y;
		}
		set
		{
			Vector3 vector = m_Anchor.position;
			vector.y = value;
			m_Anchor.position = vector;
		}
	}

	public Vector3 pivot
	{
		get
		{
			return m_Anchor.position;
		}
		set
		{
			m_Anchor.position = value;
		}
	}

	public Vector3 position
	{
		get
		{
			return pivot;
		}
		set
		{
			pivot = value;
		}
	}

	public Vector3 rotation
	{
		get
		{
			return m_Anchor.rotation.eulerAngles;
		}
		set
		{
			m_Anchor.rotation = Quaternion.Euler(value);
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

	public bool collisionsEnabled
	{
		get
		{
			return m_RestrictToTerrain.enableObjectCollisions;
		}
		set
		{
			m_RestrictToTerrain.enableObjectCollisions = value;
		}
	}

	public ref LensSettings lens => ref m_VCam.m_Lens;

	public Action eventCameraMove { get; set; }

	public float fov
	{
		get
		{
			return m_VCam.m_Lens.FieldOfView;
		}
		set
		{
			m_VCam.m_Lens.FieldOfView = value;
		}
	}

	public float dutch
	{
		get
		{
			return m_VCam.m_Lens.Dutch;
		}
		set
		{
			m_VCam.m_Lens.Dutch = value;
		}
	}

	public bool inputEnabled { get; set; } = true;

	private async void Awake()
	{
		if (await GameManager.instance.WaitForReadyState())
		{
			m_Anchor = new GameObject("CinematicCameraControllerAnchor").transform;
			m_VCam = GetComponent<CinemachineVirtualCamera>();
			m_VCam.Follow = m_Anchor;
			m_RestrictToTerrain = GetComponent<CinemachineRestrictToTerrain>();
			m_CameraInput = GetComponent<CameraInput>();
			if (m_CameraInput != null)
			{
				m_CameraInput.Initialize();
			}
			m_CameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraUpdateSystem>();
			m_CameraUpdateSystem.cinematicCameraController = this;
			base.gameObject.SetActive(value: false);
		}
	}

	public void TryMatchPosition(IGameCameraController other)
	{
		position = other.position;
		rotation = other.rotation;
	}

	public void UpdateCamera()
	{
		if (m_CameraInput != null)
		{
			m_CameraInput.Refresh();
			if (m_CameraInput.any)
			{
				eventCameraMove?.Invoke();
			}
			if (inputEnabled)
			{
				UpdateController(m_CameraInput);
			}
		}
		AudioManager.instance?.UpdateAudioListener(base.transform.position, base.transform.rotation);
	}

	public void ResetCamera()
	{
	}

	private void UpdateController(CameraInput input)
	{
		m_RestrictToTerrain.Refresh();
		Vector3 vector = m_Anchor.position;
		m_RestrictToTerrain.ClampToTerrain(vector, restrictToMapArea: true, out var terrainHeight);
		float t = Mathf.Min(vector.y - terrainHeight, m_MaxMovementSpeedHeight) / m_MaxMovementSpeedHeight;
		Vector2 move = input.move;
		move *= Mathf.Lerp(m_MinMoveSpeed, m_MaxMoveSpeed, t);
		Vector2 vector2 = input.rotate * m_RotateSpeed;
		float num = input.zoom * Mathf.Lerp(m_MinZoomSpeed, m_MaxZoomSpeed, t);
		Vector3 eulerAngles = m_Anchor.rotation.eulerAngles;
		vector += Quaternion.AngleAxis(eulerAngles.y, Vector3.up) * new Vector3(move.x, 0f - num, move.y);
		vector = m_RestrictToTerrain.ClampToTerrain(vector, restrictToMapArea: true, out var terrainHeight2);
		vector.y = Mathf.Min(vector.y, terrainHeight2 + m_MaxHeight);
		Quaternion quaternion = Quaternion.Euler(Mathf.Clamp((eulerAngles.x + 90f) % 360f - vector2.y, 0f, 180f) - 90f, eulerAngles.y + vector2.x, 0f);
		if (m_RestrictToTerrain.enableObjectCollisions && m_RestrictToTerrain.CheckForCollision(vector, m_RestrictToTerrain.previousPosition, quaternion, out var vector3))
		{
			m_Anchor.position = vector3;
		}
		else
		{
			m_Anchor.position = vector;
		}
		m_Anchor.rotation = quaternion;
	}

	private void OnDestroy()
	{
		if (m_Anchor != null)
		{
			UnityEngine.Object.Destroy(m_Anchor.gameObject);
		}
		if (m_CameraUpdateSystem != null)
		{
			m_CameraUpdateSystem.cinematicCameraController = null;
		}
	}
}
