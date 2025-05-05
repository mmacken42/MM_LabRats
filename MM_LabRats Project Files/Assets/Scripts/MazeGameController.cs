using System;
using UnityEngine;
using UnityEngine.UI;

public class MazeGameController : MonoBehaviour
{
    //Maze generator helper
    private MazeGenerator mazeGenerator;

    //Timer
    private float levelTimer;
    private float totalTimeSeconds;
    public Text timerText;
    private bool playerSolvingMaze = false;

    //Level dimensions (levels 1 -> 5)
    public Vector2Int level1Size = new(6, 6);
    public Vector2Int level2Size = new(12, 12);
    public Vector2Int level3Size = new(18, 18);
    public Vector2Int level4Size = new(24, 24);
    public Vector2Int level5Size = new(30, 30);
    //keep track of current level
    private int currentLevel;

    //Scoring
    public GameObject scoreUI;
    public Text textTimerScore;
    public Text textSniffScore;
    public Text textPoopScore;
    public Text textFinalScore;
    private int numSniffs;
    private int numPoops;

    //Player Abilities
    public PlayerAbilityController sniffAbilityController;
    public PlayerAbilityController poopAbilityController;

    private void Start()
    {
        mazeGenerator = GetComponent<MazeGenerator>();

        StartNewGame();
    }

    private void Update()
    {
        //when player is solving maze, increment level timer + text
        if (playerSolvingMaze)
        {
            levelTimer += Time.deltaTime;

            int minutes = Mathf.FloorToInt(levelTimer / 60);
            int seconds = Mathf.RoundToInt(levelTimer % 60);

            string minutesString = minutes.ToString();
            if (minutes < 10)
            {
                minutesString = "0" + minutes.ToString();
            }

            string secondsString = seconds.ToString();
            if (seconds < 10)
            {
                secondsString = "0" + seconds.ToString();
            }

            timerText.text = minutesString + ":" + secondsString;
        }
    }

    public void StartNewGame()
    {
        scoreUI.SetActive(false);
        currentLevel = 1;

        ResetLevelTimer();

        totalTimeSeconds = 0;
        numSniffs = 0;
        numPoops = 0;

        mazeGenerator.SetNewMazeCreationDimensions(level1Size.x, level1Size.y);
        mazeGenerator.SetNewDelayBtwMazeCreationOperations(0.025f);

        mazeGenerator.GenerateNewMaze();
    }

    private void ResetLevelTimer()
    {
        levelTimer = 0f;
        timerText.text = "";
    }

    public void PlayerSniffingOutSolution()
    {
        numSniffs++;

        mazeGenerator.TempRevealMazeSolution(mazeGenerator.GetPlayerCurrentCell(), mazeGenerator.GetEndOfMaze());

        sniffAbilityController.StartAbilityCooldown();
    }

    public void PlayerDroppingPoopMarker()
    {
        //TODO better

        numPoops++;

        poopAbilityController.StartAbilityCooldown();
    }

    public void PlayerSolvedTheMaze()
    {
        playerSolvingMaze = false;
        totalTimeSeconds += levelTimer;
        ResetLevelTimer();
        sniffAbilityController.ResetAbility();
        poopAbilityController.ResetAbility();

        switch (currentLevel)
        {
            case 1:
                currentLevel = 2;
                mazeGenerator.SetNewMazeCreationDimensions(level2Size.x, level2Size.y);
                mazeGenerator.SetNewDelayBtwMazeCreationOperations(0.020f);
                mazeGenerator.GenerateNewMaze();
                break;
            case 2:
                currentLevel = 3;
                mazeGenerator.SetNewMazeCreationDimensions(level3Size.x, level3Size.y);
                mazeGenerator.SetNewDelayBtwMazeCreationOperations(0.015f);
                mazeGenerator.GenerateNewMaze();
                break;
            case 3:
                currentLevel = 4;
                mazeGenerator.SetNewMazeCreationDimensions(level4Size.x, level4Size.y);
                mazeGenerator.SetNewDelayBtwMazeCreationOperations(0.010f);
                mazeGenerator.GenerateNewMaze();
                break;
            case 4:
                currentLevel = 5;
                mazeGenerator.SetNewMazeCreationDimensions(level5Size.x, level5Size.y);
                mazeGenerator.SetNewDelayBtwMazeCreationOperations(0.005f);
                mazeGenerator.GenerateNewMaze();
                break;
            case 5:
                HandleEndOfGame();
                break;
        }
    }

    public void SetPlayerSolvingMaze(bool newSolvingMazeVal)
    {
        playerSolvingMaze = newSolvingMazeVal;
    }

    private void HandleEndOfGame()
    {
        mazeGenerator.DestroyOldMaze();
        mazeGenerator.DestroyOldPlayer();
        mazeGenerator.TurnOnOverviewCamera();

        int timeScore = 10000 - Mathf.FloorToInt(totalTimeSeconds);
        int sniffScore = -10 * numSniffs;
        int poopScore = -5 * numPoops;
        int finalScore = timeScore + sniffScore + poopScore;

        textTimerScore.text = timeScore.ToString();
        textSniffScore.text = sniffScore.ToString();
        textPoopScore.text = poopScore.ToString();
        textFinalScore.text = finalScore.ToString();

        scoreUI.SetActive(true);
    }
}
