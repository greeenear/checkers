using UnityEngine;

namespace ui {
    public class ChangeActive : MonoBehaviour {
        public void ChangeActiveObject() {
            Debug.Log(gameObject.name);
            Debug.Log(gameObject.activeSelf);
            gameObject.name = "asd";
            gameObject.SetActive(!gameObject.activeSelf);
            Debug.Log(gameObject.activeSelf);
        }
    }
}
