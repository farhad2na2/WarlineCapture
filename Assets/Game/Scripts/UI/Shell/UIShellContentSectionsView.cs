using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    public enum UIShellContentSectionId
    {
        MenuBackground = 0,
        Header = 1,
        Left = 2,
        Middle = 3,
        Right = 4,
        Footer = 5
    }

    [DisallowMultipleComponent]
    public sealed class UIShellContentSectionsView : MonoBehaviour
    {
        [Serializable]
        public sealed class SectionReference
        {
            [SerializeField] private UIShellContentSectionId sectionId;
            [SerializeField] private GameObject sectionRoot;

            public SectionReference(UIShellContentSectionId sectionId, GameObject sectionRoot)
            {
                this.sectionId = sectionId;
                this.sectionRoot = sectionRoot;
            }

            public UIShellContentSectionId SectionId => sectionId;
            public GameObject SectionRoot => sectionRoot;
        }

        [SerializeField] private SectionReference[] sections;

        public IReadOnlyList<SectionReference> Sections => sections;

        public bool TryGetSection(UIShellContentSectionId sectionId, out GameObject sectionRoot)
        {
            if (sections != null)
            {
                for (int i = 0; i < sections.Length; i++)
                {
                    SectionReference section = sections[i];
                    if (section != null && section.SectionId == sectionId && section.SectionRoot != null)
                    {
                        sectionRoot = section.SectionRoot;
                        return true;
                    }
                }
            }

            sectionRoot = null;
            return false;
        }

        public void ConfigureSections(SectionReference[] sectionReferences)
        {
            sections = sectionReferences;
        }
    }
}
