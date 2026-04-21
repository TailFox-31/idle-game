using UnityEngine;

namespace IdleGame
{
    public readonly struct CombatRuntimeStatus
    {
        public CombatRuntimeStatus(
            string statusId,
            string displayName,
            float remainingDuration,
            int stackCount,
            bool isBeneficial,
            bool isPersistent,
            string statusText = "")
        {
            StatusId = string.IsNullOrWhiteSpace(statusId) ? string.Empty : statusId.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
            RemainingDuration = Mathf.Max(0f, remainingDuration);
            StackCount = Mathf.Max(0, stackCount);
            IsBeneficial = isBeneficial;
            IsPersistent = isPersistent;
            StatusText = string.IsNullOrWhiteSpace(statusText) ? string.Empty : statusText.Trim();
        }

        public string StatusId { get; }

        public string DisplayName { get; }

        public float RemainingDuration { get; }

        public int StackCount { get; }

        public bool IsBeneficial { get; }

        public bool IsPersistent { get; }

        public string StatusText { get; }
    }
}
