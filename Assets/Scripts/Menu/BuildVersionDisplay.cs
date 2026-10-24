using TMPro;
using UnityEngine;

public class BuildVersionDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private string versionPrefix = "Build ";

    private void Awake()
    {
        if (versionText == null)
        {
            Debug.LogError($"{nameof(BuildVersionDisplay)} requires a TMP_Text reference.", this);
            enabled = false;
            return;
        }

        versionText.text = $"{versionPrefix}{Application.version}";
    }
}
