using Udayo_sapa_lehhhhh.Models;

namespace Udayo_sapa_lehhhhh.Managers
{
    /// <summary>
    /// Core rules engine: given a starting square and a dice value, figures out
    /// exactly what should happen, including chaining through multiple ladders/
    /// snakes/frogs if one leads directly into another. Pure logic - no UI.
    /// </summary>
    public class GameManager
    {
        public MoveResult CalculateMove(int currentSquare, int diceValue)
        {
            var result = new MoveResult();

            bool hasStarted = currentSquare >= 1;

            if (!hasStarted && GameConfig.RequireOneOrSixToStart && !RollAllowsStart(diceValue))
            {
                result.IsValidMove = false;
                result.LandingSquare = currentSquare;
                result.FinalSquare = currentSquare;
                return result;
            }

            int target = currentSquare + diceValue;

            if (GameConfig.RequireExactLandingToWin && target > GameConfig.WinningSquare)
            {
                result.IsValidMove = false;
                result.LandingSquare = currentSquare;
                result.FinalSquare = currentSquare;
                return result;
            }

            result.IsValidMove = true;
            result.LandingSquare = target;

            for (int square = currentSquare + 1; square <= target; square++)
            {
                result.Path.Add(square);
            }

            result.IsWin = (target == GameConfig.WinningSquare);

            // Resolve any chain of ladder/snake/frog hops starting from the landing square.
            // The safety cap guarantees this always terminates.
            int current = target;
            int hopsResolved = 0;

            while (hopsResolved < GameConfig.MaxChainResolutions)
            {
                if (GameConfig.Ladders.TryGetValue(current, out int ladderDestination))
                {
                    result.Chain.Add(new SpecialHop { FromSquare = current, ToSquare = ladderDestination, Type = SpecialSquareType.Ladder });
                    current = ladderDestination;
                }
                else if (GameConfig.Snakes.TryGetValue(current, out int snakeDestination))
                {
                    result.Chain.Add(new SpecialHop { FromSquare = current, ToSquare = snakeDestination, Type = SpecialSquareType.Snake });
                    current = snakeDestination;
                }
                else if (GameConfig.Frogs.TryGetValue(current, out int frogDestination))
                {
                    result.Chain.Add(new SpecialHop { FromSquare = current, ToSquare = frogDestination, Type = SpecialSquareType.Frog });
                    current = frogDestination;
                }
                else
                {
                    break;
                }

                hopsResolved++;
            }

            result.FinalSquare = current;

            return result;
        }

        private bool RollAllowsStart(int diceValue)
        {
            foreach (int validRoll in GameConfig.ValidStartingRolls)
            {
                if (diceValue == validRoll) return true;
            }
            return false;
        }
    }
}
