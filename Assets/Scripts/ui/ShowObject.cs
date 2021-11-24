using UnityEngine;

namespace ui {
    public class ShowObject : MonoBehaviour {
        public float duration;
        private bool isActiveTimer;
        private float startPoint = 0;

        public void Show() {
            gameObject.SetActive(true);
            isActiveTimer = true;
        }

        private void Update() {
            if (isActiveTimer) {
                startPoint += Time.deltaTime;
                if (startPoint >= duration) {
                    gameObject.SetActive(false);
                    isActiveTimer = false;
                    startPoint = 0;
                }
            }
        }
    }
}