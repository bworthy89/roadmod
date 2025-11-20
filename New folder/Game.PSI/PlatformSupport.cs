using System;
using Colossal.PSI.Common;
using Colossal.PSI.Discord;
using Colossal.PSI.Steamworks;

namespace Game.PSI;

public static class PlatformSupport
{
	private const uint kSteamAppId = 949230u;

	public static readonly Func<IPlatformServiceIntegration> kCreateSteamPlatform = () => new SteamworksPlatform(949230u);

	private const long kDiscordClientId = 1125009418476605441L;

	public static readonly Func<IPlatformServiceIntegration> kCreateDiscordRichPresence = () => new DiscordRichPresence(1125009418476605441L);
}
