using UnityEngine;
using controller;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;
using System.IO;

namespace ui {
    public class UI : MonoBehaviour {
        public Controller gameController;
        public RawImage successfulSaving;
        public Button saveBut;
        public Text unsuccessfulSaving;
        public GameObject mainMenu;
        public GameObject loadMenu;

        public GameObject loadTemplate;
        public GameObject boardImage10x10;
        public GameObject boardImage8x8;
        public RawImage whiteCheckerImage;
        public RawImage blackCheckerImage;
        public GameObject emptyCell;
        public GameObject saveTemplatesStorage;
        public RectTransform saveTemplatesStorage2;

        private void Start() {
            gameController.successfulSaving += SuccessfulSaving;
            gameController.unsuccessfulSaving += UnsuccessfulSaving;
            gameController.changeActiveMainMenu += ChangeActiveMainMenu;
            gameController.changeActiveLoadMenu += ChangeActiveLoadMenu;

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

        private void ChangeActiveMainMenu() {
            mainMenu.SetActive(!mainMenu.activeSelf);
        }

        public void ChangeActiveLoadMenu() {
            loadMenu.SetActive(!loadMenu.activeSelf);
        }

        public void FillLoadMenu() {
            foreach (Transform child in saveTemplatesStorage2.transform) {
                Destroy(child.gameObject);
            }
            var saves = gameController.GetSavesInfo();
            saveTemplatesStorage2.sizeDelta = new Vector2(0, 130 * saves.Count);

            foreach (var save in saves) {
                var curObj = Instantiate(
                    loadTemplate,
                    Vector3.zero,
                    Quaternion.identity,
                    saveTemplatesStorage2.transform
                );
                var imageBoardPrefab = boardImage10x10;
                if (save.map.GetLength(0) < 10) {
                    imageBoardPrefab = boardImage8x8;
                }

                var parent = curObj.transform.GetChild(5).transform;
                var boardGrid = Instantiate(imageBoardPrefab, parent).transform.GetChild(0);
                for (int i = 0; i < save.map.GetLength(1); i++) {
                    for (int j = 0; j < save.map.GetLength(0); j++) {
                        if (save.map[i, j] == 0) {
                            Instantiate(emptyCell, boardGrid);
                        } else if (save.map[i, j] == 1) {
                            Instantiate(whiteCheckerImage, boardGrid);
                        } else if (save.map[i, j] == 2) {
                            Instantiate(blackCheckerImage, boardGrid);
                        }
                    }
                }

                foreach (Transform child in curObj.transform) {
                    if (child.gameObject.TryGetComponent(out Button but)) {
                        if (but.name == "Load") {
                            but.onClick.AddListener(() => gameController.Load(save.fileName));
                            but.onClick.AddListener(() => ChangeActiveLoadMenu());
                        } else {
                            but.onClick.AddListener(() => {
                                    File.Delete(save.fileName);
                                    Destroy(curObj);
                                }
                            );
                        }
                    } else if (child.gameObject.TryGetComponent(out Text text)) {
                        if (text.name == "Date") {
                            text.text = save.saveDate;
                        } else if (text.name == "Kind") {
                            text.text = save.checkerKind;
                        }
                    } else if (child.gameObject.TryGetComponent(out RawImage image)) {
                        if (save.whoseMove == controller.ChColor.White) {
                            image.texture = whiteCheckerImage.texture;
                            image.color = whiteCheckerImage.color;
                        } else if (save.whoseMove == controller.ChColor.Black) {
                            image.texture = blackCheckerImage.texture;
                            image.color = blackCheckerImage.color;
                        }
                    }
                }
            }
        }
    }

}