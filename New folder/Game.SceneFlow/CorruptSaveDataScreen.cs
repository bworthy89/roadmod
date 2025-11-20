using System.Threading;
using System.Threading.Tasks;
using Game.Input;

namespace Game.SceneFlow;

public class CorruptSaveDataScreen : FullScreenOverlay
{
	protected override OverlayScreen overlayScreen => OverlayScreen.CorruptSaveData;

	protected override string actionA => "AnyKey";

	public override async Task Execute(GameManager manager, CancellationToken token)
	{
		using EnabledActionScoped continueAction = new EnabledActionScoped(manager, "Engagement", actionA, HandleScreenChange, continueDisplayProperty, continueDisplayPriority);
		using (InputManager.instance.CreateOverlayBarrier("CorruptSaveDataScreen"))
		{
			OverlayBindings overlayBindings = manager.userInterface.overlayBindings;
			using (overlayBindings.ActivateScreenScoped(overlayScreen))
			{
				await IScreenState.WaitForInput(continueAction, null, null, token);
			}
		}
	}
}
