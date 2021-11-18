using UnityEngine;

namespace ui {
    public class ChangeActive : MonoBehaviour {
        public void ChangeActiveObject() {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}
