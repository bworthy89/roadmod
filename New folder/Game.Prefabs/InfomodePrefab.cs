using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public abstract class InfomodePrefab : PrefabBase
{
	public int m_Priority;

	public virtual string infomodeTypeLocaleKey => "ObjectColor";

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfomodeData>());
	}

	public virtual void GetColors(out Color color0, out Color color1, out Color color2, out float steps, out float speed, out float tiling, out float fill)
	{
		color0 = default(Color);
		color1 = default(Color);
		color2 = default(Color);
		steps = 1f;
		speed = 0f;
		tiling = 0f;
		fill = 0f;
	}

	public virtual int GetColorGroup(out int secondaryGroup)
	{
		secondaryGroup = -1;
		return 2;
	}

	public virtual bool CanActivateBoth(InfomodePrefab other)
	{
		return true;
	}
}
