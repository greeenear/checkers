using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using controller;

public class SetButtonEvent : MonoBehaviour {
    public Button button;
    public Controller controller;

    private void Awake() {
        if (controller != null) {
            controller.onStartGame += SetLisener;
        }
    }

    public void SetLisener(UnityAction onClickEvent) {
        button.onClick.AddListener(onClickEvent);
    }
}
