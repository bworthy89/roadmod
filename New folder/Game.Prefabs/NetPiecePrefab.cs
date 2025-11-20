using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Net/Piece/", new Type[] { })]
public class NetPiecePrefab : RenderPrefab
{
	public NetPieceLayer m_Layer;

	public float m_Width = 3f;

	public float m_Length = 64f;

	public Bounds1 m_HeightRange = new Bounds1(0f, 3f);

	public float m_WidthOffset;

	public float m_NodeOffset = 0.5f;

	public float m_SideConnectionOffset;

	public float4 m_SurfaceHeights = 0f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<NetPieceData>());
		components.Add(ComponentType.ReadWrite<MeshMaterial>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (!TryGet<LodProperties>(out var component) || component.m_LodMeshes == null)
		{
			return;
		}
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		for (int i = 0; i < component.m_LodMeshes.Length; i++)
		{
			Entity entity2 = orCreateSystemManaged.GetEntity(component.m_LodMeshes[i]);
			if (!entityManager.HasBuffer<MeshMaterial>(entity2))
			{
				entityManager.AddComponent<MeshMaterial>(entity2);
			}
		}
	}
}
