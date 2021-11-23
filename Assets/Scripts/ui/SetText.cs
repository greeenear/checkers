using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class SetText : MonoBehaviour {
        public Text text;

        public void WriteText(string inputText) {
            text.text = inputText;
        }
    }
}
