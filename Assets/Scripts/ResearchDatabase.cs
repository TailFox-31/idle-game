using System.Collections.Generic;
using UnityEngine;

namespace IdleGame
{
    [CreateAssetMenu(fileName = "ResearchDatabase", menuName = "Idle Game/Research Database")]
    public sealed class ResearchDatabase : ScriptableObject
    {
        [SerializeField]
        private List<ResearchDefinition> definitions = new();

        public IReadOnlyList<ResearchDefinition> Definitions => definitions ?? new List<ResearchDefinition>();
    }
}
