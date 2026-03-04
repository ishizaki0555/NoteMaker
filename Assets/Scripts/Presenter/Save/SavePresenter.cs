// ========================================
//
// NoteMaker Project
//
// ========================================
//
// SavePresenter.cs
// ノーツ編集内容の保存・未保存状態の管理・終了時の保存確認ダイアログを扱う
// プレゼンターです。Ctrl+S や保存ボタンでの保存、未保存状態の検知、
// アプリ終了時の確認ダイアログ表示など、エディタの「保存まわり」を統括します。
// 
// ※「Note」はすべて「ノーツ」表記に統一しています。
//
//========================================

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
    /// <summary>
    /// ノーツ編集データの保存・未保存状態管理・終了時ダイアログを扱うクラスです。
    /// ・Ctrl+S / 保存ボタンで保存  
    /// ・未保存状態の検知（BPM/Offset/MaxBlock/ノーツ編集など）  
    /// ・保存ボタンの色変更  
    /// ・アプリ終了時の保存確認ダイアログ  
    /// </summary>
    public class SavePresenter : MonoBehaviour
    {
        [SerializeField] Button saveButton = default;                 // 保存ボタン
        [SerializeField] Text messageText = default;                  // 状態メッセージ
        [SerializeField] Color unsavedStateButtonColor = default;     // 未保存時の色
        [SerializeField] Color savedStateButtonColor = Color.white;   // 保存済みの色

        [SerializeField] GameObject saveDialog = default;             // 保存確認ダイアログ
        [SerializeField] Button dialogSaveButton = default;           // ダイアログ：保存
        [SerializeField] Button dialogDoNotSaveButton = default;      // ダイアログ：保存しない
        [SerializeField] Button dialogCancelButton = default;         // ダイアログ：キャンセル
        [SerializeField] Text dialogMessageText = default;            // ダイアログメッセージ

        ReactiveProperty<bool> mustBeSaved = new ReactiveProperty<bool>(); // 未保存状態

        void Awake()
        {
            var editPresenter = EditNotesPresenter.Instance;

            //===============================
            // Esc → アプリ終了
            //===============================
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Escape))
                .Subscribe(_ => Application.Quit());

            //===============================
            // 保存アクション（Ctrl+S / ボタン）
            //===============================
            var saveActionObservable =
                this.UpdateAsObservable()
                    .Where(_ => KeyInput.CtrlPlus(KeyCode.S))
                    .Merge(saveButton.OnClickAsObservable());

            //===============================
            // 未保存状態の検知
            //===============================
            mustBeSaved =
                Observable.Merge(
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
                    .Do(unsaved =>
                        saveButton.GetComponent<Image>().color =
                            unsaved ? unsavedStateButtonColor : savedStateButtonColor)
                    .ToReactiveProperty();

            // メッセージ表示
            mustBeSaved.SubscribeToText(messageText,
                unsaved => unsaved ? "保存が必要な状態" : "");

            // 保存実行
            saveActionObservable.Subscribe(_ => Save());

            //===============================
            // ダイアログ：保存して終了
            //===============================
            dialogSaveButton.AddListener(
                EventTriggerType.PointerClick,
                (e) =>
                {
                    mustBeSaved.Value = false;
                    saveDialog.SetActive(false);
                    Save();
                    Application.Quit();
                });

            //===============================
            // ダイアログ：保存せず終了
            //===============================
            dialogDoNotSaveButton.AddListener(
                EventTriggerType.PointerClick,
                (e) =>
                {
                    mustBeSaved.Value = false;
                    saveDialog.SetActive(false);
                    Application.Quit();
                });

            //===============================
            // ダイアログ：キャンセル
            //===============================
            dialogCancelButton.AddListener(
                EventTriggerType.PointerClick,
                (e) =>
                {
                    saveDialog.SetActive(false);
                });

            //===============================
            // アプリ終了時のフック
            //===============================
            Application.wantsToQuit += ApplicationQuit;
        }

        /// <summary>
        /// アプリ終了時の保存確認。
        /// </summary>
        bool ApplicationQuit()
        {
            // 未保存状態で無ければそのまま終了
            if (mustBeSaved.Value)
            {
                dialogMessageText.text =
                    "ノーツ '" + EditData.Name.Value + "' の変更を保存しますか？"
                    + System.Environment.NewLine
                    + "保存しない場合、変更内容は失われます。";

                saveDialog.SetActive(true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// ノーツデータと楽曲ファイルを保存します。
        /// </summary>
        public void Save()
        {
            var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);
            var difficultyName = EditData.DifficultyName.Value;

            //===============================
            // Notes/曲名 フォルダ作成
            //===============================
            var notesRoot =
                Path.Combine(Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Notes");
            var musicFolder = Path.Combine(notesRoot, musicName);

            if (!Directory.Exists(musicFolder))
                Directory.CreateDirectory(musicFolder);

            //===============================
            // 譜面 JSON 保存
            //===============================
            var jsonFileName = $"{difficultyName}.json";
            var jsonPath = Path.Combine(musicFolder, jsonFileName);

            var json = EditDataSerializer.Serialize();
            File.WriteAllText(jsonPath, json, System.Text.Encoding.UTF8);

            //===============================
            // 楽曲ファイルのコピー（music.xxx）
            //===============================
            var sourceMusicPath = Path.Combine(
                MusicSelector.DirectoryPath.Value,
                MusicSelector.SelectedFileName.Value);

            if (File.Exists(sourceMusicPath))
            {
                var ext = Path.GetExtension(sourceMusicPath);
                var destMusicPath = Path.Combine(musicFolder, "music" + ext);
                File.Copy(sourceMusicPath, destMusicPath, true);
            }

            //===============================
            // 曲ファイルのコピー（フォルダ直下のファイル）
            //===============================
            var musicFilePath = MusicSelector.DirectoryPath.Value;
            if (File.Exists(musicFilePath))
            {
                var musicFileName = Path.GetFileName(musicFilePath);
                var destMusicPath = Path.Combine(musicFolder, musicFileName);
                File.Copy(musicFilePath, destMusicPath, true);
            }

            messageText.text = "保存済み";
        }
    }
}
