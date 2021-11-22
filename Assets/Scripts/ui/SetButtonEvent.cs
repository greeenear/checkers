using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SetButtonEvent : MonoBehaviour {
    public Button button;
    public void SetLisener(UnityAction onClickEvent) {
        button.onClick.AddListener(onClickEvent);
    }
}
