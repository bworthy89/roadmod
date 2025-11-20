using System.ComponentModel;

namespace Game.Input;

public enum OptionGroupOverride
{
	None,
	[Description("Navigation")]
	Navigation,
	[Description("Menu")]
	Menu,
	[Description("Camera")]
	Camera,
	[Description("Tool")]
	Tool,
	[Description("Shortcuts")]
	Shortcuts,
	[Description("Photo mode")]
	PhotoMode,
	[Description("Toolbar")]
	Toolbar,
	[Description("Tutorial")]
	Tutorial,
	[Description("Simulation")]
	Simulation,
	[Description("SIP")]
	SIP
}
