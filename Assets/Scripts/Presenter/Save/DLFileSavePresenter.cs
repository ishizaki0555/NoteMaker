// ========================================
//
// NoteMaker Project
//
// ========================================
//
// DLFileSavePresenter.cs
//
// 作曲者やタイトルなどの情報を含む、WAV ファイルのメタデータを保存します。
//
//========================================

using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System.IO;
using NoteMaker.DTO;
using NoteMaker.Model;

public class DLFileSavePresenter : MonoBehaviour
{
    [SerializeField] private Button openButton;                 // 開くボタン
    [SerializeField] private Button closeButton;                // 閉じるボタン
    [SerializeField] private GameObject DLWriteWindow;          // DL 書き込みウィンドウ
    [SerializeField] private InputField arttistNameField;       // 作曲者名入力フィールド
    [SerializeField] private InputField titleField;             // タイトル入力フィールド
    [SerializeField] private InputField creatorNameField;       // 譜面制作者名入力フィールド
    [SerializeField] private InputField[] difficultyLevels;     // 難易度名入力フィールドの配列
    [SerializeField] private Button writeButton;                // 書き込みボタン

    private void Awake()
    {
        // 初期状態では非表示にする
        DLWriteWindow.SetActive(false);

        // 開くボタンが押された時の処理を登録
        openButton.onClick.AsObservable()
            .Subscribe(_ => OpenWindow())
            .AddTo(this);

        // 閉じるボタンが押された時の処理を登録
        closeButton.onClick.AsObservable()
            .Subscribe(_ => CloseWindow())
            .AddTo(this);

        // 書き込みボタンが押された時の処理を登録
        writeButton.onClick.AsObservable()
            .Subscribe(_ => OnWrite())
            .AddTo(this);
    }

    /// <summary>
    /// インプットフィールドの値を書き出します。
    /// </summary>
    private void OnWrite()
    {
        // 現在編集中の曲名を入手
        var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);

        // Notes/曲名/Notes.jsonのパスを取得
        var notesRoot = Path.Combine(
            Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), 
            "Notes"
        );

        var musicFolder = Path.Combine(notesRoot, musicName);
        var jsonPath = Path.Combine(musicFolder, "Note.json");

        // Note.jsonが存在する場合のみ書き込みを行う
        // 存在しないバイアはNote.jsonを新しく生成する
        if(!File.Exists(jsonPath))
        {
            // Note.jsonが存在しない場合は新規作成する
            var newContainer = new MusicDTO.NoteContainer
            {
                artistName = arttistNameField.text,
                title = titleField.text,
                creator = creatorNameField.text,
                difficultyLevel = new int[difficultyLevels.Length],
                difficulties = new System.Collections.Generic.List<MusicDTO.DifficultyData>()
            };
            // 難易度レベルを配列に格納
            for (int i = 0; i < difficultyLevels.Length; i++)
            {
                if (int.TryParse(difficultyLevels[i].text, out int level))
                {
                    newContainer.difficultyLevel[i] = level;
                }
                else
                {
                    newContainer.difficultyLevel[i] = 0;
                }
            }
            // Note.jsonに書き込む
            var newOutPutJson = JsonUtility.ToJson(newContainer, false);
            File.WriteAllText(jsonPath, newOutPutJson, System.Text.Encoding.UTF8);
        }

        // Note.jsonを読み込む
        var json = File.ReadAllText(jsonPath);
        var notesContainer = JsonUtility.FromJson<MusicDTO.NoteContainer>(json);

        if(notesContainer == null || notesContainer.difficulties == null)
        {
            Debug.LogError("Note.json の読み込みに失敗しました。");
            return;
        }

        // 作者名を取得
        string artist = arttistNameField.text;
        notesContainer.artistName = artist;

        // タイトルを取得
        string title = titleField.text;
        notesContainer.title = title;

        // 譜面制作者名を取得
        string creator = creatorNameField.text;
        notesContainer.creator = creator;

        // 難易度レベルを取得
        if (notesContainer.difficultyLevel == null || notesContainer.difficultyLevel.Length != difficultyLevels.Length)
        {
            notesContainer.difficultyLevel = new int[difficultyLevels.Length];
        }

        // 難易度レベルを配列に格納
        for (int i = 0; i < difficultyLevels.Length; i++)
        {
            // 難易度レベルの入力値を整数に変換して格納する
            if (int.TryParse(difficultyLevels[i].text, out int level))
            {
                notesContainer.difficultyLevel[i] = level;
            }
            else
            {
                notesContainer.difficultyLevel[i] = 0;
            }
        }

        // Note.jsonに書き込む
        var outPutJson = JsonUtility.ToJson(notesContainer, false);
        File.WriteAllText(jsonPath, outPutJson, System.Text.Encoding.UTF8);
        CloseWindow();
    }

    /// <summary>
    /// 開くボタンが押された時の処理
    /// </summary>
    private void OpenWindow()
    {
        // DL書き込みウィンドウを表示する
        DLWriteWindow.SetActive(true);

        // ----------------------------------------
        //
        // Note.jsonに内容を反映する
        //
        // ----------------------------------------

        // Note.jsonのパスを取得
        if (EditData.Name != null && !string.IsNullOrEmpty(EditData.Name.Value) && MusicSelector.DirectoryPath != null && !string.IsNullOrEmpty(MusicSelector.DirectoryPath.Value))
        {
            // 現在編集中の曲名を取得
            var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);
            var notesRoot = Path.Combine(
                Path.GetDirectoryName(MusicSelector.DirectoryPath.Value),
                "Notes"
            );
            var musicFolder = Path.Combine(notesRoot, musicName);
            var jsonPath = Path.Combine(musicFolder, "Note.json");

            // Note.jsonが存在する場合のみ読み込みを行う
            if (File.Exists(jsonPath))
            {
                try
                {
                    // Note.jsonを読み込む
                    var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                    var container = JsonUtility.FromJson<MusicDTO.NoteContainer>(json);

                    // NoteContainerの内容をUIに反映する
                    if (container != null)
                    {
                        // 作況者名をUIに反映する
                        arttistNameField.text = container.artistName;
                        
                        // タイトルをUIに反映する
                        titleField.text = container.title;
                        
                        // 譜面制作者名をUIに反映する
                        creatorNameField.text = container.creator;
                        
                        // 難易度レベルをUIに反映する
                        // 難易度レベルの数がUIの配列と一致しない場合は、UIの配列に合わせて調整する
                        for (int i = 0; i < difficultyLevels.Length && i < container.difficultyLevel.Length; i++)
                        {
                            if (difficultyLevels[i] != null)
                            {
                                difficultyLevels[i].text = container.difficultyLevel[i].ToString();
                            }
                        }
                    }
                }
                catch
                {
#if UNITY_EDITOR
                    Debug.LogError($"Note.json の読み込みに失敗しました: {jsonPath}");
#endif
                }
            }
        }
    }

    /// <summary>
    /// 閉じるボタンが押された時の処理
    /// </summary>
    private void CloseWindow()
    {
        DLWriteWindow.SetActive(false);         // ウィンドウを非表示にする
    }
}
