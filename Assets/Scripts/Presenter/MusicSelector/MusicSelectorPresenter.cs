// ========================================
//
// NoteMaker Project
//
// ========================================
//
// MusicSelectorPresenter.cs
// 楽曲フォルダの選択・移動・ファイル一覧表示・Undo/Redo・楽曲読み込みなど、
// 「楽曲選択画面」の全体的な振る舞いを管理するプレゼンターです。
// ディレクトリパスの変更、ファイルリストの更新、UI ボタン制御など
// 多くの機能が集約されるため、可読性と意図の明確化を重視して整理しています。
//
//========================================

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
    /// <summary>
    /// 楽曲選択画面の UI と Model を同期させるクラスです。
    /// ・ディレクトリパスの入力・変更  
    /// ・ファイルリストの更新と表示  
    /// ・Undo/Redo によるディレクトリ移動の巻き戻し  
    /// ・楽曲読み込み（MusicLoader 連携）  
    /// ・Notes/Musics フォルダを OS のエクスプローラで開く  
    /// </summary>
    public class MusicSelectorPresenter : MonoBehaviour
    {
        [SerializeField] InputField directoryPathInputField = default;      // ディレクトリパス入力欄
        [SerializeField] GameObject fileItemPrefab = default;               // ファイルリストアイテムのプレハブ
        [SerializeField] GameObject fileItemContainer = default;            // アイテム配置先
        [SerializeField] Transform fileItemContainerTransform = default;    // Transform 参照
        [SerializeField] Button redoButton = default;                       // Redo ボタン
        [SerializeField] Button undoButton = default;                       // Undo ボタン
        [SerializeField] Button loadButton = default;                       // 楽曲読み込みボタン
        [SerializeField] Button openNotesButton = default;                  // Notes フォルダを開く
        [SerializeField] Button openMusicsButton = default;                 // Musics フォルダを開く
        [SerializeField] MusicLoader musicLoader = default;                 // 楽曲ローダー

        void Start()
        {
            // Undo / Redo ボタン制御
            ChangeLocationCommandManager.CanUndo.SubscribeToInteractable(undoButton);
            ChangeLocationCommandManager.CanRedo.SubscribeToInteractable(redoButton);

            undoButton.OnClickAsObservable().Subscribe(_ => ChangeLocationCommandManager.Undo());
            redoButton.OnClickAsObservable().Subscribe(_ => ChangeLocationCommandManager.Redo());

            // ワークスペースパス → ディレクトリ入力欄
            Settings.WorkSpacePath
                .Where(path => !string.IsNullOrEmpty(path))
                .Subscribe(workSpacePath =>
                    directoryPathInputField.text = Path.Combine(workSpacePath, "Musics"));

            // 入力欄 → DirectoryPath
            directoryPathInputField.OnValueChangedAsObservable()
                .Subscribe(path => MusicSelector.DirectoryPath.Value = path);

            // DirectoryPath → 入力欄
            MusicSelector.DirectoryPath
                .Subscribe(path => directoryPathInputField.text = path);

            // DirectoryPath の Undo/Redo 対応
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

            // ファイルリスト更新（300ms ごとに監視）
            Observable.Timer(TimeSpan.FromMilliseconds(300), TimeSpan.Zero)
                .Where(_ => Directory.Exists(MusicSelector.DirectoryPath.Value))
                .Select(_ => new DirectoryInfo(MusicSelector.DirectoryPath.Value))
                .Select(dir =>
                    dir.GetDirectories().Select(d => new FileItemInfo(true, d.FullName))
                        .Concat(dir.GetFiles().Select(f => new FileItemInfo(false, f.FullName)))
                        .ToList())
                .Where(list =>
                    !list.Select(item => item.fullName)
                        .SequenceEqual(MusicSelector.FilePathList.Value.Select(item => item.fullName)))
                .Subscribe(list => MusicSelector.FilePathList.Value = list);

            // ファイルリスト UI 再構築
            MusicSelector.FilePathList.AsObservable()
                .Do(_ =>
                {
                    // 既存アイテム削除
                    Enumerable.Range(0, fileItemContainerTransform.childCount)
                        .Select(i => fileItemContainerTransform.GetChild(i))
                        .ToList()
                        .ForEach(child => Destroy(child.gameObject));
                })
                .SelectMany(list => list)
                .Select(info => new { info, obj = Instantiate(fileItemPrefab) })
                .Do(elm => elm.obj.transform.SetParent(fileItemContainer.transform))
                .Subscribe(elm => elm.obj.GetComponent<FileListItem>().SetInfo(elm.info));

            // 楽曲読み込み
            loadButton.OnClickAsObservable()
                .Select(_ => MusicSelector.SelectedFileName.Value)
                .Where(name => !string.IsNullOrEmpty(name))
                .Subscribe(name => musicLoader.Load(name));

            // Notes / Musics フォルダを開く
            openNotesButton.OnClickAsObservable()
                .Subscribe(_ => OpenNotesFolder());

            openMusicsButton.OnClickAsObservable()
                .Subscribe(_ => OpenMusicsFolder());

            // 初回起動時のディレクトリ作成
            if (!Directory.Exists(MusicSelector.DirectoryPath.Value))
            {
                SettingsSerializer.Deserialize(SettingsWindowPresenter.LoadSettingsJson());
                Directory.CreateDirectory(MusicSelector.DirectoryPath.Value);
            }
        }

        /// <summary>
        /// Notes フォルダを OS のエクスプローラで開きます。
        /// </summary>
        void OpenNotesFolder()
        {
            var notesRoot = Path.Combine(
                Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Notes");

            if (!Directory.Exists(notesRoot))
                Directory.CreateDirectory(notesRoot);

            System.Diagnostics.Process.Start(notesRoot);
        }

        /// <summary>
        /// Musics フォルダを OS のエクスプローラで開きます。
        /// </summary>
        void OpenMusicsFolder()
        {
            var musicsRoot = Path.Combine(
                Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Musics");

            if (!Directory.Exists(musicsRoot))
                Directory.CreateDirectory(musicsRoot);

            System.Diagnostics.Process.Start(musicsRoot);
        }
    }
}
