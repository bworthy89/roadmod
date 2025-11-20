using Game;
using Game.Modding;
using Game.SceneFlow;
using Colossal.Logging;

namespace OrganicNeighborhood
{
    /// <summary>
    /// Main mod class - entry point for Organic Neighborhood Layout Tool
    /// Implements Cities: Skylines II IMod interface
    /// </summary>
    public class Mod : IMod
    {
        /// <summary>
        /// Mod identifier for logging and configuration
        /// </summary>
        public const string ModId = "OrganicNeighborhoodMod";

        /// <summary>
        /// Mod version
        /// </summary>
        public const string Version = "0.1.0-alpha";

        /// <summary>
        /// Logger instance for this mod
        /// </summary>
        private static ILog s_Log;

        /// <summary>
        /// Public accessor for logger (used by systems)
        /// </summary>
        public static ILog Log => s_Log;

        /// <summary>
        /// Called by the game when the mod is loaded
        /// This is where we register our systems
        /// </summary>
        /// <param name="updateSystem">Game's update system for registering custom systems</param>
        public void OnLoad(UpdateSystem updateSystem)
        {
            // Initialize logging
            s_Log = LogManager.GetLogger(ModId).SetShowsErrorsInUI(false);
            s_Log.Info($"[{ModId}] Loading version {Version}...");

            // Log Phase 1 completion
            s_Log.Info($"[{ModId}] Phase 1 utilities loaded:");
            s_Log.Info($"[{ModId}]   - BurstPerlinNoise (noise generation)");
            s_Log.Info($"[{ModId}]   - TerrainHelpers (terrain integration)");
            s_Log.Info($"[{ModId}]   - CurveUtils (bezier curves)");
            s_Log.Info($"[{ModId}]   - LayoutParameters (data structures)");

            // TODO Phase 2: Register OrganicNeighborhoodToolSystem
            // When we create the tool system in Phase 2, register it here:
            //
            // s_Log.Info($"[{ModId}] Registering systems...");
            // updateSystem.UpdateAt<OrganicNeighborhoodToolSystem>(
            //     SystemUpdatePhase.ToolUpdate);
            // s_Log.Info($"[{ModId}]   - OrganicNeighborhoodToolSystem registered");

            s_Log.Info($"[{ModId}] Loaded successfully!");
            s_Log.Info($"[{ModId}] Ready for Phase 2: Tool System implementation");
        }

        /// <summary>
        /// Called by the game when the mod is unloaded or disabled
        /// Cleanup resources here
        /// </summary>
        public void OnDispose()
        {
            if (s_Log != null)
            {
                s_Log.Info($"[{ModId}] Disposing...");

                // TODO: Add cleanup code here when we have systems
                // - Unregister event handlers
                // - Dispose of any native containers
                // - Clean up any static state

                s_Log.Info($"[{ModId}] Disposed successfully");
            }
        }
    }
}
