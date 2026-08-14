namespace Udayo_sapa_lehhhhh.Models
{
    /// <summary>
    /// Central configuration for the entire game.
    /// Change values here instead of hunting through the codebase.
    /// </summary>
    public static class GameConfig
    {
        // ---------- BOARD SETTINGS ----------

        public const int BoardSize = 10;
        public const int TotalSquares = BoardSize * BoardSize; // 100
        public const int WinningSquare = TotalSquares; // 100

        public const bool RequireExactLandingToWin = true;

        public const bool RequireOneOrSixToStart = true;
        public static readonly int[] ValidStartingRolls = { 1, 6 };

        public const int MaxChainResolutions = 10;

        // ---------- PLAYER SETTINGS ----------

        public const int MinPlayers = 2;
        public const int MaxPlayers = 4;

        // ---------- DICE SETTINGS ----------

        public const int DiceMinValue = 1;
        public const int DiceMaxValue = 6;
        public const int DiceAnimationDurationMs = 800;

        public static int? DebugForcedDiceValue = null;

        // ---------- ANIMATION SETTINGS ----------

        public const int TokenMoveStepDurationMs = 200;
        public const int SnakeOrLadderAnimationDurationMs = 600;
        public const int PreAnimationPauseMs = 300;

        // ---------- AUDIO SETTINGS ----------

        public const double DefaultSoundEffectVolume = 0.8;
        public const double DefaultMusicVolume = 0.4;

        // Place actual sound files with these exact names inside Assets/Sounds.
        // Missing files are skipped silently - the game works fine without them.
        // Prefer canonical filenames (single extension). AudioManager will
        // attempt to locate files with different extensions if the exact name
        // isn't present, so it's safe to use a clean name here.
        private const string SoundsFolder = "Assets/";
        public const string DiceRollSoundPath = SoundsFolder + "dice_roll.mp3";
        public const string LadderClimbSoundPath = SoundsFolder + "ladder_climb.wav";
        public const string SnakeSlideSoundPath = SoundsFolder + "snake_slide.wav";
        public const string FrogJumpSoundPath = SoundsFolder + "frog_jump.wav";
        // Canonical local victory audio file. Place a licensed audio file named
        // 'victory.mp3' into the Assets folder. The project copies Assets to
        // output so the file will be available at runtime.
        //
        // Legal / conversion notes:
        // 1) Only use audio you own or are licensed to use. Do NOT download or
        //    reuse copyrighted material unless you have permission.
        // 2) If you have a YouTube video you own/are allowed to extract audio
        //    from, use a legal tool to export the audio to a local file (e.g.
        //    an MP3). Keep a record of the license/permission.
        // 3) Place the resulting file at: Assets\\victory.mp3
        // 4) In Solution Explorer, confirm the file's Properties: Build Action =
        //    Content, Copy to Output Directory = PreserveNewest (project is
        //    already configured to copy Assets/** by default).
        public const string VictorySoundPath = SoundsFolder + "victory.mp3";
        // Optional fallback URL (left for reference) - not required for local playback.
        public const string VictoryMusicUrl = "https://youtu.be/QUys6sX-UWk";
        public const string ButtonClickSoundPath = SoundsFolder + "button_click.wav";
        public const string MoveStepSoundPath = SoundsFolder + "move_step.wav";

        // ---------- SNAKES, LADDERS, FROGS ----------

        public static readonly System.Collections.Generic.Dictionary<int, int> Ladders =
            new System.Collections.Generic.Dictionary<int, int>
            {
                { 2, 38 }, { 7, 14 }, { 8, 31 }, { 15, 26 }, { 21, 42 },
                { 28, 84 }, { 36, 44 }, { 51, 67 }, { 71, 91 }, { 78, 98 }
            };

        public static readonly System.Collections.Generic.Dictionary<int, int> Snakes =
            new System.Collections.Generic.Dictionary<int, int>
            {
                { 16, 6 }, { 47, 26 }, { 49, 11 }, { 56, 53 }, { 62, 19 },
                { 64, 60 }, { 87, 24 }, { 93, 73 }, { 95, 75 }, { 98, 54 }
            };

        /// <summary>Exactly 3 frogs total: 10->13, 67->64 (chains into the 64->60 snake), 82->79.</summary>
        public static readonly System.Collections.Generic.Dictionary<int, int> Frogs =
            new System.Collections.Generic.Dictionary<int, int>
            {
                { 10, 13 },
                { 67, 64 },
                { 82, 79 }
            };
    }
}
