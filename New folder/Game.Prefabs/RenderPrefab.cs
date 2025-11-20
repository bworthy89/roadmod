using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[] { })]
public class RenderPrefab : RenderPrefabBase
{
	[SerializeField]
	private AssetReference<GeometryAsset> m_GeometryAsset;

	[SerializeField]
	private AssetReference<SurfaceAsset>[] m_SurfaceAssets;

	[SerializeField]
	private Bounds3 m_Bounds;

	[SerializeField]
	private float m_SurfaceArea;

	[SerializeField]
	private int m_IndexCount;

	[SerializeField]
	private int m_VertexCount;

	[SerializeField]
	private int m_MeshCount;

	[SerializeField]
	private bool m_IsImpostor;

	[SerializeField]
	private bool m_ManualVTRequired;

	private Material[] m_MaterialsContainer;

	public bool hasGeometryAsset => m_GeometryAsset != null;

	public GeometryAsset geometryAsset
	{
		get
		{
			return m_GeometryAsset;
		}
		set
		{
			m_GeometryAsset = value;
		}
	}

	public IEnumerable<SurfaceAsset> surfaceAssets
	{
		get
		{
			return ((IEnumerable<AssetReference<SurfaceAsset>>)m_SurfaceAssets).Select((Func<AssetReference<SurfaceAsset>, SurfaceAsset>)((AssetReference<SurfaceAsset> x) => x));
		}
		set
		{
			m_SurfaceAssets = value.Select((SurfaceAsset x) => new AssetReference<SurfaceAsset>(x.id)).ToArray();
		}
	}

	public Bounds3 bounds
	{
		get
		{
			return m_Bounds;
		}
		set
		{
			m_Bounds = value;
		}
	}

	public float surfaceArea
	{
		get
		{
			return m_SurfaceArea;
		}
		set
		{
			m_SurfaceArea = value;
		}
	}

	public int indexCount
	{
		get
		{
			return m_IndexCount;
		}
		set
		{
			m_IndexCount = value;
		}
	}

	public int vertexCount
	{
		get
		{
			return m_VertexCount;
		}
		set
		{
			m_VertexCount = value;
		}
	}

	public int meshCount
	{
		get
		{
			return m_MeshCount;
		}
		set
		{
			m_MeshCount = value;
		}
	}

	public bool isImpostor
	{
		get
		{
			return m_IsImpostor;
		}
		set
		{
			m_IsImpostor = value;
		}
	}

	public bool manualVTRequired
	{
		get
		{
			return m_ManualVTRequired;
		}
		set
		{
			m_ManualVTRequired = value;
		}
	}

	public int materialCount => m_SurfaceAssets.Length;

	public SurfaceAsset GetSurfaceAsset(int index)
	{
		return m_SurfaceAssets[index];
	}

	public void SetSurfaceAsset(int index, SurfaceAsset value)
	{
		m_SurfaceAssets[index] = value;
	}

	public Mesh[] ObtainMeshes()
	{
		ComponentBase.baseLog.TraceFormat(this, "ObtainMeshes {0}", base.name);
		return geometryAsset?.ObtainMeshes();
	}

	public Mesh ObtainMesh(int materialIndex, out int subMeshIndex)
	{
		ComponentBase.baseLog.TraceFormat(this, "ObtainMesh {0}", base.name);
		subMeshIndex = materialIndex;
		if (hasGeometryAsset)
		{
			Mesh[] array = geometryAsset?.ObtainMeshes();
			if (array != null)
			{
				Mesh[] array2 = array;
				foreach (Mesh mesh in array2)
				{
					if (materialIndex < mesh.subMeshCount)
					{
						subMeshIndex = materialIndex;
						return mesh;
					}
					materialIndex -= mesh.subMeshCount;
				}
			}
		}
		return null;
	}

	public void ReleaseMeshes()
	{
		ComponentBase.baseLog.TraceFormat(this, "ReleaseMeshes {0}", base.name);
		geometryAsset?.ReleaseMeshes();
	}

	public Material[] ObtainMaterials(bool useVT = true)
	{
		ComponentBase.baseLog.TraceFormat(this, "ObtainMaterials {0}", base.name);
		if (m_MaterialsContainer == null || m_MaterialsContainer.Length != materialCount)
		{
			m_MaterialsContainer = new Material[materialCount];
		}
		for (int i = 0; i < materialCount; i++)
		{
			SurfaceAsset surfaceAsset = m_SurfaceAssets[i];
			m_MaterialsContainer[i] = surfaceAsset.Load(-1, loadTextures: true, TextureAsset.KeepOnCPU.Dont, useVT);
		}
		return m_MaterialsContainer;
	}

	public Material ObtainMaterial(int i, bool useVT = true)
	{
		Material[] array = ObtainMaterials(useVT);
		if (i < 0 || i >= array.Length)
		{
			throw new IndexOutOfRangeException($"i {i} is out of material range (length: {array.Length}) in {base.name}");
		}
		return array[i];
	}

	public void ReleaseMaterials()
	{
		ComponentBase.baseLog.TraceFormat(this, "ReleaseMaterials {0}", base.name);
	}

	public void Release()
	{
		ReleaseMeshes();
		ReleaseMaterials();
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<MeshData>());
		components.Add(ComponentType.ReadWrite<SharedMeshData>());
		components.Add(ComponentType.ReadWrite<BatchGroup>());
		if (isImpostor)
		{
			components.Add(ComponentType.ReadWrite<ImpostorData>());
		}
	}
}
