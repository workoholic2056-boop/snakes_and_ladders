using System.Collections.Generic;

namespace Udayo_sapa_lehhhhh.Models
{
    public class Player
    {
        public string Name { get; set; }
        public string TokenColorHex { get; set; }
        public int CurrentSquare { get; set; } = 0;
        public bool HasWon { get; set; } = false;

        public int TotalTurns { get; set; } = 0;
        public List<int> DiceRollHistory { get; set; } = new List<int>();
        public int LaddersClimbed { get; set; } = 0;
        public int SnakesEncountered { get; set; } = 0;
        public int FrogsHopped { get; set; } = 0;

        public Player(string name, string tokenColorHex)
        {
            Name = name;
            TokenColorHex = tokenColorHex;
        }

        public void Reset()
        {
            CurrentSquare = 0;
            HasWon = false;
            TotalTurns = 0;
            DiceRollHistory.Clear();
            LaddersClimbed = 0;
            SnakesEncountered = 0;
            FrogsHopped = 0;
        }
    }
}
