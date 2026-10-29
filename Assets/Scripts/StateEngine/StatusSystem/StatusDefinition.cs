using UnityEngine;

namespace ANTK.Game.StateEngine.StatusSystem
{
    public enum StatusType
    {
        Buff,
        Debuff
    }

    [CreateAssetMenu(
        fileName = "StatusDefinition",
        menuName = "State Engine/Status Definition")]
    public class StatusDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private StatusType type;

        public string Id => id;
        public string Name => displayName;
        public StatusType Type => type;
    }
}
