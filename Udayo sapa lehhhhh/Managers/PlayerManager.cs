using System.Collections.Generic;
using Udayo_sapa_lehhhhh.Models;

namespace Udayo_sapa_lehhhhh.Managers
{
    /// <summary>
    /// Holds the list of players in the current game and tracks whose turn it is.
    /// Pure logic - no UI code.
    /// </summary>
    public class PlayerManager
    {
        public List<Player> Players { get; } = new List<Player>();

        public int CurrentPlayerIndex { get; set; } = 0;

        public Player CurrentPlayer => Players[CurrentPlayerIndex];

        private static readonly string[] TokenColors = { "#3498DB", "#E74C3C", "#2ECC71", "#F1C40F" };
        private static readonly string[] DefaultNames = { "Player 1", "Player 2", "Player 3", "Player 4" };

        /// <summary>Creates 'count' players (2-4) with default names/colors, all off the board.</summary>
        public void SetupPlayers(int count)
        {
            Players.Clear();

            for (int i = 0; i < count; i++)
            {
                var player = new Player(DefaultNames[i], TokenColors[i])
                {
                    CurrentSquare = 0 // 0 = off the board, not yet started
                };
                Players.Add(player);
            }

            CurrentPlayerIndex = 0;
        }

        /// <summary>Advances to the next player's turn, wrapping back to player 0 after the last player.</summary>
        public void NextTurn()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
    }
}
