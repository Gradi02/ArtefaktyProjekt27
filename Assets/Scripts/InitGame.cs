using UnityEngine;

public class InitGame : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance does not exist", this);
            Destroy(this.gameObject);
        }

        GameManager.Instance.StartGameInitialization();
    }
}
