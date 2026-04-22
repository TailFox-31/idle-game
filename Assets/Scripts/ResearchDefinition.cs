using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame
{
    [CreateAssetMenu(fileName = "ResearchDefinition", menuName = "Idle Game/Research Definition")]
    public sealed class ResearchDefinition : ScriptableObject
    {
        private static readonly string[] EmptyPrerequisiteIds = Array.Empty<string>();

        [SerializeField]
        private string researchId = string.Empty;

        [SerializeField]
        private ResearchAxis axis = ResearchAxis.Sustain;

        [SerializeField]
        private string displayName = "Research";

        [SerializeField, TextArea(2, 4)]
        private string description = string.Empty;

        [SerializeField, Min(1)]
        private int maxLevel = 1;

        [SerializeField, Min(1)]
        private int costPerLevel = 1;

        [SerializeField]
        private List<string> prerequisiteIds = new();

        [SerializeField]
        private ResearchEffectType effectType = ResearchEffectType.None;

        [SerializeField]
        private float effectValueA;

        [SerializeField]
        private float effectValueB;

        public string ResearchId => string.IsNullOrWhiteSpace(researchId) ? string.Empty : researchId.Trim();

        public ResearchAxis Axis => axis;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();

        public string Description => string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();

        public int MaxLevel => Mathf.Max(1, maxLevel);

        public int CostPerLevel => Mathf.Max(1, costPerLevel);

        public IReadOnlyList<string> PrerequisiteIds => prerequisiteIds != null ? prerequisiteIds : EmptyPrerequisiteIds;

        public ResearchEffectType EffectType => effectType;

        public float EffectValueA => effectValueA;

        public float EffectValueB => effectValueB;

        public bool IsValid => !string.IsNullOrWhiteSpace(ResearchId);
    }
}
