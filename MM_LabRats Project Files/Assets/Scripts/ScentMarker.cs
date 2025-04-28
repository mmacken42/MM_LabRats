using UnityEngine;

public class ScentMarker : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            transform.gameObject.SetActive(false);
        }
    }
}
