using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class AIRobot
{
    private Player player;
    private StoneType aiStone;
    private StoneType opponentStone;
    private System.Random random = new System.Random();

    public AIRobot(Player player)
    {
        this.player = player;
        this.aiStone = player.Stone;
        this.opponentStone = aiStone == StoneType.Black ? StoneType.White : StoneType.Black;
    }

    public Vector2Int FindBestMove(Grid grid)
    {
        // ���� ù ���� �� ���, �߾� ��ó�� �������� ���� �α�
        if (aiStone == StoneType.Black && IsFirstMove(grid))
        {
            return GetRandomCentralMove(grid);
        }

        int bestValue = int.MinValue;
        Vector2Int bestMove = new Vector2Int(-1, -1);

        for (int y = 0; y < grid.GetSize(); y++)
        {
            for (int x = 0; x < grid.GetSize(); x++)
            {
                if (grid.GetStoneAt(x, y) == StoneType.None)
                {
                    // �ݼ� üũ
                    if (player.IsForbiddenMove(grid, x, y))
                    {
                        continue;
                    }

                    // �� ��ġ�� ���� �ξ��� ���� ������ ���
                    int moveValue = EvaluateMove(grid, x, y);

                    if (moveValue > bestValue)
                    {
                        bestMove = new Vector2Int(x, y);
                        bestValue = moveValue;
                    }
                }
            }
        }

        return bestMove;
    }

    private bool IsFirstMove(Grid grid)
    {
        // ���尡 ����ִ��� Ȯ��
        for (int y = 0; y < grid.GetSize(); y++)
        {
            for (int x = 0; x < grid.GetSize(); x++)
            {
                if (grid.GetStoneAt(x, y) != StoneType.None)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private Vector2Int GetRandomCentralMove(Grid grid)
    {
        int centerX = grid.GetSize() / 2;
        int centerY = grid.GetSize() / 2;

        // �߾� ��ó�� ��ġ���� �����ϰ� ����
        int offsetX = random.Next(-1, 2); // -1, 0, 1 �� �ϳ��� ����
        int offsetY = random.Next(-1, 2);

        int x = Mathf.Clamp(centerX + offsetX, 0, grid.GetSize() - 1);
        int y = Mathf.Clamp(centerY + offsetY, 0, grid.GetSize() - 1);

        return new Vector2Int(x, y);
    }

    private int EvaluateMove(Grid grid, int x, int y)
    {
        int score = 0;

        // AI�� �̱� �� �ִ����� Ȯ���մϴ�.
        if (CheckIfWinningMove(grid, x, y, aiStone))
        {
            return int.MaxValue; // ��� �̱� �� �ִ� �ڸ��� �ְ� ����
        }

        // ������ �̱� �� �ִ����� Ȯ���մϴ�.
        if (CheckIfWinningMove(grid, x, y, opponentStone))
        {
            return int.MaxValue - 1; // ������ �¸��� ���� ���� ���� �ְ� ����
        }

        // ����, ����, �밢�� �������� ���� ���
        score = EvaluateDirection(grid, x, y, 1, 0, aiStone);   // ����
        score = Mathf.Max(score, EvaluateDirection(grid, x, y, 0, 1, aiStone));   // ����
        score = Mathf.Max(score, EvaluateDirection(grid, x, y, 1, 1, aiStone));   // �밢�� (\)
        score = Mathf.Max(score, EvaluateDirection(grid, x, y, 1, -1, aiStone));  // �밢�� (/)

        // ������ ���� ���� �͵� ���
        score = Mathf.Max(score, EvaluateDirection(grid, x, y, 1, 0, opponentStone));   // ����
        score = Mathf.Max(score, EvaluateDirection(grid, x, y, 0, 1, opponentStone));   // ����
        score = Mathf.Max(score, EvaluateDirection(grid, x, y, 1, 1, opponentStone));   // �밢�� (\)
        score = Mathf.Max(score, EvaluateDirection(grid, x, y, 1, -1, opponentStone));  // �밢�� (/)

        // ������ ���� 33, 44, 34 ���Ͽ� ���� �߰� ����
        score = Mathf.Max(score, CheckAdvancedPatterns(grid, x, y, opponentStone));

        return score;
    }
    private bool CheckIfWinningMove(Grid grid, int x, int y, StoneType stone)
    {
        return CheckDirection(grid, x, y, 1, 0, stone) >= 5 ||  // ���� üũ
               CheckDirection(grid, x, y, 0, 1, stone) >= 5 ||  // ���� üũ
               CheckDirection(grid, x, y, 1, 1, stone) >= 5 ||  // �밢�� (\) üũ
               CheckDirection(grid, x, y, 1, -1, stone) >= 5;   // �밢�� (/) üũ
    }

    private int CheckDirection(Grid grid, int x, int y, int dx, int dy, StoneType stone)
    {
        int count = 1;

        // ������ üũ
        int nx = x + dx;
        int ny = y + dy;
        while (grid.IsPositionValid(nx, ny) && grid.GetStoneAt(nx, ny) == stone)
        {
            count++;
            nx += dx;
            ny += dy;
        }

        // �ڷ� üũ
        nx = x - dx;
        ny = y - dy;
        while (grid.IsPositionValid(nx, ny) && grid.GetStoneAt(nx, ny) == stone)
        {
            count++;
            nx -= dx;
            ny -= dy;
        }

        return count;
    }
    private int EvaluateDirection(Grid grid, int x, int y, int dx, int dy, StoneType stone)
    {
        if (stone == StoneType.Black && player.IsBlackCheck(grid, x, y))
            return 0; // �ݼ��� ��� ���� 0 ��ȯ

        int score = 0;
        int consecutive = 0;
        int openEnds = 0;

        // ������ üũ
        for (int i = 1; i <= 5; i++)
        {
            int nx = x + i * dx;
            int ny = y + i * dy;

            if (!grid.IsPositionValid(nx, ny))
                break;

            if (grid.GetStoneAt(nx, ny) == stone)
            {
                consecutive++;
            }
            else if (grid.GetStoneAt(nx, ny) == StoneType.None)
            {
                openEnds++;
                break;
            }
            else
            {
                break;
            }
        }

        score = GetScore(consecutive, openEnds);

        consecutive = 0;
        openEnds = 0;

        // �ڷ� üũ
        for (int i = 1; i < 5; i++)
        {
            int nx = x - i * dx;
            int ny = y - i * dy;

            if (!grid.IsPositionValid(nx, ny))
                break;

            if (grid.GetStoneAt(nx, ny) == stone)
            {
                consecutive++;
            }
            else if (grid.GetStoneAt(nx, ny) == StoneType.None)
            {
                openEnds++;
                break;
            }
            else
            {
                break;
            }
        }

        score = Mathf.Max(score, GetScore(consecutive, openEnds));

        return score;
    }

    private int GetScore(int consecutive, int openEnds)
    {
        // ���� ���
        if (consecutive >= 5)
        {
            return 10000;
        }
        else if (consecutive == 4 && openEnds > 0) // ���� 4
        {
            return 6;
        }
        else if (consecutive == 4 && openEnds == 0) // ���� 4�� �켱 ó��
        {
            return 5;
        }
        else if (consecutive == 3 && openEnds == 2) // ������ ���� 3
        {
            return 4;
        }
        else if (consecutive == 3 && openEnds == 1) // �� ���� ���� 3
        {
            return 3;
        }
        else if (consecutive == 2 && openEnds > 0)
        {
            return 2;
        }
        else if (consecutive == 1 && openEnds > 0)
        {
            return 1;
        }

        return 0;
    }

    private int CheckAdvancedPatterns(Grid grid, int x, int y, StoneType stone)
    {
        int score = 0;

        // 33, 44, 34 ���� üũ
        int openThrees = 0;
        int openFours = 0;

        openThrees += CountOpenPatterns(grid, x, y, 1, 0, 3);
        openThrees += CountOpenPatterns(grid, x, y, 0, 1, 3);
        openThrees += CountOpenPatterns(grid, x, y, 1, 1, 3);
        openThrees += CountOpenPatterns(grid, x, y, 1, -1, 3);

        openFours += CountOpenPatterns(grid, x, y, 1, 0, 4);
        openFours += CountOpenPatterns(grid, x, y, 0, 1, 4);
        openFours += CountOpenPatterns(grid, x, y, 1, 1, 4);
        openFours += CountOpenPatterns(grid, x, y, 1, -1, 4);

        if (openThrees >= 2) // 33 ����
        {
            score += 7;
        }
        if (openFours >= 2) // 44 ����
        {
            score += 8;
        }
        if (openThrees >= 1 && openFours >= 1) // 34 ����
        {
            score += 9;
        }

        return score;
    }

    private int CountOpenPatterns(Grid grid, int x, int y, int dx, int dy, int length)
    {
        int count = 0;

        int consecutiveStones = CountStonesInDirection(grid, x, y, dx, dy, aiStone);
        int reverseStones = CountStonesInDirection(grid, x, y, -dx, -dy, aiStone);

        if (consecutiveStones + reverseStones + 1 == length)
        {
            bool isOpen = IsOpenEnd(grid, x + dx * consecutiveStones, y + dy * consecutiveStones, dx, dy) &&
                          IsOpenEnd(grid, x - dx * reverseStones, y - dy * reverseStones, -dx, -dy);

            if (isOpen)
            {
                count++;
            }
        }

        return count;
    }

    private bool IsOpenEnd(Grid grid, int x, int y, int dx, int dy)
    {
        int nx = x + dx;
        int ny = y + dy;

        return grid.IsPositionValid(nx, ny) && grid.GetStoneAt(nx, ny) == StoneType.None;
    }

    private int CountStonesInDirection(Grid grid, int x, int y, int dx, int dy, StoneType stone)
    {
        int count = 0;
        int nx = x + dx;
        int ny = y + dy;

        while (grid.IsPositionValid(nx, ny) && grid.GetStoneAt(nx, ny) == stone)
        {
            count++;
            nx += dx;
            ny += dy;
        }

        return count;
    }
}
