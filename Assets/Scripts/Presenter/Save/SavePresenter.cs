using NoteMaker.Model;
using NoteMaker.Utility;
using System.IO;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class SavePresenter : MonoBehaviour
    {
        [SerializeField]
        Button saveButton = default;
        [SerializeField]
        Text messageText = default;
        [SerializeField]
        Color unsavedStateButtonColor = default;
        [SerializeField]
        Color savedStateButtonColor = Color.white;

        [SerializeField]
        GameObject saveDialog = default;
        [SerializeField]
        Button dialogSaveButton = default;
        [SerializeField]
        Button dialogDoNotSaveButton = default;
        [SerializeField]
        Button dialogCancelButton = default;
        [SerializeField]
        Text dialogMessageText = default;

        ReactiveProperty<bool> mustBeSaved = new ReactiveProperty<bool>();

        void Awake()
        {
            var editPresenter = EditNotesPresenter.Instance;

            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Escape))
                .Subscribe(_ => Application.Quit());

            var saveActionObservable = this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.S))
                .Merge(saveButton.OnClickAsObservable());

            mustBeSaved = Observable.Merge(
                    EditData.BPM.Select(_ => true),
                    EditData.OffsetSamples.Select(_ => true),
                    EditData.MaxBlock.Select(_ => true),
                    editPresenter.RequestForEditNote.Select(_ => true),
                    editPresenter.RequestForAddNote.Select(_ => true),
                    editPresenter.RequestForRemoveNote.Select(_ => true),
                    editPresenter.RequestForChangeNoteStatus.Select(_ => true),
                    Audio.OnLoad.Select(_ => false),
                    saveActionObservable.Select(_ => false))
                .SkipUntil(Audio.OnLoad.DelayFrame(1))
                .Do(unsaved => saveButton.GetComponent<Image>().color = unsaved ? unsavedStateButtonColor : savedStateButtonColor)
                .ToReactiveProperty();

            mustBeSaved.SubscribeToText(messageText, unsaved => unsaved ? "保存が必要な状態" : "");

            saveActionObservable.Subscribe(_ => Save());

            dialogSaveButton.AddListener(
                EventTriggerType.PointerClick,
                (e) =>
                {
                    mustBeSaved.Value = false;
                    saveDialog.SetActive(false);
                    Save();
                    Application.Quit();
                });

            dialogDoNotSaveButton.AddListener(
                EventTriggerType.PointerClick,
                (e) =>
                {
                    mustBeSaved.Value = false;
                    saveDialog.SetActive(false);
                    Application.Quit();
                });

            dialogCancelButton.AddListener(
                EventTriggerType.PointerClick,
                (e) =>
                {
                    saveDialog.SetActive(false);
                });

            Application.wantsToQuit += ApplicationQuit;
        }

        bool ApplicationQuit()
        {
            if (mustBeSaved.Value)
            {
                dialogMessageText.text = "Do you want to save the changes you made in the note '"
                    + EditData.Name.Value + "' ?" + System.Environment.NewLine
                    + "Your changes will be lost if you don't save them.";
                saveDialog.SetActive(true);
                return false;
            }

            return true;
        }

        public void Save()
        {
            var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);
            var difficultyName = EditData.DifficultyName.Value;

            // Notes/曲名/のフォルダを作成
            var notesRoot = Path.Combine(Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Notes");
            var musicFolder = Path.Combine(notesRoot, musicName);

            if(!Directory.Exists(musicFolder))
            {
                Directory.CreateDirectory(musicFolder);
            }

            // 譜面ファイルの保存先
            var jsonFileName = $"{difficultyName}.json";
            var jsonPath = Path.Combine(musicFolder, jsonFileName);

            // 譜面データを保存
            var json = EditDataSerializer.Serialize();
            File.WriteAllText(jsonPath, json, System.Text.Encoding.UTF8);

            // 曲ファイルのコピー
            var musicFilePath = MusicSelector.DirectoryPath.Value;
            if(File.Exists(musicFilePath))
            {
                var musicFileName = Path.GetFileName(musicFilePath);
                var destMusicpath = Path.Combine(musicFolder, musicFileName);

                // 上書きコピー
                File.Copy(musicFilePath, destMusicpath, true);
            }

            messageText.text = "保存済み";
        }
    }
}
