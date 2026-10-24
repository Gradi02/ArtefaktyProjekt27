using UnityEngine;
using UnityEngine.UI;

public sealed class MapUIManager : MonoBehaviour
{
    [SerializeField] private Button startFightGameButton;
    [SerializeField] private Button surrenderRunButton;

    void Start()
    {
        if (startFightGameButton == null || surrenderRunButton == null)
        {
            Debug.LogError("Map UI buttons are not assigned.", this);
            return;
        }

        startFightGameButton.onClick.AddListener(OnStartFightGameButtonClicked);
        surrenderRunButton.onClick.AddListener(OnSurrenderRunButtonClicked);
    }

    private void OnDestroy()
    {
        if (startFightGameButton != null)
        {
            startFightGameButton.onClick.RemoveListener(OnStartFightGameButtonClicked);
        }
        if (surrenderRunButton != null)
        {
            surrenderRunButton.onClick.RemoveListener(OnSurrenderRunButtonClicked);
        }
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
