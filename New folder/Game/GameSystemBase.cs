using System;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.SceneFlow;
using Game.Serialization;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game;

public abstract class GameSystemBase : COSystemBase
{
	private LoadGameSystem m_LoadGameSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		if (base.World == World.DefaultGameObjectInjectionWorld)
		{
			m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
			LoadGameSystem loadGameSystem = m_LoadGameSystem;
			loadGameSystem.onOnSaveGameLoaded = (LoadGameSystem.EventGameLoaded)Delegate.Combine(loadGameSystem.onOnSaveGameLoaded, new LoadGameSystem.EventGameLoaded(GameLoaded));
		}
		GameManager.instance.onWorldReady += WorldReady;
		GameManager.instance.onGamePreload += GamePreload;
		GameManager.instance.onGameLoadingComplete += GameLoadingComplete;
		Application.focusChanged += FocusChanged;
	}

	private void FocusChanged(bool hasfocus)
	{
		try
		{
			OnFocusChanged(hasfocus);
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception, GetType().Name + ": Error on Focus change");
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		GameManager.instance.onWorldReady -= WorldReady;
		GameManager.instance.onGamePreload -= GamePreload;
		GameManager.instance.onGameLoadingComplete -= GameLoadingComplete;
		if (base.World == World.DefaultGameObjectInjectionWorld && m_LoadGameSystem != null)
		{
			LoadGameSystem loadGameSystem = m_LoadGameSystem;
			loadGameSystem.onOnSaveGameLoaded = (LoadGameSystem.EventGameLoaded)Delegate.Remove(loadGameSystem.onOnSaveGameLoaded, new LoadGameSystem.EventGameLoaded(GameLoaded));
		}
		Application.focusChanged -= FocusChanged;
		base.OnDestroy();
	}

	private void GameLoadingComplete(Purpose purpose, GameMode mode)
	{
		try
		{
			OnGameLoadingComplete(purpose, mode);
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception, GetType().Name + ": Error on state change, disabling system...");
		}
	}

	private void GameLoaded(Context serializationContext)
	{
		try
		{
			OnGameLoaded(serializationContext);
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception, GetType().Name + ": Error on game load, disabling system...");
			base.Enabled = false;
		}
	}

	private void GamePreload(Purpose purpose, GameMode mode)
	{
		try
		{
			OnGamePreload(purpose, mode);
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception, GetType().Name + ": Error on game preload, disabling system...");
			base.Enabled = false;
		}
	}

	private void WorldReady()
	{
		try
		{
			OnWorldReady();
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception, GetType().Name + ": Error on game preload, disabling system...");
			base.Enabled = false;
		}
	}

	protected virtual void OnWorldReady()
	{
	}

	protected virtual void OnGamePreload(Purpose purpose, GameMode mode)
	{
	}

	protected virtual void OnGameLoaded(Context serializationContext)
	{
	}

	protected virtual void OnGameLoadingComplete(Purpose purpose, GameMode mode)
	{
	}

	protected virtual void OnFocusChanged(bool hasFocus)
	{
	}

	public virtual int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 1;
	}

	public virtual int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return -1;
	}

	public void ResetDependency()
	{
		base.Dependency = default(JobHandle);
	}

	[Preserve]
	protected GameSystemBase()
	{
	}
}
