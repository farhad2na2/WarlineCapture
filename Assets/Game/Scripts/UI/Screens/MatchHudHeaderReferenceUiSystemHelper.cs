using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed class MatchHudHeaderReferenceUiSystemHelper
    {
        private const string HeaderContentName = "HeaderContent";
        private const string ResourceStripName = "ResourceStrip";
        private const string ThreatJumpPanelName = "ThreatJumpPanel";
        private const string ThreatTitleName = "Title";
        private const string CreditsSlotName = "CreditsSlot";
        private const string OilSlotName = "OilSlot";
        private const string FuelSlotName = "FuelSlot";
        private const string SupplySlotName = "SupplySlot";
        private const string CivilianRiskSlotName = "CivilianRiskSlot";

        public Transform ResourceStrip { get; private set; }
        public Transform ThreatJumpPanel { get; private set; }
        public TMP_Text ThreatTitle { get; private set; }
        public ResourceSlotReference CreditsSlot { get; private set; }
        public ResourceSlotReference OilSlot { get; private set; }
        public ResourceSlotReference FuelSlot { get; private set; }
        public ResourceSlotReference SupplySlot { get; private set; }
        public ResourceSlotReference CivilianRiskSlot { get; private set; }

        public static MatchHudHeaderReferenceUiSystemHelper Create(Transform contentRoot)
        {
            var references = new MatchHudHeaderReferenceUiSystemHelper();
            Transform nestedHeader = references.CacheDirectHeaderChildren(contentRoot);
            if (nestedHeader != null &&
                (references.ResourceStrip == null || references.ThreatJumpPanel == null))
            {
                references.CacheDirectHeaderChildren(nestedHeader);
            }

            return references;
        }

        public void CacheOilSlot(Transform oilSlot)
        {
            OilSlot = oilSlot != null ? new ResourceSlotReference(oilSlot) : null;
        }

        private Transform CacheDirectHeaderChildren(Transform headerRoot)
        {
            if (headerRoot == null)
                return null;

            Transform nestedHeader = null;
            for (int i = 0; i < headerRoot.childCount; i++)
            {
                Transform child = headerRoot.GetChild(i);
                switch (child.name)
                {
                    case HeaderContentName when nestedHeader == null:
                        nestedHeader = child;
                        break;
                    case ResourceStripName when ResourceStrip == null:
                        CacheResourceStrip(child);
                        break;
                    case ThreatJumpPanelName when ThreatJumpPanel == null:
                        CacheThreatJumpPanel(child);
                        break;
                }
            }

            return nestedHeader;
        }

        private void CacheResourceStrip(Transform resourceStrip)
        {
            ResourceStrip = resourceStrip;
            for (int i = 0; i < resourceStrip.childCount; i++)
            {
                Transform child = resourceStrip.GetChild(i);
                switch (child.name)
                {
                    case CreditsSlotName when CreditsSlot == null:
                        CreditsSlot = new ResourceSlotReference(child);
                        break;
                    case OilSlotName when OilSlot == null:
                        CacheOilSlot(child);
                        break;
                    case FuelSlotName when FuelSlot == null:
                        FuelSlot = new ResourceSlotReference(child);
                        break;
                    case SupplySlotName when SupplySlot == null:
                        SupplySlot = new ResourceSlotReference(child);
                        break;
                    case CivilianRiskSlotName when CivilianRiskSlot == null:
                        CivilianRiskSlot = new ResourceSlotReference(child);
                        break;
                }
            }
        }

        private void CacheThreatJumpPanel(Transform panel)
        {
            ThreatJumpPanel = panel;
            for (int i = 0; i < panel.childCount; i++)
            {
                Transform child = panel.GetChild(i);
                if (child.name != ThreatTitleName)
                    continue;

                ThreatTitle = child.GetComponent<TMP_Text>();
                return;
            }

            ThreatTitle = panel.GetComponentInChildren<TMP_Text>(true);
        }

        internal sealed class ResourceSlotReference
        {
            private const string LabelName = "Label";
            private const string ValueName = "Value";

            public ResourceSlotReference(Transform root)
            {
                Root = root;
                bool labelResolved = false;
                bool valueResolved = false;
                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    if (!labelResolved && child.name == LabelName)
                    {
                        labelResolved = true;
                        Label = child.GetComponent<TMP_Text>();
                    }
                    else if (!valueResolved && child.name == ValueName)
                    {
                        valueResolved = true;
                        Value = child.GetComponent<TMP_Text>();
                    }

                    if (labelResolved && valueResolved)
                        break;
                }
            }

            public Transform Root { get; }
            public TMP_Text Label { get; }
            public TMP_Text Value { get; }
        }
    }
}
