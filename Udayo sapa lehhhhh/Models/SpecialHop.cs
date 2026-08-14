namespace Udayo_sapa_lehhhhh.Models
{
    public enum SpecialSquareType
    {
        Ladder,
        Snake,
        Frog
    }

    /// <summary>
    /// One "hop" in a chain of special-square resolutions after a dice move
    /// (e.g. landing on a ladder that leads straight into a frog, which leads into a snake).
    /// </summary>
    public class SpecialHop
    {
        public int FromSquare { get; set; }
        public int ToSquare { get; set; }
        public SpecialSquareType Type { get; set; }
    }
}
