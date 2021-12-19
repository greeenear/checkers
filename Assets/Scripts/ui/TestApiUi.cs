using UnityEngine;
using UnityEngine.UI;
using checkers;

namespace ui {
    public class TestApiUi : MonoBehaviour {
        public TestApi test;
        public InputField input;
        public Button moveBut;

        private void Awake() {
            if (input == null) {
                Debug.LogError("NoInputRef");
                return;
            }

            if (moveBut == null) {
                Debug.LogError("NoMoveButRef");
                return;
            }

            moveBut.onClick.AddListener(
                () => {
                    if (test.SetPos(input.text.ToString())) {
                        test.CheckInputPoint();
                    }
                    input.text = "";
                }
            );
        }
    }
}
