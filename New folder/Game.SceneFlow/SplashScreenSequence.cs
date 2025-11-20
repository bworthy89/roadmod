using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game.Input;
using UnityEngine.InputSystem;

namespace Game.SceneFlow;

public class SplashScreenSequence : IScreenState
{
	private const string kSkipSplashKeyAction = "Skip";

	private Task WaitForCompletion(TimeSpan delay, InputAction anyKey, CancellationToken token)
	{
		return Task.WhenAny(Task.Delay(delay, token), IScreenState.WaitForInput(anyKey, null, null, token));
	}

	public async Task Execute(GameManager manager, CancellationToken token)
	{
		using (EnabledActionScoped anyKey = new EnabledActionScoped(manager, "Splash screen", "Skip", HandleScreenChange))
		{
			using (Game.Input.InputManager.instance.CreateOverlayBarrier("SplashScreenSequence"))
			{
				OverlayBindings overlay = manager.userInterface.overlayBindings;
				OverlayScreen[] splashes = GetSplashSequence().ToArray();
				int i = 0;
				while (i < splashes.Length)
				{
					if (i == 0)
					{
						overlay.ActivateScreen(splashes[i]);
					}
					else
					{
						overlay.SwapScreen(splashes[i - 1], splashes[i]);
					}
					await WaitForCompletion(TimeSpan.FromSeconds(4.0), anyKey, token);
					overlay.DeactivateScreen(splashes[i]);
					token.ThrowIfCancellationRequested();
					int num = i + 1;
					i = num;
				}
				i = 0;
				while (i < 60)
				{
					await Task.Yield();
					int num = i + 1;
					i = num;
				}
			}
		}
		static bool HandleScreenChange(OverlayScreen screen)
		{
			switch (screen)
			{
			case OverlayScreen.Splash1:
			case OverlayScreen.Splash2:
			case OverlayScreen.Splash3:
			case OverlayScreen.Splash4:
			case OverlayScreen.PiracyDisclaimer:
			case OverlayScreen.PhotosensitivityDisclaimer:
				Game.Input.InputManager.instance.AssociateActionsWithUser(associate: false);
				break;
			case OverlayScreen.None:
			case OverlayScreen.Loading:
				Game.Input.InputManager.instance.AssociateActionsWithUser(associate: true);
				break;
			}
			if (screen != OverlayScreen.Splash1 && screen != OverlayScreen.Splash2 && screen != OverlayScreen.Splash3)
			{
				return screen == OverlayScreen.Splash4;
			}
			return true;
		}
	}

	private IEnumerable<OverlayScreen> GetSplashSequence()
	{
		yield return OverlayScreen.Splash1;
		yield return OverlayScreen.Splash2;
		yield return OverlayScreen.Splash4;
	}
}
