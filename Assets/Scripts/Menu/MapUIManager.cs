using UnityEngine;
using UnityEngine.UI;

public class MapUIManager : MonoBehaviour
{
    [SerializeField] private Button startFightGameButton;
    [SerializeField] private Button surrenderRunButton;

    void Start()
    {
        startFightGameButton.onClick.AddListener(OnStartFightGameButtonClicked);
        surrenderRunButton.onClick.AddListener(OnSurrenderRunButtonClicked);
    }

    private void OnStartFightGameButtonClicked()
    {
        GameManager.Instance.runManager.StartFight();
    }

    private void OnSurrenderRunButtonClicked()
    {
        GameManager.Instance.EndRun();
    }
}
