using System;
using Udayo_sapa_lehhhhh.Models;

namespace Udayo_sapa_lehhhhh.Managers
{
    /// <summary>
    /// Handles dice roll logic. Pure logic - no UI, no animation.
    /// Supports an optional debug override for testing.
    /// </summary>
    public class DiceManager
    {
        private readonly Random _random = new Random();

        /// <summary>
        /// Returns a random value between DiceMinValue and DiceMaxValue (inclusive),
        /// unless GameConfig.DebugForcedDiceValue is set, in which case that value is returned instead.
        /// </summary>
        public int Roll()
        {
            if (GameConfig.DebugForcedDiceValue.HasValue)
            {
                return GameConfig.DebugForcedDiceValue.Value;
            }

            return _random.Next(GameConfig.DiceMinValue, GameConfig.DiceMaxValue + 1);
        }
    }
}
