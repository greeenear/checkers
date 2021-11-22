using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class FillText : MonoBehaviour {
        public Text text;

        public void SetText(string inputText) {
            text.text = inputText;
        }
    }
}
