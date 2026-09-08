using UnityEngine;

namespace ThrownTogether
{
    public abstract class ContentDefinition : ScriptableObject
    {
        [Tooltip("Stable content key. Keep unchanged when renaming an asset.")]
        public string id;
        public string displayName;
        [TextArea] public string description;
    }
}
