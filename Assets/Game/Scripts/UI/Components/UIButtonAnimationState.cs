using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Animator))]
    public sealed class UIButtonAnimationState : MonoBehaviour
    {
        [SerializeField] private string initialStateName = "Normal";
        [SerializeField] private bool selectWithEventSystem;

        private Animator _animator;
        private Button _button;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            ApplyInitialState();
        }

        public void ApplyInitialState()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();

            if (_button == null)
                _button = GetComponent<Button>();

            if (_animator != null && _animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(initialStateName))
            {
                _animator.Play(initialStateName, 0, 1f);
                _animator.Update(0f);
            }

            if (selectWithEventSystem && _button != null)
                _button.Select();
        }
    }
}
