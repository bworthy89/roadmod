using System;
using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Tools;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tools/", new Type[]
{
	typeof(NetPrefab),
	typeof(ObjectPrefab)
})]
public class EditorContainer : ComponentBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<EditorContainerData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (components.Contains(ComponentType.ReadWrite<Edge>()))
		{
			components.Add(ComponentType.ReadWrite<Game.Tools.EditorContainer>());
			components.Add(ComponentType.ReadWrite<Game.Net.SubLane>());
			components.Add(ComponentType.ReadWrite<Curve>());
			components.Add(ComponentType.ReadWrite<CullingInfo>());
			components.Add(ComponentType.ReadWrite<PseudoRandomSeed>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Node>()))
		{
			components.Add(ComponentType.ReadWrite<Game.Tools.EditorContainer>());
			components.Add(ComponentType.ReadWrite<CullingInfo>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Game.Objects.Object>()))
		{
			components.Add(ComponentType.ReadWrite<Game.Tools.EditorContainer>());
			components.Add(ComponentType.ReadWrite<Game.Objects.SubObject>());
			components.Add(ComponentType.ReadWrite<Static>());
			components.Add(ComponentType.ReadWrite<CullingInfo>());
		}
	}
}
