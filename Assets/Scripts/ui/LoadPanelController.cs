using UnityEngine;
using UnityEngine.UI;
using controller;
using System.IO;

namespace ui {
    public class LoadPanelController : MonoBehaviour {
        public FillBoardImage boardImage;
        public FillText date;
        public FillText kind;
        public SetButtonEvent delete;
        public SetButtonEvent load;

        public void FillLoadPanel(
            GameObject panel,
            SaveInfo save,
            UiResources res,
            Controller gameController,
            ToggleActive loadMenu
        ) {
            boardImage.FillImage(save, res);
            date.SetText(save.saveDate.ToString("dd.MM.yyyy HH:mm:ss"));
            kind.SetText("Checker Kind: " + save.checkerKind.ToString());
            delete.SetLisener(() => File.Delete(save.fileName));
            delete.SetLisener(() => Destroy(panel.gameObject));
            load.SetLisener(() => gameController.Load(save.fileName));
            load.SetLisener(() => loadMenu.ChangeActiveObject());

            foreach (Transform child in panel.transform) {
                if (child.name == "WhoseMoveImage") {
                    if (save.whoseMove == controller.ChColor.White) {
                        Instantiate(res.whiteCheckerImage, child.transform);
                    } else if (save.whoseMove == controller.ChColor.Black) {
                        Instantiate(res.blackCheckerImage, child.transform);
                    }
                }
            }
        }
    }
}
