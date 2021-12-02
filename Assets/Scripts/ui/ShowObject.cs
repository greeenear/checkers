using UnityEngine;

namespace ui {
    public class ShowObject : MonoBehaviour {
        public float duration;
        private float time;

        private void OnEnable() {
            time = 0;
        }

        private void Update() {
            time += Time.deltaTime;
            if (time >= duration) {
                gameObject.SetActive(false);
            }
        }
    }
}