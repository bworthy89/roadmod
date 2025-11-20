using System.Threading;
using System.Threading.Tasks;
using Game.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.SceneFlow;

public class ValidationScreen : FullScreenOverlay
{
	protected override OverlayScreen overlayScreen => OverlayScreen.Validation;

	protected override string continueDisplayProperty => "Proceed";

	protected override string cancelDisplayProperty => "Back";

	protected override int cancelDisplayPriority => 30;

	public override async Task Execute(GameManager manager, CancellationToken token)
	{
		using EnabledActionScoped continueAction = new EnabledActionScoped(manager, "Engagement", actionA, HandleScreenChange, continueDisplayProperty, continueDisplayPriority);
		using EnabledActionScoped cancelAction = new EnabledActionScoped(manager, "Engagement", actionB, HandleScreenChange, cancelDisplayProperty, cancelDisplayPriority);
		using (Game.Input.InputManager.instance.CreateOverlayBarrier("ValidationScreen"))
		{
			OverlayBindings overlayBindings = manager.userInterface.overlayBindings;
			using (overlayBindings.ActivateScreenScoped(overlayScreen))
			{
				Task<(bool ok, InputDevice device)> input = IScreenState.WaitForInput(continueAction, cancelAction, null, token);
				await input;
				if (input.IsCompletedSuccessfully)
				{
					if (input.Result.ok)
					{
						UnityEngine.Debug.Log("OK");
					}
					else
					{
						UnityEngine.Debug.Log("Cancel");
					}
				}
			}
		}
	}
}
