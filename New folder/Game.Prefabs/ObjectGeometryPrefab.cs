using System.Collections.Generic;
using Game.Common;
using Game.Objects;
using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

public abstract class ObjectGeometryPrefab : ObjectPrefab
{
	public ObjectMeshInfo[] m_Meshes;

	public bool m_Circular;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Meshes != null)
		{
			for (int i = 0; i < m_Meshes.Length; i++)
			{
				prefabs.Add(m_Meshes[i].m_Mesh);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ObjectGeometryData>());
		components.Add(ComponentType.ReadWrite<SubMesh>());
		bool flag = false;
		bool flag2 = false;
		if (m_Meshes != null)
		{
			for (int i = 0; i < m_Meshes.Length; i++)
			{
				RenderPrefabBase mesh = m_Meshes[i].m_Mesh;
				if (!(mesh == null))
				{
					flag |= mesh.Has<StackProperties>();
					flag2 = flag2 || mesh is CharacterGroup;
				}
			}
		}
		if (flag)
		{
			components.Add(ComponentType.ReadWrite<StackData>());
		}
		if (flag2)
		{
			components.Add(ComponentType.ReadWrite<SubMeshGroup>());
			components.Add(ComponentType.ReadWrite<CharacterElement>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<ObjectGeometry>());
		components.Add(ComponentType.ReadWrite<CullingInfo>());
		components.Add(ComponentType.ReadWrite<MeshBatch>());
		components.Add(ComponentType.ReadWrite<PseudoRandomSeed>());
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		bool flag8 = false;
		if (m_Meshes != null)
		{
			for (int i = 0; i < m_Meshes.Length; i++)
			{
				RenderPrefabBase mesh = m_Meshes[i].m_Mesh;
				if (mesh == null)
				{
					continue;
				}
				flag |= mesh.Has<ColorProperties>();
				flag2 |= mesh.Has<StackProperties>();
				flag6 = flag6 || mesh is CharacterGroup;
				ProceduralAnimationProperties component = mesh.GetComponent<ProceduralAnimationProperties>();
				if (component != null)
				{
					flag3 = true;
					if (component.m_Bones != null)
					{
						for (int j = 0; j < component.m_Bones.Length; j++)
						{
							switch (component.m_Bones[j].m_Type)
							{
							case BoneType.LookAtDirection:
							case BoneType.WindTurbineRotation:
							case BoneType.WindSpeedRotation:
							case BoneType.PoweredRotation:
							case BoneType.TrafficBarrierDirection:
							case BoneType.LookAtRotation:
							case BoneType.LookAtAim:
							case BoneType.LengthwiseLookAtRotation:
							case BoneType.WorkingRotation:
							case BoneType.OperatingRotation:
							case BoneType.LookAtMovementX:
							case BoneType.LookAtMovementY:
							case BoneType.LookAtMovementZ:
							case BoneType.LookAtRotationSide:
							case BoneType.LookAtAimForward:
								flag4 = true;
								break;
							case BoneType.PlaybackLayer0:
							case BoneType.PlaybackLayer1:
							case BoneType.PlaybackLayer2:
							case BoneType.PlaybackLayer3:
							case BoneType.PlaybackLayer4:
							case BoneType.PlaybackLayer5:
							case BoneType.PlaybackLayer6:
							case BoneType.PlaybackLayer7:
								flag8 = true;
								break;
							}
						}
					}
				}
				CharacterProperties component2 = mesh.GetComponent<CharacterProperties>();
				if (component2 != null && !string.IsNullOrEmpty(component2.m_AnimatedPropName))
				{
					flag7 = true;
				}
				if (mesh.GetComponent<EmissiveProperties>() != null)
				{
					flag5 = true;
				}
			}
		}
		if (flag || flag6)
		{
			components.Add(ComponentType.ReadWrite<MeshColor>());
		}
		if (flag2)
		{
			components.Add(ComponentType.ReadWrite<Stack>());
		}
		if (flag3)
		{
			components.Add(ComponentType.ReadWrite<Skeleton>());
			components.Add(ComponentType.ReadWrite<Bone>());
			components.Add(ComponentType.ReadWrite<BoneHistory>());
			if (flag4)
			{
				components.Add(ComponentType.ReadWrite<Momentum>());
			}
			if (flag8)
			{
				components.Add(ComponentType.ReadWrite<PlaybackLayer>());
			}
		}
		else if (flag7)
		{
			components.Add(ComponentType.ReadWrite<Animated>());
		}
		if (flag5)
		{
			components.Add(ComponentType.ReadWrite<Emissive>());
			components.Add(ComponentType.ReadWrite<LightState>());
		}
		if (flag6)
		{
			components.Add(ComponentType.ReadWrite<MeshGroup>());
			components.Add(ComponentType.ReadWrite<Animated>());
		}
	}
}
