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
using System.Linq;

public class DLFileSavePresenter : MonoBehaviour
{
    [SerializeField] private Button openButton;                 // 開くボタン
    [SerializeField] private Button closeButton;                // 閉じるボタン
    [SerializeField] private GameObject DLWriteWindow;          // DL 書き込みウィンドウ
    [SerializeField] private InputField arttistNameField;            // 作曲者名入力フィールド
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
        if(!File.Exists(jsonPath))
        {
            Debug.LogError($"Note.json が存在しません: {jsonPath}");
            return;
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

        // 各難易度に情報を書き込む
        for(int i = 0; i < notesContainer.difficultyLevel.Length; i++)
        {

        }
    }

    /// <summary>
    /// 開くボタンが押された時の処理
    /// </summary>
    private void OpenWindow()
    {
        DLWriteWindow.SetActive(true);          // ウィンドウを表示する
    }

    /// <summary>
    /// 閉じるボタンが押された時の処理
    /// </summary>
    private void CloseWindow()
    {
        DLWriteWindow.SetActive(false);         // ウィンドウを非表示にする
    }
}
