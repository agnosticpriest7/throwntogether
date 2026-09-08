using UnityEngine;

namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Appliance")]
    public sealed class ApplianceDefinition : ContentDefinition
    {
        public ProcessingRecipe[] supportedProcesses = new ProcessingRecipe[0];
        public bool Supports(ProcessingRecipe process) => process != null && System.Array.IndexOf(supportedProcesses,process)>=0;
    }
}
