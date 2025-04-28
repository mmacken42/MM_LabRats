using UnityEngine;

public class Cheese : MonoBehaviour
{
    public bool cheeseFound;

    private MazeGameController gameController;

    private void Start()
    {
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<MazeGameController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            cheeseFound = true;

            gameController.PlayerSolvedTheMaze();
        }
    }
}
