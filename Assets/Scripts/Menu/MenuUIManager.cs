using UnityEngine;
using UnityEngine.UI;

public class MenuUIManager : MonoBehaviour
{
    [SerializeField] private Button startGameButton;

    void Start()
    {
        startGameButton.onClick.AddListener(OnStartGameButtonClicked);
    }

    private void OnStartGameButtonClicked()
    {
        GameManager.Instance.StartRun();
    }
}
