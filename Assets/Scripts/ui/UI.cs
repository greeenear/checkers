using UnityEngine;
using controller;
using UnityEngine.UI;

namespace ui {
    public class UI : MonoBehaviour {
        public Controller controller;
        public ShowImage successfulSaving;
        public ChangeActive changeActiveMainMenu;
        public ChangeActive changeActiveBackMainMenu;
        public Button saveBut;

        private void Start() {
            saveBut.onClick.AddListener(() => controller.Save(controller.GenerateSavePath()));
            controller.successfulSaving += successfulSaving.SuccessfulSaving;
            controller.gameOver += changeActiveMainMenu.ChangeActiveObject;
            controller.gameOver += changeActiveBackMainMenu.ChangeActiveObject;
        }
    }
}