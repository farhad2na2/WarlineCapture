using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PilotoStudio
{
    public class ParticleShowcase : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> particles = new List<GameObject>();
        [SerializeField]
        private int currentlyActive = 0;
        [SerializeField]
        private Text displayName;

        private void Start()
        {
            foreach (Transform t in this.transform)
            {
                particles.Add(t.gameObject);
            }
            PostUpdateLogic();
            particles[currentlyActive].SetActive(true);
        }

        void PostUpdateLogic()
        {
            if (displayName != null)
            displayName.text = particles[currentlyActive].name;
            if (particles[currentlyActive].TryGetComponent<ParticleHandler>(out ParticleHandler handler))
            {
                handler.Cast();
            }
        }


        public void ActivateNext()
        {
            if (currentlyActive + 1 >= particles.Count)
            {
                particles[currentlyActive].SetActive(false);
                currentlyActive = 0;
                particles[currentlyActive].SetActive(true);
            }
            else
            {
                particles[currentlyActive].SetActive(false);
                currentlyActive++;
                particles[currentlyActive].SetActive(true);
            }

            PostUpdateLogic();
        }

        public void ActivatePrevious()
        {
            if (currentlyActive - 1 < 0)
            {
                particles[currentlyActive].SetActive(false);
                currentlyActive = (particles.Count - 1);
                particles[currentlyActive].SetActive(true);
            }
            else
            {
                particles[currentlyActive].SetActive(false);
                currentlyActive--;
                particles[currentlyActive].SetActive(true);
            }

            PostUpdateLogic();
        }


        private void Update()
        {
            if (GetKeyDown(KeyCode.Q))
            {
                ActivatePrevious();
            }

            if (GetKeyDown(KeyCode.W))
            {
                ActivateNext();
            }

            if (GetKeyDown(KeyCode.Space))
            {
          
                if (particles[currentlyActive].TryGetComponent<ParticleSystem>(out ParticleSystem ps))
                {
                    ps.Play();
                }
                PostUpdateLogic() ;
            }
        }

        private static bool GetKeyDown(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            return keyCode switch
            {
                KeyCode.Q => keyboard.qKey.wasPressedThisFrame,
                KeyCode.W => keyboard.wKey.wasPressedThisFrame,
                KeyCode.Space => keyboard.spaceKey.wasPressedThisFrame,
                _ => false
            };
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(keyCode);
#else
            return false;
#endif
        }

    }
}
