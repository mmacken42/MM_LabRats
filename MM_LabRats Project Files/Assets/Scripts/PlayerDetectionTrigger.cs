using UnityEngine;

public class PlayerDetectionTrigger : MonoBehaviour
{
    private bool playerOnThisCell;

    public bool IsPlayerStandingOnThisCell()
    {
        return playerOnThisCell;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnThisCell = true;
        }
        else
        {
            playerOnThisCell = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnThisCell = false;
        }
    }
}
