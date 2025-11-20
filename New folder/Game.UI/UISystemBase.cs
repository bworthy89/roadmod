using System.Collections.Generic;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Colossal.UI;
using Colossal.UI.Binding;
using Game.SceneFlow;
using UnityEngine.Scripting;

namespace Game.UI;

public abstract class UISystemBase : GameSystemBase
{
	protected static ILog log = UIManager.log;

	private List<IBinding> m_Bindings;

	private List<IUpdateBinding> m_UpdateBindings;

	public virtual GameMode gameMode => GameMode.All;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Bindings = new List<IBinding>();
		m_UpdateBindings = new List<IUpdateBinding>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		foreach (IBinding binding in m_Bindings)
		{
			GameManager.instance.userInterface.bindings.RemoveBinding(binding);
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		foreach (IUpdateBinding updateBinding in m_UpdateBindings)
		{
			updateBinding.Update();
		}
	}

	protected void AddBinding(IBinding binding)
	{
		m_Bindings.Add(binding);
		GameManager.instance.userInterface.bindings.AddBinding(binding);
	}

	protected void AddUpdateBinding(IUpdateBinding binding)
	{
		AddBinding(binding);
		m_UpdateBindings.Add(binding);
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = (gameMode & mode) != 0;
	}

	[Preserve]
	protected UISystemBase()
	{
	}
}
