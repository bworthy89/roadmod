using System.Collections.Generic;
using Game.Areas;
using Game.Tools;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Rendering;

public class AreaRenderSystem : GameSystemBase
{
	private RenderingSystem m_RenderingSystem;

	private AreaBufferSystem m_AreaBufferSystem;

	private AreaBatchSystem m_AreaBatchSystem;

	private CityBoundaryMeshSystem m_CityBoundaryMeshSystem;

	private ToolSystem m_ToolSystem;

	private int m_AreaTriangleBuffer;

	private int m_AreaBatchBuffer;

	private int m_AreaBatchColors;

	private int m_VisibleIndices;

	private Mesh m_AreaMesh;

	private GraphicsBuffer m_ArgsBuffer;

	private List<GraphicsBuffer.IndirectDrawIndexedArgs> m_ArgsArray;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_AreaBufferSystem = base.World.GetOrCreateSystemManaged<AreaBufferSystem>();
		m_AreaBatchSystem = base.World.GetOrCreateSystemManaged<AreaBatchSystem>();
		m_CityBoundaryMeshSystem = base.World.GetOrCreateSystemManaged<CityBoundaryMeshSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_AreaTriangleBuffer = Shader.PropertyToID("colossal_AreaTriangleBuffer");
		m_AreaBatchBuffer = Shader.PropertyToID("colossal_AreaBatchBuffer");
		m_AreaBatchColors = Shader.PropertyToID("colossal_AreaBatchColors");
		m_VisibleIndices = Shader.PropertyToID("colossal_VisibleIndices");
		RenderPipelineManager.beginContextRendering += Render;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		RenderPipelineManager.beginContextRendering -= Render;
		if (m_AreaMesh != null)
		{
			Object.Destroy(m_AreaMesh);
		}
		if (m_ArgsBuffer != null)
		{
			m_ArgsBuffer.Release();
		}
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	private unsafe void Render(ScriptableRenderContext context, List<Camera> cameras)
	{
		try
		{
			int num = 0;
			int batchCount = m_AreaBatchSystem.GetBatchCount();
			ComputeBuffer buffer;
			Material material3;
			Bounds bounds;
			if (!m_RenderingSystem.hideOverlay)
			{
				for (AreaType areaType = AreaType.Lot; areaType < AreaType.Count; areaType++)
				{
					if (!m_AreaBufferSystem.GetNameMesh(areaType, out var mesh, out var subMeshCount))
					{
						continue;
					}
					for (int i = 0; i < subMeshCount; i++)
					{
						if (!m_AreaBufferSystem.GetNameMaterial(areaType, i, out var material))
						{
							continue;
						}
						foreach (Camera camera in cameras)
						{
							if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView)
							{
								Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0, camera, i, null, castShadows: false, receiveShadows: false);
							}
						}
					}
				}
				if (m_CityBoundaryMeshSystem.GetBoundaryMesh(out var mesh2, out var material2))
				{
					foreach (Camera camera2 in cameras)
					{
						if (camera2.cameraType == CameraType.Game || camera2.cameraType == CameraType.SceneView)
						{
							Graphics.DrawMesh(mesh2, Matrix4x4.identity, material2, 0, camera2, 0, null, castShadows: false, receiveShadows: false);
						}
					}
				}
				if (m_ToolSystem.activeTool != null && m_ToolSystem.activeTool.requireAreas != AreaTypeMask.None)
				{
					for (int j = 0; j < 5; j++)
					{
						if (((uint)m_ToolSystem.activeTool.requireAreas & (uint)(1 << j)) != 0 && m_AreaBufferSystem.GetAreaBuffer((AreaType)j, out buffer, out material3, out bounds))
						{
							num++;
						}
					}
				}
			}
			for (int k = 0; k < batchCount; k++)
			{
				if (m_AreaBatchSystem.GetAreaBatch(k, out buffer, out var _, out var _, out material3, out bounds, out var _, out var _))
				{
					num++;
				}
			}
			if (num == 0)
			{
				return;
			}
			if (m_ArgsBuffer != null && m_ArgsBuffer.count < num)
			{
				m_ArgsBuffer.Release();
				m_ArgsBuffer = null;
			}
			if (m_ArgsBuffer == null)
			{
				m_ArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, num, sizeof(GraphicsBuffer.IndirectDrawIndexedArgs));
			}
			if (m_ArgsArray == null)
			{
				m_ArgsArray = new List<GraphicsBuffer.IndirectDrawIndexedArgs>();
			}
			m_ArgsArray.Clear();
			if (!m_RenderingSystem.hideOverlay && m_ToolSystem.activeTool != null && m_ToolSystem.activeTool.requireAreas != AreaTypeMask.None)
			{
				for (int l = 0; l < 5; l++)
				{
					if (((uint)m_ToolSystem.activeTool.requireAreas & (uint)(1 << l)) == 0 || !m_AreaBufferSystem.GetAreaBuffer((AreaType)l, out var buffer2, out var material4, out var bounds2))
					{
						continue;
					}
					if (m_AreaMesh == null)
					{
						m_AreaMesh = CreateMesh();
					}
					GraphicsBuffer.IndirectDrawIndexedArgs item = new GraphicsBuffer.IndirectDrawIndexedArgs
					{
						indexCountPerInstance = m_AreaMesh.GetIndexCount(0),
						instanceCount = (uint)buffer2.count,
						startIndex = m_AreaMesh.GetIndexStart(0),
						baseVertexIndex = m_AreaMesh.GetBaseVertex(0)
					};
					int count2 = m_ArgsArray.Count;
					m_ArgsArray.Add(item);
					material4.SetBuffer(m_AreaTriangleBuffer, buffer2);
					foreach (Camera camera3 in cameras)
					{
						if (camera3.cameraType == CameraType.Game || camera3.cameraType == CameraType.SceneView)
						{
							RenderParams rparams = new RenderParams(material4);
							rparams.worldBounds = bounds2;
							rparams.camera = camera3;
							Graphics.RenderMeshIndirect(in rparams, m_AreaMesh, m_ArgsBuffer, 1, count2);
						}
					}
				}
			}
			for (int m = 0; m < batchCount; m++)
			{
				if (!m_AreaBatchSystem.GetAreaBatch(m, out var buffer3, out var colors2, out var indices2, out var material5, out var bounds3, out var count3, out var rendererPriority2))
				{
					continue;
				}
				if (m_AreaMesh == null)
				{
					m_AreaMesh = CreateMesh();
				}
				GraphicsBuffer.IndirectDrawIndexedArgs item2 = new GraphicsBuffer.IndirectDrawIndexedArgs
				{
					indexCountPerInstance = m_AreaMesh.GetIndexCount(0),
					instanceCount = (uint)count3,
					startIndex = m_AreaMesh.GetIndexStart(0),
					baseVertexIndex = m_AreaMesh.GetBaseVertex(0)
				};
				int count4 = m_ArgsArray.Count;
				m_ArgsArray.Add(item2);
				material5.SetBuffer(m_AreaBatchBuffer, buffer3);
				material5.SetBuffer(m_AreaBatchColors, colors2);
				material5.SetBuffer(m_VisibleIndices, indices2);
				foreach (Camera camera4 in cameras)
				{
					if (camera4.cameraType != CameraType.Preview)
					{
						RenderParams rparams2 = new RenderParams(material5);
						rparams2.worldBounds = bounds3;
						rparams2.camera = camera4;
						rparams2.rendererPriority = rendererPriority2;
						Graphics.RenderMeshIndirect(in rparams2, m_AreaMesh, m_ArgsBuffer, 1, count4);
					}
				}
			}
			m_ArgsBuffer.SetData(m_ArgsArray, 0, 0, m_ArgsArray.Count);
		}
		finally
		{
		}
	}

	private static Mesh CreateMesh()
	{
		Vector3[] array = new Vector3[6];
		int[] array2 = new int[24];
		int num = 0;
		int num2 = 0;
		array[num++] = new Vector3(0f, 0f, 0f);
		array[num++] = new Vector3(0f, 0f, 1f);
		array[num++] = new Vector3(1f, 0f, 0f);
		array[num++] = new Vector3(0f, 1f, 0f);
		array[num++] = new Vector3(0f, 1f, 1f);
		array[num++] = new Vector3(1f, 1f, 0f);
		array2[num2++] = 0;
		array2[num2++] = 2;
		array2[num2++] = 1;
		array2[num2++] = 3;
		array2[num2++] = 4;
		array2[num2++] = 5;
		array2[num2++] = 3;
		array2[num2++] = 0;
		array2[num2++] = 4;
		array2[num2++] = 4;
		array2[num2++] = 0;
		array2[num2++] = 1;
		array2[num2++] = 4;
		array2[num2++] = 1;
		array2[num2++] = 5;
		array2[num2++] = 5;
		array2[num2++] = 1;
		array2[num2++] = 2;
		array2[num2++] = 5;
		array2[num2++] = 2;
		array2[num2++] = 3;
		array2[num2++] = 3;
		array2[num2++] = 2;
		array2[num2++] = 0;
		return new Mesh
		{
			name = "Area triangle volume",
			vertices = array,
			triangles = array2
		};
	}

	[Preserve]
	public AreaRenderSystem()
	{
	}
}
