using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Udayo_sapa_lehhhhh.Models;
using Udayo_sapa_lehhhhh.Managers;

namespace Udayo_sapa_lehhhhh
{
    public partial class MainWindow : Window
    {
        private const double SquareSize = 55;
        private const double HumanTokenWidth = 14;
        private const double HumanTokenHeight = 20;

        private readonly PlayerManager _playerManager = new PlayerManager();
        private readonly DiceManager _diceManager = new DiceManager();
        private readonly GameManager _gameManager = new GameManager();
        private readonly AudioManager _audioManager = new AudioManager();

        private bool _isDiceRolling = false;
        private bool _isGameOver = false;
        private int _selectedPlayerCount = 4;

        private Player? _animatingPlayer = null;

        private readonly Random _visualRandom = new Random();

        private static readonly SolidColorBrush SelectedColor = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
        private static readonly SolidColorBrush UnselectedColor = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x52));

        public MainWindow()
        {
            InitializeComponent();

            BuildBoardGrid();
            BuildTokenGridStructure();
            // Keep overlay canvases sized to the rendered board in case the
            // Viewbox or window scaling changes – update sizes whenever the
            // board's layout changes so token coordinates stay aligned.
            BoardGrid.SizeChanged += (s, e) => AdjustOverlaySizes();
            Loaded += (s, e) => AdjustOverlaySizes();
            DrawSnakesAndLadders();

            SetSelectedPlayerCount(4);
        }

        // ---------- MAIN MENU ----------

        private void PlayerCountButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            int count = int.Parse((string)button.Tag);
            SetSelectedPlayerCount(count);
        }

        private void SetSelectedPlayerCount(int count)
        {
            _selectedPlayerCount = count;
            TwoPlayersButton.Background = (count == 2) ? SelectedColor : UnselectedColor;
            ThreePlayersButton.Background = (count == 3) ? SelectedColor : UnselectedColor;
            FourPlayersButton.Background = (count == 4) ? SelectedColor : UnselectedColor;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            _audioManager.PlayButtonClick();
            MenuOverlay.Visibility = Visibility.Collapsed;
            GameRootGrid.Visibility = Visibility.Visible;
            StartNewGame();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ---------- GAME OVER SCREEN ----------

        private void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            _audioManager.PlayButtonClick();
            GameOverOverlay.Visibility = Visibility.Collapsed;
            StartNewGame();
        }

        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _audioManager.PlayButtonClick();
            GameOverOverlay.Visibility = Visibility.Collapsed;
            GameRootGrid.Visibility = Visibility.Collapsed;
            MenuOverlay.Visibility = Visibility.Visible;
        }

        // ---------- GAME SETUP ----------

        private void StartNewGame()
        {
            _isGameOver = false;
            _isDiceRolling = false;
            _animatingPlayer = null;

            _playerManager.SetupPlayers(_selectedPlayerCount);

            RollDiceButton.IsEnabled = true;
            DiceValueText.Text = "1";
            StatusText.Text = $"{_playerManager.CurrentPlayer.Name}'s turn. Roll a 1 or 6 to start!";

            RenderTokens();
            UpdateCurrentPlayerDisplay();
        }

        // ---------- DICE ----------

        private void RollDiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDiceRolling || _isGameOver) return;

            _isDiceRolling = true;
            RollDiceButton.IsEnabled = false;

            _audioManager.PlayDiceRoll();

            int finalValue = _diceManager.Roll();
            AnimateDiceRoll(finalValue);
        }

        private void AnimateDiceRoll(int finalValue)
        {
            int ticksElapsed = 0;
            int totalTicks = GameConfig.DiceAnimationDurationMs / 60;

            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };

            timer.Tick += (s, args) =>
            {
                ticksElapsed++;

                if (ticksElapsed >= totalTicks)
                {
                    timer.Stop();
                    DiceValueText.Text = finalValue.ToString();
                    ProcessMove(finalValue);
                }
                else
                {
                    DiceValueText.Text = _visualRandom.Next(GameConfig.DiceMinValue, GameConfig.DiceMaxValue + 1).ToString();
                }
            };

            timer.Start();
        }

        // ---------- MOVEMENT / GAME RULES ----------

        private void ProcessMove(int diceValue)
        {
            Player current = _playerManager.CurrentPlayer;
            current.DiceRollHistory.Add(diceValue);
            current.TotalTurns++;

            bool hasStarted = current.CurrentSquare >= 1;

            MoveResult move = _gameManager.CalculateMove(current.CurrentSquare, diceValue);

            if (!move.IsValidMove)
            {
                if (!hasStarted)
                    StatusText.Text = $"{current.Name} rolled a {diceValue} - needs a 1 or 6 to enter the board. Turn skipped.";
                else
                    StatusText.Text = $"{current.Name} rolled a {diceValue} - needs exact {GameConfig.WinningSquare} to move. Turn skipped.";

                EndTurn(isWin: false);
                return;
            }

            if (!hasStarted)
                StatusText.Text = $"{current.Name} rolled a {diceValue} and enters the board!";
            else
                StatusText.Text = $"{current.Name} rolled a {diceValue}.";

            MovePlayerStepByStep(current, move.Path, () =>
            {
                PlayChainSequentially(current, move.Chain, 0, () => EndTurn(move.IsWin));
            });
        }

        private void MovePlayerStepByStep(Player player, List<int> path, Action onComplete)
        {
            int index = 0;

            DispatcherTimer moveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(GameConfig.TokenMoveStepDurationMs)
            };

            moveTimer.Tick += (s, e) =>
            {
                if (index >= path.Count)
                {
                    moveTimer.Stop();
                    onComplete();
                    return;
                }

                player.CurrentSquare = path[index];
                RenderTokens();
                _audioManager.PlayMoveStep(); // NEW: short sound on every square-to-square hop
                index++;
            };

            moveTimer.Start();
        }

        private void PlayChainSequentially(Player player, List<SpecialHop> chain, int index, Action onAllComplete)
        {
            if (index >= chain.Count)
            {
                onAllComplete();
                return;
            }

            SpecialHop hop = chain[index];

            switch (hop.Type)
            {
                case SpecialSquareType.Ladder:
                    player.LaddersClimbed++;
                    StatusText.Text = $"{player.Name} found a ladder! Climbing to {hop.ToSquare}.";
                    _audioManager.PlayLadderClimb();
                    PauseThenJump(player, hop.ToSquare, () => PlayChainSequentially(player, chain, index + 1, onAllComplete));
                    break;

                case SpecialSquareType.Snake:
                    player.SnakesEncountered++;
                    StatusText.Text = $"{player.Name} got eaten by a snake! Sliding down to {hop.ToSquare}.";
                    AnimateSnakeSlide(player, hop.FromSquare, hop.ToSquare, () => PlayChainSequentially(player, chain, index + 1, onAllComplete));
                    break;

                case SpecialSquareType.Frog:
                    player.FrogsHopped++;
                    StatusText.Text = $"{player.Name} landed on a frog! Jumping to {hop.ToSquare}.";
                    AnimateFrogJump(player, hop.FromSquare, hop.ToSquare, () => PlayChainSequentially(player, chain, index + 1, onAllComplete));
                    break;
            }
        }

        private void PauseThenJump(Player player, int destinationSquare, Action onComplete)
        {
            DispatcherTimer pauseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(GameConfig.PreAnimationPauseMs)
            };

            pauseTimer.Tick += (s, e) =>
            {
                pauseTimer.Stop();

                player.CurrentSquare = destinationSquare;
                RenderTokens();

                DispatcherTimer settleTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(GameConfig.SnakeOrLadderAnimationDurationMs)
                };

                settleTimer.Tick += (s2, e2) =>
                {
                    settleTimer.Stop();
                    onComplete();
                };

                settleTimer.Start();
            };

            pauseTimer.Start();
        }

        private void AnimateSnakeSlide(Player player, int fromSquare, int toSquare, Action onComplete)
        {
            _audioManager.PlaySnakeSlide();

            Point head = GetSquareCenterPoint(fromSquare);
            Point tail = GetSquareCenterPoint(toSquare);
            List<Point> path = BuildSnakeWavePoints(head, tail);

            _animatingPlayer = player;
            RenderTokens();

            FrameworkElement travelingToken = CreateHumanToken(player.TokenColorHex);
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            travelingToken.RenderTransform = scaleTransform;
            travelingToken.RenderTransformOrigin = new Point(0.5, 0.5);
            TokenGrid.Children.Add(travelingToken);

            int index = 0;
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(35) };

            timer.Tick += (s, e) =>
            {
                if (index >= path.Count)
                {
                    timer.Stop();
                    TokenGrid.Children.Remove(travelingToken);
                    player.CurrentSquare = toSquare;
                    _animatingPlayer = null;
                    RenderTokens();
                    onComplete();
                    return;
                }

                Point p = path[index];
                Canvas.SetLeft(travelingToken, p.X - HumanTokenWidth / 2);
                Canvas.SetTop(travelingToken, p.Y - HumanTokenHeight / 2);

                double progress = (double)index / Math.Max(1, path.Count - 1);
                double scale = 1.0 - (progress * 0.4);
                scaleTransform.ScaleX = scale;
                scaleTransform.ScaleY = scale;

                index++;
            };

            timer.Start();
        }

        private void AnimateFrogJump(Player player, int fromSquare, int toSquare, Action onComplete)
        {
            _audioManager.PlayFrogJump();

            Point start = GetSquareCenterPoint(fromSquare);
            Point end = GetSquareCenterPoint(toSquare);

            _animatingPlayer = player;
            RenderTokens();

            FrameworkElement travelingToken = CreateHumanToken(player.TokenColorHex);
            TokenGrid.Children.Add(travelingToken);

            const int steps = 20;
            const double arcHeight = 30;
            int index = 0;

            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };

            timer.Tick += (s, e) =>
            {
                if (index > steps)
                {
                    timer.Stop();
                    TokenGrid.Children.Remove(travelingToken);
                    player.CurrentSquare = toSquare;
                    _animatingPlayer = null;
                    RenderTokens();
                    onComplete();
                    return;
                }

                double t = (double)index / steps;
                double x = start.X + (end.X - start.X) * t;
                double y = start.Y + (end.Y - start.Y) * t - (Math.Sin(t * Math.PI) * arcHeight);

                Canvas.SetLeft(travelingToken, x - HumanTokenWidth / 2);
                Canvas.SetTop(travelingToken, y - HumanTokenHeight / 2);

                index++;
            };

            timer.Start();
        }

        private void EndTurn(bool isWin)
        {
            Player current = _playerManager.CurrentPlayer;

            if (isWin)
            {
                current.HasWon = true;
                _isDiceRolling = false;
                _isGameOver = true;
                RollDiceButton.IsEnabled = false;
                _audioManager.PlayVictory();
                ShowGameOverScreen(current);
                return;
            }

            _playerManager.NextTurn();
            UpdateCurrentPlayerDisplay();

            _isDiceRolling = false;
            RollDiceButton.IsEnabled = true;
        }

        private void ShowGameOverScreen(Player winner)
        {
            WinnerText.Text = $"🏆 {winner.Name} WINS!";

            StatsText.Text =
                $"Total turns: {winner.TotalTurns}\n" +
                $"Dice rolls: {winner.DiceRollHistory.Count}\n" +
                $"Ladders climbed: {winner.LaddersClimbed}\n" +
                $"Snakes encountered: {winner.SnakesEncountered}\n" +
                $"Frogs hopped: {winner.FrogsHopped}";

            GameOverOverlay.Visibility = Visibility.Visible;
        }

        private void UpdateCurrentPlayerDisplay()
        {
            CurrentPlayerText.Text = _playerManager.CurrentPlayer.Name;
        }

        // ---------- PLAYER LIST (side panel) ----------

        private void UpdatePlayerListDisplay()
        {
            PlayerListPanel.Children.Clear();

            foreach (Player player in _playerManager.Players)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

                var dot = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = (Brush)new BrushConverter().ConvertFromString(player.TokenColorHex),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };

                string positionLabel = player.CurrentSquare < 1 ? "Not started" : $"Square {player.CurrentSquare}";

                var label = new TextBlock
                {
                    Text = $"{player.Name} — {positionLabel}",
                    Foreground = Brushes.White,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                };

                row.Children.Add(dot);
                row.Children.Add(label);
                PlayerListPanel.Children.Add(row);
            }
        }

        // ---------- BOARD ----------

        private void BuildBoardGrid()
        {
            for (int i = 0; i < GameConfig.BoardSize; i++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(SquareSize) });
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(SquareSize) });
            }

            for (int squareNumber = 1; squareNumber <= GameConfig.TotalSquares; squareNumber++)
            {
                var (logicalRow, column) = BoardSquare.GetGridPosition(squareNumber);
                int visualRow = GameConfig.BoardSize - 1 - logicalRow;

                Border squareBorder = CreateSquareVisual(squareNumber);
                Grid.SetRow(squareBorder, visualRow);
                Grid.SetColumn(squareBorder, column);
                BoardGrid.Children.Add(squareBorder);
            }
        }

        private void BuildTokenGridStructure()
        {
            // Initial logical size based on square size; actual rendered size may
            // differ once WPF measures/layouts the Viewbox, so we also update
            // sizes on SizeChanged (see constructor).
            double logicalSize = GameConfig.BoardSize * SquareSize;
            OverlayCanvas.Width = logicalSize;
            OverlayCanvas.Height = logicalSize;

            TokenGrid.Width = logicalSize;
            TokenGrid.Height = logicalSize;
            Panel.SetZIndex(TokenGrid, 100); // ensure tokens are on top
        }

        private void AdjustOverlaySizes()
        {
            // Use the measured size of the BoardGrid so overlay canvases align
            // perfectly with the visible board even after Viewbox scaling.
            double w = BoardGrid.ActualWidth;
            double h = BoardGrid.ActualHeight;

            if (w > 0 && h > 0)
            {
                OverlayCanvas.Width = w;
                OverlayCanvas.Height = h;
                TokenGrid.Width = w;
                TokenGrid.Height = h;

                // Also redraw snakes/ladders to match new scale
                DrawSnakesAndLadders();
                RenderTokens();
            }
        }

        private Border CreateSquareVisual(int squareNumber)
        {
            Brush background = (squareNumber % 2 == 0)
                ? new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x52))
                : new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x48));

            var border = new Border
            {
                Background = background,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x6B)),
                BorderThickness = new Thickness(0.5)
            };

            var text = new TextBlock
            {
                Text = squareNumber.ToString(),
                Foreground = Brushes.White,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3, 1, 0, 0)
            };

            border.Child = text;
            return border;
        }

        private Point GetSquareCenterPoint(int squareNumber)
        {
            var (logicalRow, column) = BoardSquare.GetGridPosition(squareNumber);
            int visualRow = GameConfig.BoardSize - 1 - logicalRow;

            double x = (column * SquareSize) + (SquareSize / 2);
            double y = (visualRow * SquareSize) + (SquareSize / 2);

            return new Point(x, y);
        }

        private void DrawSnakesAndLadders()
        {
            OverlayCanvas.Children.Clear();

            foreach (var ladder in GameConfig.Ladders)
                DrawLadder(GetSquareCenterPoint(ladder.Key), GetSquareCenterPoint(ladder.Value));

            foreach (var snake in GameConfig.Snakes)
                DrawSnake(GetSquareCenterPoint(snake.Key), GetSquareCenterPoint(snake.Value));

            foreach (var frog in GameConfig.Frogs)
                DrawFrog(GetSquareCenterPoint(frog.Key), GetSquareCenterPoint(frog.Value));
        }

        private void DrawLadder(Point bottom, Point top)
        {
            var railBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x5A, 0x2B));

            double dx = top.X - bottom.X;
            double dy = top.Y - bottom.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001) length = 0.001;

            double ux = dx / length;
            double uy = dy / length;
            double px = -uy;
            double py = ux;

            double railGap = 8;

            Point rail1Start = new Point(bottom.X + px * railGap, bottom.Y + py * railGap);
            Point rail1End = new Point(top.X + px * railGap, top.Y + py * railGap);
            Point rail2Start = new Point(bottom.X - px * railGap, bottom.Y - py * railGap);
            Point rail2End = new Point(top.X - px * railGap, top.Y - py * railGap);

            AddLine(rail1Start, rail1End, railBrush, 4);
            AddLine(rail2Start, rail2End, railBrush, 4);

            int rungCount = Math.Max(3, (int)(length / 14));
            for (int i = 0; i <= rungCount; i++)
            {
                double t = (double)i / rungCount;
                Point rungOnRail1 = Lerp(rail1Start, rail1End, t);
                Point rungOnRail2 = Lerp(rail2Start, rail2End, t);
                AddLine(rungOnRail1, rungOnRail2, railBrush, 3);
            }
        }

        private void DrawSnake(Point head, Point tail)
        {
            var bodyBrush = new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47));

            var points = new PointCollection(BuildSnakeWavePoints(head, tail));

            var polyline = new Polyline
            {
                Points = points,
                Stroke = bodyBrush,
                StrokeThickness = 8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            OverlayCanvas.Children.Add(polyline);

            var headMarker = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = bodyBrush,
                Stroke = Brushes.White,
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(headMarker, head.X - 8);
            Canvas.SetTop(headMarker, head.Y - 8);
            OverlayCanvas.Children.Add(headMarker);

            double dxE = tail.X - head.X;
            double dyE = tail.Y - head.Y;
            double lenE = Math.Sqrt(dxE * dxE + dyE * dyE);
            if (lenE < 0.001) lenE = 0.001;
            double pxE = -(dyE / lenE);
            double pyE = (dxE / lenE);

            AddEye(head, pxE, pyE, 3.5);
            AddEye(head, pxE, pyE, -3.5);
        }

        private List<Point> BuildSnakeWavePoints(Point head, Point tail)
        {
            double dx = tail.X - head.X;
            double dy = tail.Y - head.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001) length = 0.001;

            double ux = dx / length;
            double uy = dy / length;
            double px = -uy;
            double py = ux;

            int segments = Math.Max(10, (int)(length / 6));
            double waveAmplitude = 10;
            double waveFrequency = 3.0;

            var points = new List<Point>();

            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double baseX = head.X + dx * t;
                double baseY = head.Y + dy * t;
                double wave = Math.Sin(t * Math.PI * waveFrequency) * waveAmplitude;
                points.Add(new Point(baseX + px * wave, baseY + py * wave));
            }

            return points;
        }

        private void AddEye(Point head, double px, double py, double offset)
        {
            var eye = new Ellipse { Width = 3.5, Height = 3.5, Fill = Brushes.Black, Stroke = Brushes.White, StrokeThickness = 0.5 };
            Canvas.SetLeft(eye, head.X + px * offset - 1.75);
            Canvas.SetTop(eye, head.Y + py * offset - 1.75);
            OverlayCanvas.Children.Add(eye);
        }

        private void DrawFrog(Point frogSquare, Point destination)
        {
            var frogBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0xC3, 0x4A));

            var line = new Line
            {
                X1 = frogSquare.X,
                Y1 = frogSquare.Y,
                X2 = destination.X,
                Y2 = destination.Y,
                Stroke = frogBrush,
                StrokeThickness = 4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeDashArray = new DoubleCollection { 1, 3 },
                Opacity = 0.9
            };
            OverlayCanvas.Children.Add(line);

            var body = new Ellipse { Width = 18, Height = 14, Fill = frogBrush, Stroke = Brushes.White, StrokeThickness = 1.5 };
            Canvas.SetLeft(body, frogSquare.X - 9);
            Canvas.SetTop(body, frogSquare.Y - 5);
            OverlayCanvas.Children.Add(body);

            AddFrogEyeBump(frogSquare, -5);
            AddFrogEyeBump(frogSquare, 5);
        }

        private void AddFrogEyeBump(Point frogSquare, double xOffset)
        {
            var bump = new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Color.FromRgb(0x8B, 0xC3, 0x4A)), Stroke = Brushes.White, StrokeThickness = 1 };
            Canvas.SetLeft(bump, frogSquare.X + xOffset - 2.5);
            Canvas.SetTop(bump, frogSquare.Y - 10);
            OverlayCanvas.Children.Add(bump);

            var pupil = new Ellipse { Width = 2, Height = 2, Fill = Brushes.Black };
            Canvas.SetLeft(pupil, frogSquare.X + xOffset - 1);
            Canvas.SetTop(pupil, frogSquare.Y - 9);
            OverlayCanvas.Children.Add(pupil);
        }

        private void AddLine(Point start, Point end, Brush brush, double thickness)
        {
            var line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = brush,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            OverlayCanvas.Children.Add(line);
        }

        private Point Lerp(Point a, Point b, double t)
        {
            return new Point(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        // ---------- HUMAN TOKENS ----------

        private FrameworkElement CreateHumanToken(string colorHex)
        {
            Brush brush = (Brush)new BrushConverter().ConvertFromString(colorHex);
            var canvas = new Canvas { Width = HumanTokenWidth, Height = HumanTokenHeight };

            double headSize = HumanTokenWidth * 0.55;

            var body = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(HumanTokenWidth * 0.5, headSize * 0.9),
                    new Point(HumanTokenWidth, HumanTokenHeight),
                    new Point(0, HumanTokenHeight)
                },
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            canvas.Children.Add(body);

            var head = new Ellipse
            {
                Width = headSize,
                Height = headSize,
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(head, (HumanTokenWidth - headSize) / 2);
            Canvas.SetTop(head, 0);
            canvas.Children.Add(head);

            return canvas;
        }

        private void RenderTokens()
        {
            TokenGrid.Children.Clear();

            for (int i = 0; i < _playerManager.Players.Count; i++)
            {
                Player player = _playerManager.Players[i];

                if (player == _animatingPlayer) continue;
                if (player.CurrentSquare < 1) continue;

                Point center = GetSquareCenterPoint(player.CurrentSquare);

                double offsetX = (i % 2 == 0) ? -11 : 11;
                double offsetY = (i < 2) ? -11 : 11;

                FrameworkElement token = CreateHumanToken(player.TokenColorHex);

                Canvas.SetLeft(token, center.X + offsetX - HumanTokenWidth / 2);
                Canvas.SetTop(token, center.Y + offsetY - HumanTokenHeight / 2);

                TokenGrid.Children.Add(token);
            }

            UpdatePlayerListDisplay();
        }
    }
}