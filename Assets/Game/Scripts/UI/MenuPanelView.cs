using System;
using UnityEngine;

namespace Game.Scripts.UI
{
    public class MenuPanelView : MonoBehaviour
    {
        public MenuView menuView;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void HideComplete()
        {
            menuView.HideComplete(animator);
        }
    }
}