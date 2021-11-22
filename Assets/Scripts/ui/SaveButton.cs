using UnityEngine;
using UnityEngine.UI;
using controller;
using UnityEngine.Events;

namespace ui {
    public class SaveButton : MonoBehaviour {
        public Button saveBut;
        public Controller controller;

        private void Awake() {
            saveBut.onClick.AddListener(() => controller.Save(controller.GenerateSavePath()));
        }
    }
}
