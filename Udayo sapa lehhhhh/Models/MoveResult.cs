using System.Collections.Generic;

namespace Udayo_sapa_lehhhhh.Models
{
    /// <summary>
    /// The outcome of calculating a single dice-driven move, before any animation happens.
    /// </summary>
    public class MoveResult
    {
        public bool IsValidMove { get; set; }

        /// <summary>Squares to step through one at a time from the dice roll (before any chain).</summary>
        public List<int> Path { get; set; } = new List<int>();

        /// <summary>The square the token lands on directly from the dice roll, before any chain resolution.</summary>
        public int LandingSquare { get; set; }

        /// <summary>Every ladder/snake/frog hop triggered in sequence after landing, in order.</summary>
        public List<SpecialHop> Chain { get; set; } = new List<SpecialHop>();

        /// <summary>The square the token ends up on after the dice move AND the full chain of hops.</summary>
        public int FinalSquare { get; set; }

        /// <summary>True only if the direct dice roll landed exactly on the winning square.</summary>
        public bool IsWin { get; set; }
    }
}
