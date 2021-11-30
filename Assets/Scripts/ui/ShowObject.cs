using UnityEngine;

namespace ui {
    public class ShowObject : MonoBehaviour {
        public float duration;
        private float time;

        public void Show() {
            gameObject.SetActive(true);
            this.enabled = true;
            time = 0;
        }

        private void Update() {
            time += Time.deltaTime;
            if (time >= duration) {
                this.enabled = false;
                gameObject.SetActive(false);
                time = 0;
            }
        }
    }
}