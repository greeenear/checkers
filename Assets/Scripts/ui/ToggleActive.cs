using UnityEngine;

namespace ui {
    public class ToggleActive : MonoBehaviour {
        public void ChangeActiveObject() {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}
