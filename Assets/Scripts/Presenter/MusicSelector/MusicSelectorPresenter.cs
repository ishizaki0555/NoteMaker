// ========================================
//
// MusicSelectorPresenter.cs
//
// ========================================
//
// 楽曲選択画面の UI と状態管理を行うクラス。
// ・ディレクトリパス入力
// ・ファイルリストの更新
// ・Undo / Redo（ディレクトリ移動）
// ・ファイル選択
// ・楽曲ロード
//
// ========================================

using NoteMaker.Common;
using NoteMaker.Model;
using System;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class MusicSelectorPresenter : MonoBehaviour
    {
        [SerializeField] InputField directoryPathInputField = default;      // ディレクトリパス入力欄
        [SerializeField] GameObject fileItemPrefab = default;               // ファイルリストアイテムのプレハブ
        [SerializeField] GameObject fileItemContainer = default;            // ファイルリストの親オブジェクト
        [SerializeField] Transform fileItemContainerTransform = default;    // Transform 参照
        [SerializeField] Button redoButton = default;                       // Redo ボタン
        [SerializeField] Button undoButton = default;                       // Undo ボタン
        [SerializeField] Button loadButton = default;                       // 読み込みボタン
        [SerializeField] MusicLoader musicLoader = default;                 // 楽曲ローダー

        private void Start()
        {
            // -----------------------------
            // Undo / Redo ボタンの有効状態
            // -----------------------------
            ChangeLocationCommandManager.CanUndo.SubscribeToInteractable(undoButton);
            ChangeLocationCommandManager.CanRedo.SubscribeToInteractable(redoButton);

            undoButton.OnClickAsObservable().Subscribe(_ => ChangeLocationCommandManager.Undo());
            redoButton.OnClickAsObservable().Subscribe(_ => ChangeLocationCommandManager.Redo());

            // -----------------------------
            // ワークスペースパス → ディレクトリ入力欄
            // -----------------------------
            Settings.WorkSpacePath
                .Subscribe(workSpacePath =>
                    directoryPathInputField.text = Path.Combine(workSpacePath, "Musics"));

            // 入力欄 → DirectoryPath
            directoryPathInputField.OnValueChangedAsObservable()
                .Subscribe(path => MusicSelector.DirectoryPath.Value = path);

            // DirectoryPath → 入力欄
            MusicSelector.DirectoryPath
                .Subscribe(path => directoryPathInputField.text = path);

            // -----------------------------
            // DirectoryPath の Undo / Redo
            // -----------------------------
            var isUndoRedoAction = false;

            MusicSelector.DirectoryPath
                .Where(path => Directory.Exists(path))
                .Buffer(2, 1)
                .Where(_ => isUndoRedoAction ? (isUndoRedoAction = false) : true)
                .Select(b => new { prev = b[0], current = b[1] })
                .Subscribe(path =>
                    ChangeLocationCommandManager.Do(
                        new Command(
                            () => { },
                            () => { isUndoRedoAction = true; MusicSelector.DirectoryPath.Value = path.prev; },
                            () => { isUndoRedoAction = true; MusicSelector.DirectoryPath.Value = path.current; }
                        )));

            // -----------------------------
            // ディレクトリ監視 → ファイルリスト更新
            // -----------------------------
            Observable.Timer(TimeSpan.FromMilliseconds(300), TimeSpan.Zero)
                .Where(_ => Directory.Exists(MusicSelector.DirectoryPath.Value))
                .Select(_ => new DirectoryInfo(MusicSelector.DirectoryPath.Value))
                .Select(directoryInfo =>
                    directoryInfo.GetDirectories().Select(directory => new FileItemInfo(true, directory.FullName))
                        .Concat(directoryInfo.GetFiles().Select(file => new FileItemInfo(false, file.FullName)))
                        .ToList())
                .Where(x => !x.Select(item => item.fullName)
                    .SequenceEqual(MusicSelector.FilePathList.Value.Select(item => item.fullName)))
                .Subscribe(filePathList => MusicSelector.FilePathList.Value = filePathList);

            // -----------------------------
            // ファイルリスト UI の再構築
            // -----------------------------
            MusicSelector.FilePathList.AsObservable()
                .Do(_ =>
                    Enumerable.Range(0, fileItemContainerTransform.childCount)
                        .Select(i => fileItemContainerTransform.GetChild(i))
                        .ToList()
                        .ForEach(child => Destroy(child.gameObject)))
                .SelectMany(fileItemList => fileItemList)
                .Select(fileItemInfo => new { fileItemInfo, obj = Instantiate(fileItemPrefab) as GameObject })
                .Do(elm => elm.obj.transform.SetParent(fileItemContainer.transform))
                .Subscribe(elm => elm.obj.GetComponent<FileListItem>().SetInfo(elm.fileItemInfo));

            // -----------------------------
            // Load ボタン
            // -----------------------------
            loadButton.OnClickAsObservable()
                .Select(_ => MusicSelector.SelectedFileName.Value)
                .Where(fileName => !string.IsNullOrEmpty(fileName))
                .Subscribe(fileName => musicLoader.Load(fileName));

            // -----------------------------
            // ディレクトリが存在しない場合は作成
            // -----------------------------
            if (!Directory.Exists(MusicSelector.DirectoryPath.Value))
            {
                Directory.CreateDirectory(MusicSelector.DirectoryPath.Value);
            }
        }
    }
}
