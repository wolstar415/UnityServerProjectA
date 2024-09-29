using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class GameManager : MonoBehaviour
{
    private Grid grid;
    private Player player1;
    private Player player2;
    private Player currentPlayer;

    public int gridSize = 19;
    public int currentMoveNumber { get; private set; }
    private bool isGamePaused = false;
    private bool isGameOver = false; // ���� ���� ���¸� ��Ÿ���� ����

    private List<MoveRecord> moveHistory; // �� ���� �������� ����ϴ� ����Ʈ
    public int MaxHisyory => moveHistory.Count; // �� ���� �������� ����ϴ� ����Ʈ

    public StoneType[,] BoardState => grid == null ? null : grid.GetBoard();

    void Start()
    {
        InitializeGame(false, true); // ��: player1�� �浹 AI, player2�� �鵹 �ΰ�
    }

    public void InitializeGame(bool isPlayer1AI, bool isPlayer2AI)
    {
        grid = new Grid(gridSize);
        currentMoveNumber = 1;
        isGameOver = false; // ������ �ʱ�ȭ�� �� ���� ���¸� false�� ����
        moveHistory = new List<MoveRecord>();

        player1 = isPlayer1AI ? new BlackPlayer(isAI: true) : new BlackPlayer(isAI: false);
        player2 = isPlayer2AI ? new Player(StoneType.White, isAI: true) : new Player(StoneType.White, isAI: false);

        currentPlayer = player1; // �浹���� ����
        if (currentPlayer.IsAIPlayer)
        {
            MakeAIMove(); // AI�� ù ���� ��� �ٷ� ����
        }
    }

    public bool OnBoardClick(int x, int y)
    {
        if (isGamePaused || isGameOver) return false; // ������ �Ͻ� ���� �Ǵ� ���� ���¿����� ���� ���� �� ����

        if (!currentPlayer.IsAIPlayer && currentPlayer.MakeMove(grid, x, y))
        {
            RecordMove(x, y, currentPlayer.Stone);
            if (currentPlayer.CheckForWin(grid, x, y))
            {
                isGameOver = true; // ������ �������� ����
                DisplayWinMessage(currentPlayer.Stone);
            }
            else
            {
                SwitchTurn();
            }
            return true;
        }
        return false;
    }

    private void SwitchTurn()
    {
        currentPlayer = currentPlayer == player1 ? player2 : player1;
        currentMoveNumber++;

        if (currentPlayer.IsAIPlayer)
        {
            MakeAIMove(); // AI�� ���̸� ���� �ΰ� ��
        }
    }

    private void MakeAIMove()
    {
        if (isGameOver) return; // ������ ����� ��� AI�� �� �̻� ���� ���� ����

        Vector2Int aiMove = currentPlayer.MakeAIMove(grid, player1.Stone == currentPlayer.Stone ? player2.Stone : player1.Stone);

        if (aiMove != new Vector2Int(-1, -1))
        {
            currentPlayer.MakeMove(grid, aiMove.x, aiMove.y);
            RecordMove(aiMove.x, aiMove.y, currentPlayer.Stone);

            if (currentPlayer.CheckForWin(grid, aiMove.x, aiMove.y))
            {
                isGameOver = true; // ������ �������� ����
                DisplayWinMessage(currentPlayer.Stone);
                return;
            }
        }

        SwitchTurn(); // AI�� ���� �ξ����� ���� �ѱ�
    }

    private void DisplayWinMessage(StoneType winningStone)
    {
        string message = $"{winningStone} wins the game!";
        Debug.Log(message);

        // ���� ���� �� ���â ǥ��
        EditorUtility.DisplayDialog("Game Over", message, "OK");

        // �߰����� ���� ���� ó�� ���� (��: ������ �����ϰų� �������� ����� ��)
    }

    public void ResetGame()
    {
        InitializeGame(player1.IsAIPlayer, player2.IsAIPlayer); // ������ �ʱ�ȭ�մϴ�.
    }

    public StoneType GetStoneAt(int x, int y)
    {
        return grid?.GetStoneAt(x, y) ?? StoneType.None;
    }

    public StoneType GetCurrentPlayerStone()
    {
        return currentPlayer.Stone;
    }

    public bool IsForbiddenMove(int x, int y)
    {
        return currentPlayer.IsForbiddenMove(grid, x, y);
    }

    private void RecordMove(int x, int y, StoneType stone)
    {
        moveHistory.Add(new MoveRecord(x, y, stone));
    }

    public void LoadState(int moveNumber)
    {
        grid.ResetBoard();
        moveNumber -= 1;
        for (int i = 0; i <= moveNumber; i++)
        {
            var move = moveHistory[i];
            grid.SetStone(move.X, move.Y, move.Stone);
        }
        currentMoveNumber = moveNumber + 1;
    }

    public void SetPause(bool pause)
    {
        isGamePaused = pause;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    public bool IsGamePaused => isGamePaused;

    private struct MoveRecord
    {
        public int X;
        public int Y;
        public StoneType Stone;

        public MoveRecord(int x, int y, StoneType stone)
        {
            X = x;
            Y = y;
            Stone = stone;
        }
    }
}
