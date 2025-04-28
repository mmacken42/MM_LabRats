using UnityEngine;

public class MainMenuRat : MonoBehaviour
{
    Vector3 resetPosition = new Vector3 (-90, -20, 0);
    void Update()
    {
        if (transform.position.x < 62 && transform.position.x > -91)
        {
            transform.position += -1 * 10f * Time.deltaTime * transform.right;
        }
        else
        {
            transform.SetPositionAndRotation(resetPosition, Quaternion.Euler(-25, 180, 0));
        }
    }
}
