using Cinemachine;
using Colossal.Mathematics;
using Game.Rendering;
using Game.SceneFlow;
using Game.Simulation;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game;

public class CinemachineRestrictToTerrain : CinemachineExtension
{
	public float m_MapSurfacePadding = 1f;

	public bool m_RestrictToMapArea = true;

	private CameraCollisionSystem m_CollisionSystem;

	private CameraUpdateSystem m_CameraSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	public bool enableObjectCollisions { get; set; } = true;

	public Vector3 previousPosition { get; set; }

	protected void Start()
	{
		m_CollisionSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraCollisionSystem>();
		m_CameraSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_TerrainSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WaterSystem>();
	}

	public void Refresh()
	{
		previousPosition = base.transform.position;
	}

	protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
	{
		if (stage == CinemachineCore.Stage.Body)
		{
			float terrainHeight;
			Vector3 rawPosition = ClampToTerrain(state.RawPosition, m_RestrictToMapArea, out terrainHeight);
			state.RawPosition = rawPosition;
			if (enableObjectCollisions && CheckForCollision(state.RawPosition, previousPosition, state.RawOrientation, out var position))
			{
				state.RawPosition = position;
			}
		}
	}

	public bool CheckForCollision(Vector3 currentPosition, Vector3 lastPosition, Quaternion rotation, out Vector3 position)
	{
		if (m_CollisionSystem != null && m_CameraSystem != null && m_CameraSystem.activeCamera != null)
		{
			float3 position2 = currentPosition;
			float3 @float = lastPosition;
			float nearClipPlane = m_CameraSystem.activeCamera.nearClipPlane;
			float2 fieldOfView = default(float2);
			fieldOfView.y = m_CameraSystem.activeCamera.fieldOfView;
			fieldOfView.x = Camera.VerticalToHorizontalFieldOfView(fieldOfView.y, m_CameraSystem.activeCamera.aspect);
			m_CollisionSystem.CheckCollisions(ref position2, @float, rotation, 200f, 200f, nearClipPlane * 2f + 1f, nearClipPlane, 0.001f, fieldOfView);
			position = position2;
			return true;
		}
		position = Vector3.zero;
		return false;
	}

	public Vector3 ClampToTerrain(Vector3 position, bool restrictToMapArea, out float terrainHeight)
	{
		terrainHeight = 0f;
		TerrainHeightData data = m_TerrainSystem.GetHeightData();
		if (data.isCreated)
		{
			if (restrictToMapArea)
			{
				Bounds3 bounds = (GameManager.instance.gameMode.IsEditor() ? TerrainUtils.GetEditorCameraBounds(m_TerrainSystem, ref data) : TerrainUtils.GetBounds(ref data));
				float3 max = bounds.max;
				max.y = bounds.min.y + math.max(bounds.max.y - bounds.min.y, 4096f);
				bounds.max = max;
				position = MathUtils.Clamp(position, bounds);
			}
			if (m_WaterSystem.Loaded)
			{
				JobHandle deps;
				WaterSurfaceData<SurfaceWater> data2 = m_WaterSystem.GetSurfaceData(out deps);
				deps.Complete();
				if (data2.isCreated)
				{
					terrainHeight = WaterUtils.SampleHeight(ref data2, ref data, position);
				}
			}
			else
			{
				terrainHeight = TerrainUtils.SampleHeight(ref data, position);
			}
			position.y = Mathf.Max(position.y, terrainHeight += m_MapSurfacePadding);
		}
		return position;
	}
}
