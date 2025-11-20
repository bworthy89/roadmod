using System;

namespace Game.Modding.Toolchain;

[Flags]
public enum DeploymentAction
{
	None = 0,
	Install = 2,
	Update = 4,
	Repair = 8,
	Uninstall = 0x10
}
