using Game;
using Game.Modding;
using Game.SceneFlow;
using Colossal.Logging;
using OrganicNeighborhood.Systems;

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

            // Phase 2: Register OrganicNeighborhoodToolSystem
            s_Log.Info($"[{ModId}] Registering systems...");
            updateSystem.UpdateAt<OrganicNeighborhoodToolSystem>(
                SystemUpdatePhase.ToolUpdate);
            s_Log.Info($"[{ModId}]   - OrganicNeighborhoodToolSystem registered");

            s_Log.Info($"[{ModId}] Loaded successfully!");
            s_Log.Info($"[{ModId}] Phase 5 COMPLETE: Full road generation pipeline!");
            s_Log.Info($"[{ModId}]   ✅ Organic grid generation (6 layout styles)");
            s_Log.Info($"[{ModId}]   ✅ Terrain awareness (height, slope, water)");
            s_Log.Info($"[{ModId}]   ✅ NetCourse entity creation (in-game roads!)");
            s_Log.Info($"[{ModId}] NOTE: Configure road prefabs via SetRoadPrefabs() to see roads in-game");
            s_Log.Info($"[{ModId}] Next: Phase 6 - UI panel and final polish");
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

                // Cleanup: Systems are automatically disposed by Unity ECS
                // Add manual cleanup here if needed in future phases:
                // - Unregister event handlers (if any)
                // - Dispose of any native containers (Phase 3+)
                // - Clean up any static state (if any)

                s_Log.Info($"[{ModId}] Disposed successfully");
            }
        }
    }
}
