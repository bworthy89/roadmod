using System;
using Colossal;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Logging;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class VTTestGameManager : MonoBehaviour
{
	[Header("VT Settings")]
	public bool m_OverrideVTSettings;

	public int m_VTMipBias;

	public UnityEngine.Rendering.VirtualTexturing.FilterMode m_VTFilterMode = UnityEngine.Rendering.VirtualTexturing.FilterMode.Trilinear;

	private uint m_FrameIndex;

	private float m_FrameTime;

	private WindControl m_WindControl;

	[Header("Camera")]
	public Camera movingCamera;

	public float cameraSpeed = 0.1f;

	public Transform cameraStart;

	public Transform cameraEnd;

	private float cameraPosition;

	private bool movingBackward;

	private World m_World;

	private TextureStreamingSystem m_TextureStreamingSystem;

	private GizmosSystem m_GizmosSystem;

	private void Awake()
	{
		m_WindControl = WindControl.instance;
		LogManager.SetDefaultEffectiveness(Level.Info);
		m_World = new World("Game");
		World.DefaultGameObjectInjectionWorld = m_World;
		m_GizmosSystem = m_World.GetOrCreateSystemManaged<GizmosSystem>();
		m_TextureStreamingSystem = m_World.GetOrCreateSystemManaged<TextureStreamingSystem>();
		if (m_OverrideVTSettings)
		{
			m_TextureStreamingSystem.Initialize(m_VTMipBias, m_VTFilterMode);
		}
		else
		{
			m_TextureStreamingSystem.Initialize();
		}
		cameraPosition = 0f;
	}

	private void OnDestroy()
	{
		Colossal.Gizmos.ReleaseResources();
		m_World.Dispose();
		m_WindControl.Dispose();
	}

	private void Update()
	{
		m_FrameIndex += 5u;
		m_FrameTime = Time.deltaTime;
		float4 xyxy = (m_FrameIndex % new uint2(60u, 3600u) + new float2(m_FrameTime)).xyxy;
		xyxy *= new float4(1f / 60f, 0.00027777778f, MathF.PI / 30f, 0.0017453294f);
		Shader.SetGlobalVector("colossal_SimulationTime", xyxy);
		float value = (float)(m_FrameIndex % 216000) + m_FrameTime;
		Shader.SetGlobalFloat("colossal_SimulationTime2", value);
		m_TextureStreamingSystem.Update();
		if (!(movingCamera != null) || !(cameraEnd != null) || !(cameraStart != null))
		{
			return;
		}
		Vector3 vector = cameraEnd.position - cameraStart.position;
		float num = cameraSpeed * Time.deltaTime;
		if (movingBackward)
		{
			cameraPosition -= num;
			if (cameraPosition < 0f)
			{
				cameraPosition = 0f;
				movingBackward = false;
			}
		}
		else
		{
			cameraPosition += num;
			if (cameraPosition > vector.magnitude)
			{
				cameraPosition = vector.magnitude;
				movingBackward = true;
			}
		}
		movingCamera.transform.position = cameraStart.position + vector.normalized * cameraPosition;
	}

	private void LateUpdate()
	{
		m_GizmosSystem.Update();
	}
}
