using UnityEngine;
using controller;
using UnityEngine.UI;

namespace ui {
    public class SetSaveAction : MonoBehaviour {
        public Button saveBut;
        public Controller controller;

        private void Awake() {
            saveBut.onClick.AddListener(() => {
                if (!controller.Save("")) {
                    Debug.Log("CantSave");
                }
            });
        }
    }
}

