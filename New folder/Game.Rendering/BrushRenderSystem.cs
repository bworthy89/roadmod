using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class BrushRenderSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Brush> __Game_Tools_Brush_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_Brush_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Brush>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		}
	}

	private EntityQuery m_BrushQuery;

	private EntityQuery m_SettingsQuery;

	private ToolSystem m_ToolSystem;

	private TerrainSystem m_TerrainSystem;

	private PrefabSystem m_PrefabSystem;

	private Mesh m_Mesh;

	private MaterialPropertyBlock m_Properties;

	private int m_BrushTexture;

	private int m_BrushOpacity;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_BrushQuery = GetEntityQuery(ComponentType.ReadOnly<Brush>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
		m_SettingsQuery = GetEntityQuery(ComponentType.ReadOnly<OverlayConfigurationData>());
		m_BrushTexture = Shader.PropertyToID("_BrushTexture");
		m_BrushOpacity = Shader.PropertyToID("_BrushOpacity");
		RenderPipelineManager.beginContextRendering += Render;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		RenderPipelineManager.beginContextRendering -= Render;
		CoreUtils.Destroy(m_Mesh);
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	private void Render(ScriptableRenderContext context, List<Camera> cameras)
	{
		try
		{
			if (!m_PrefabSystem.TryGetSingletonPrefab<OverlayConfigurationPrefab>(m_SettingsQuery, out var prefab))
			{
				return;
			}
			float y = m_TerrainSystem.heightScaleOffset.y - 50f;
			float y2 = m_TerrainSystem.heightScaleOffset.x + 100f;
			Mesh mesh = GetMesh();
			MaterialPropertyBlock properties = GetProperties();
			NativeArray<ArchetypeChunk> nativeArray = m_BrushQuery.ToArchetypeChunkArray(Allocator.TempJob);
			CompleteDependency();
			try
			{
				ComponentTypeHandle<Brush> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Brush_RO_ComponentTypeHandle, ref base.CheckedStateRef);
				ComponentTypeHandle<PrefabRef> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					ArchetypeChunk archetypeChunk = nativeArray[i];
					NativeArray<Brush> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle);
					NativeArray<PrefabRef> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						Brush brush = nativeArray2[j];
						PrefabRef refData = nativeArray3[j];
						float3 @float = new float3(brush.m_Position.x, y, brush.m_Position.z);
						quaternion quaternion = quaternion.RotateY(brush.m_Angle);
						float3 float2 = new float3(brush.m_Size * 0.5f, y2, brush.m_Size * 0.5f);
						PrefabBase prefab2 = m_PrefabSystem.GetPrefab<PrefabBase>(brush.m_Tool);
						BrushPrefab prefab3 = m_PrefabSystem.GetPrefab<BrushPrefab>(refData);
						properties.Clear();
						properties.SetTexture(m_BrushTexture, prefab3.m_Texture);
						properties.SetFloat(m_BrushOpacity, brush.m_Opacity);
						Material material = null;
						if (prefab2 is TerraformingPrefab terraformingPrefab)
						{
							material = terraformingPrefab.m_BrushMaterial;
						}
						else if (prefab2 is ObjectPrefab)
						{
							material = prefab.m_ObjectBrushMaterial;
						}
						Matrix4x4 matrix = Matrix4x4.TRS(@float, quaternion, float2);
						foreach (Camera camera in cameras)
						{
							if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView)
							{
								Graphics.DrawMesh(mesh, matrix, material, 0, camera, 0, properties, ShadowCastingMode.Off, receiveShadows: false);
							}
						}
						if (base.EntityManager.TryGetComponent<TerraformingData>(brush.m_Tool, out var component))
						{
							PreviewHeight(brush, prefab3, component.m_Type);
						}
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
			}
		}
		finally
		{
		}
	}

	private void PreviewHeight(Brush brush, BrushPrefab prefab, TerraformingType terraformingType)
	{
		Bounds2 bounds = ToolUtils.GetBounds(brush);
		if ((terraformingType == TerraformingType.Level || terraformingType == TerraformingType.Slope) && brush.m_Strength < 0f)
		{
			if (terraformingType == TerraformingType.Level)
			{
				brush.m_Strength = math.abs(brush.m_Strength);
			}
			else
			{
				brush.m_Strength = 0f;
			}
		}
		m_TerrainSystem.PreviewBrush(terraformingType, bounds, brush, prefab.m_Texture);
	}

	private Mesh GetMesh()
	{
		if (m_Mesh == null)
		{
			m_Mesh = new Mesh();
			m_Mesh.name = "Brush";
			m_Mesh.vertices = new Vector3[8]
			{
				new Vector3(-1f, 0f, -1f),
				new Vector3(-1f, 0f, 1f),
				new Vector3(1f, 0f, 1f),
				new Vector3(1f, 0f, -1f),
				new Vector3(-1f, 1f, -1f),
				new Vector3(-1f, 1f, 1f),
				new Vector3(1f, 1f, 1f),
				new Vector3(1f, 1f, -1f)
			};
			m_Mesh.triangles = new int[36]
			{
				0, 1, 5, 5, 4, 0, 3, 7, 6, 6,
				2, 3, 0, 3, 2, 2, 1, 0, 4, 5,
				6, 6, 7, 4, 0, 4, 7, 7, 3, 0,
				1, 2, 6, 6, 5, 1
			};
		}
		return m_Mesh;
	}

	private MaterialPropertyBlock GetProperties()
	{
		if (m_Properties == null)
		{
			m_Properties = new MaterialPropertyBlock();
		}
		return m_Properties;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public BrushRenderSystem()
	{
	}
}
