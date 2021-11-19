using UnityEngine;
using controller;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;
using System.IO;
using option;

namespace ui {
    public class UI : MonoBehaviour {
        public Controller gameController;
        public RawImage successfulSaving;
        public Button saveBut;
        public Text unsuccessfulSaving;

        public GameObject loadTemplate;
        public GameObject boardImage10x10;
        public GameObject boardImage8x8;
        public RawImage whiteCheckerImage;
        public RawImage blackCheckerImage;
        public GameObject emptyCell;
        public RectTransform saveTemplatesStorage;

        private void Start() {
            gameController.successfulSaving += SuccessfulSaving;
            gameController.unsuccessfulSaving += UnsuccessfulSaving;

            saveBut.onClick.AddListener(() => gameController.Save(
                    Path.Combine(
                        Application.persistentDataPath,
                        Guid.NewGuid().ToString() + ".save"
                    )
                )
            );
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
}