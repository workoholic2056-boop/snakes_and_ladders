using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows.Media;
using Udayo_sapa_lehhhhh.Models;

namespace Udayo_sapa_lehhhhh.Managers
{
    /// <summary>
    /// Centralized sound playback. Every sound is loaded once, the first time it's
    /// played, and reused after that - this avoids a timing issue where calling
    /// Play() immediately after Open() can be silently ignored before the file has
    /// finished loading. Missing sound files are skipped silently - the game works
    /// fine without them.
    ///
    /// To add real sounds: drop matching .wav/.mp3 files into Assets using the
    /// filenames listed in GameConfig; the project includes Assets as Content so
    /// they will be copied to the output folder.
    /// </summary>
    public class AudioManager
    {
        private readonly Dictionary<string, MediaPlayer> _players = new Dictionary<string, MediaPlayer>();

        public void PlayDiceRoll() => Play(GameConfig.DiceRollSoundPath);
        public void PlayLadderClimb() => Play(GameConfig.LadderClimbSoundPath);
        public void PlaySnakeSlide() => Play(GameConfig.SnakeSlideSoundPath);
        public void PlayFrogJump() => Play(GameConfig.FrogJumpSoundPath);
        public void PlayVictory() => PlayWithFallback(GameConfig.VictorySoundPath, GameConfig.VictoryMusicUrl);
        public void PlayButtonClick() => Play(GameConfig.ButtonClickSoundPath);
        public void PlayMoveStep() => Play(GameConfig.MoveStepSoundPath);

        private bool TryResolveFile(string relativePath, out string resolvedFullPath)
        {
            resolvedFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            if (File.Exists(resolvedFullPath)) return true;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string requestedDir = Path.Combine(baseDir, Path.GetDirectoryName(relativePath) ?? "");
                string requestedFile = Path.GetFileName(relativePath) ?? "";

                string coreName = requestedFile;
                while (Path.HasExtension(coreName)) coreName = Path.GetFileNameWithoutExtension(coreName);

                if (Directory.Exists(requestedDir))
                {
                    foreach (var f in Directory.GetFiles(requestedDir))
                    {
                        string candidate = Path.GetFileName(f) ?? "";
                        while (Path.HasExtension(candidate)) candidate = Path.GetFileNameWithoutExtension(candidate);

                        if (candidate.Equals(coreName, StringComparison.OrdinalIgnoreCase))
                        {
                            resolvedFullPath = f;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // ignore IO errors and fall through
            }

            return false;
        }

        private void Play(string relativePath)
        {
            try
            {
                if (!TryResolveFile(relativePath, out string fullPath))
                {
                    return; // no local file found - skip silently
                }

                // Debug: report which local file is being played (helps confirm offline playback)
                try { Debug.WriteLine($"AudioManager: Playing local audio: {fullPath}"); } catch { }

                if (_players.TryGetValue(fullPath, out MediaPlayer existing))
                {
                    existing.Stop();
                    existing.Position = TimeSpan.Zero;
                    existing.Play();
                    return;
                }

                var player = new MediaPlayer { Volume = GameConfig.DefaultSoundEffectVolume };
                player.MediaOpened += (object? s, EventArgs e) =>
                {
                    player.Position = TimeSpan.Zero;
                    player.Play();
                };

                player.Open(new Uri(fullPath, UriKind.Absolute));
                _players[fullPath] = player;
            }
            catch
            {
                // never let audio failures crash gameplay
            }
        }

        private void PlayWithFallback(string relativePath, string fallbackUrl)
        {
            try
            {
                if (TryResolveFile(relativePath, out string fullPath))
                {
                    try { Debug.WriteLine($"AudioManager: PlayWithFallback resolved local file: {fullPath}"); } catch { }
                    Play(relativePath);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(fallbackUrl))
                {
                    try
                    {
                        try { Debug.WriteLine($"AudioManager: No local victory file found, opening fallback URL: {fallbackUrl}"); } catch { }
                        Process.Start(new ProcessStartInfo { FileName = fallbackUrl, UseShellExecute = true });
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            catch
            {
                // swallow
            }
        }
    }
}

