// ========================================
//
// SavePresenter.cs
//
// ========================================
//
// 譜面データ（json）の保存処理を管理する Presenter。
// ・Ctrl+S / 保存ボタンで保存
// ・未保存状態の検知（Reactive）
// ・アプリ終了時の保存確認ダイアログ
// ・保存後のメッセージ表示
//
// ========================================

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
        [SerializeField] Button saveButton = default;                 // 保存ボタン
        [SerializeField] Text messageText = default;                  // 保存メッセージ表示
        [SerializeField] Color unsavedStateButtonColor = default;     // 未保存時のボタン色
        [SerializeField] Color savedStateButtonColor = Color.white;   // 保存済みのボタン色
        [SerializeField] GameObject saveDialog = default;             // 保存確認ダイアログ
        [SerializeField] Button dialogSaveButton = default;           // ダイアログ：保存
        [SerializeField] Button dialogDoNotSaveButton = default;      // ダイアログ：保存しない
        [SerializeField] Button dialogCancelButton = default;         // ダイアログ：キャンセル
        [SerializeField] Text dialogMessageText = default;            // ダイアログメッセージ

        ReactiveProperty<bool> mustBeSaved = new ReactiveProperty<bool>(); // 未保存状態フラグ

        /// <summary>
        /// コンポーネント生成直後に呼ばれる初期化処理。
        /// 保存状態の監視、ショートカット、ダイアログ処理などをセットアップする。
        /// </summary>
        void Awake()
        {
            var editPresenter = EditNotesPresenter.Instance;

            // Esc → アプリ終了
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Escape))
                .Subscribe(_ => Application.Quit());

            // Ctrl+S または 保存ボタン
            var saveActionObservable = this.UpdateAsObservable()
                .Where(_ => KeyInput.CtrlPlus(KeyCode.S))
                .Merge(saveButton.OnClickAsObservable());

            // ----------------------------------------
            // 未保存状態の検知
            // ----------------------------------------
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
                .Do(unsaved =>
                    saveButton.GetComponent<Image>().color =
                        unsaved ? unsavedStateButtonColor : savedStateButtonColor)
                .ToReactiveProperty();

            // 未保存メッセージ
            mustBeSaved.SubscribeToText(messageText,
                unsaved => unsaved ? "未保存の変更があります" : "");

            // 保存実行
            saveActionObservable.Subscribe(_ => Save());

            // ----------------------------------------
            // 保存ダイアログのボタン処理
            // ----------------------------------------
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

            // アプリ終了時のフック
            Application.wantsToQuit += ApplicationQuit;
        }

        /// <summary>
        /// アプリ終了時に呼ばれる。
        /// 未保存なら保存確認ダイアログを表示し、終了をキャンセルする。
        /// </summary>
        bool ApplicationQuit()
        {
            if (mustBeSaved.Value)
            {
                dialogMessageText.text =
                    "Do you want to save the changes you made in the note '"
                    + EditData.Name.Value + "' ?" + System.Environment.NewLine
                    + "Your changes will be lost if you don't save them.";

                saveDialog.SetActive(true);
                return false; // 終了キャンセル
            }

            return true; // 終了続行
        }

        /// <summary>
        /// 譜面データ（json）を保存する。
        /// </summary>
        public void Save()
        {
            var fileName = Path.ChangeExtension(EditData.Name.Value, "json");
            var directoryPath = Path.Combine(
                Path.GetDirectoryName(MusicSelector.DirectoryPath.Value),
                "Notes");
            var filePath = Path.Combine(directoryPath, fileName);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var json = EditDataSerializer.Serialize();
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

            messageText.text = filePath + " に保存しました";
        }
    }
}
