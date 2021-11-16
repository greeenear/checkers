using UnityEngine;
using controller;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;

public class UI : MonoBehaviour {
    public RawImage successfulSaving;
    public Text unsuccessfulSaving;

    private void Start() {
        Controller.successfulSaving += SuccessfulSaving;
        Controller.unsuccessfulSaving += UnsuccessfulSaving;
    }
    private async void SuccessfulSaving() {
        successfulSaving.gameObject.SetActive(true);
        await Timer(2);
        successfulSaving.gameObject.SetActive(false);
    }

    private async void UnsuccessfulSaving() {
        unsuccessfulSaving.gameObject.SetActive(true);
        await Timer(2);
        unsuccessfulSaving.gameObject.SetActive(false);
    }

    private async Task Timer(int waitingTime) {
        await Task.Delay(TimeSpan.FromSeconds(waitingTime));
    }
}