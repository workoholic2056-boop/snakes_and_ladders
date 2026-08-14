namespace Udayo_sapa_lehhhhh.Models
{
    public static class BoardSquare
    {
        public static (int Row, int Column) GetGridPosition(int squareNumber)
        {
            if (squareNumber < 1 || squareNumber > GameConfig.TotalSquares)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(squareNumber),
                    $"Square number must be between 1 and {GameConfig.TotalSquares}.");
            }

            int zeroBased = squareNumber - 1;
            int row = zeroBased / GameConfig.BoardSize;
            int indexInRow = zeroBased % GameConfig.BoardSize;

            int column = (row % 2 == 0) ? indexInRow : (GameConfig.BoardSize - 1 - indexInRow);

            return (row, column);
        }

        public static int GetSquareNumber(int row, int column)
        {
            int indexInRow = (row % 2 == 0) ? column : (GameConfig.BoardSize - 1 - column);
            return (row * GameConfig.BoardSize) + indexInRow + 1;
        }

        public static bool HasLadder(int squareNumber) => GameConfig.Ladders.ContainsKey(squareNumber);

        public static bool HasSnake(int squareNumber) => GameConfig.Snakes.ContainsKey(squareNumber);

        public static bool HasFrog(int squareNumber) => GameConfig.Frogs.ContainsKey(squareNumber);
    }
}
