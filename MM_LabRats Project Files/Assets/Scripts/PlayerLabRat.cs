using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerLabRat : MonoBehaviour
{
    //body
    public GameObject playerBody;

    //Player move speeds
    private float playerMoveSpeedForward = 1.5f;
    private float playerMoveSpeedBackward = 0.5f;
    private float playerRotationSpeed = 150f;

    //Player Camera ref
    public Camera playerCamera;

    //Poop
    public GameObject prefabPoop;
    public GameObject poopSpawnPoint;

    //Maze controller ref
    private MazeGameController gameController;

    private bool sniffingOutSolution;
    private float secondsToPauseWhileSniffingSolution = 1f;
    private bool isPooping;
    private float secondsToPauseWhilePooping = 1.5f;
    private List<GameObject> poops;

    private void Start()
    {
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<MazeGameController>();

        sniffingOutSolution = false;
        isPooping = false;
        poops = new List<GameObject>();

        TogglePlayerCamera(true);
    }

    private void Update()
    {
        if (!sniffingOutSolution && !isPooping && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(SniffOutSolution());
        }
        
        if (!sniffingOutSolution && !isPooping && Input.GetKeyDown(KeyCode.Q))
        {
            StartCoroutine(DropPoopMarker());
        }

        //only allowed forward/backward movement when not sniffing solution
        if (!sniffingOutSolution && !isPooping)
        {
            if (Input.GetKey(KeyCode.W)) //FORWARD
            {
                MoveForward();
            }
            else if (Input.GetKey(KeyCode.S)) //BACKWARD
            {
                MoveBackward();
            }
        }

        if (Input.GetKey(KeyCode.A)) //TURN LEFT
        {
            TurnLeft();
        }
        else if (Input.GetKey(KeyCode.D)) //TURN RIGHT
        {
            TurnRight();
        }
    }

    private void MoveForward()
    {
        transform.position += playerMoveSpeedForward * Time.deltaTime * transform.forward;
    }

    private void MoveBackward()
    {
        transform.position += -1f * playerMoveSpeedBackward * Time.deltaTime * transform.forward;
    }

    private void TurnLeft()
    {
        transform.Rotate(-1f * Vector3.up * playerRotationSpeed * Time.deltaTime);
    }

    private void TurnRight()
    {
        transform.Rotate(Vector3.up * playerRotationSpeed * Time.deltaTime);
    }

    private IEnumerator SniffOutSolution()
    {
        if (gameController.sniffAbilityController.CanUseAbility())
        {
            sniffingOutSolution = true;
            playerBody.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(65, 0, 0));
            gameController.PlayerSniffingOutSolution();

            yield return new WaitForSeconds(secondsToPauseWhileSniffingSolution);

            playerBody.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(90, 0, 0));
            sniffingOutSolution = false;
        }
    }

    private void SniffUpPowerup()
    {
        //TODO
    }

    private IEnumerator DropPoopMarker()
    {
        if (gameController.poopAbilityController.CanUseAbility())
        {
            gameController.PlayerDroppingPoopMarker();

            isPooping = true;

            playerBody.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(105, 0, 0));

            yield return new WaitForSeconds(secondsToPauseWhilePooping / 2);

            Vector3 spawnPos = new Vector3(poopSpawnPoint.transform.position.x, 0.05f, poopSpawnPoint.transform.position.z);

            GameObject newPoop = Instantiate(prefabPoop, spawnPos, Quaternion.identity);
            poops.Add(newPoop);

            yield return new WaitForSeconds(secondsToPauseWhilePooping / 2);

            playerBody.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(90, 0, 0));

            isPooping = false;
        }
    }

    public void DeletePoops()
    {
        foreach (GameObject next in poops)
        {
            GameObject.Destroy(next);
        }

        poops.Clear();
    }

    public void TogglePlayerCamera(bool newEnabled)
    {
        playerCamera.enabled = newEnabled;
    }
}
