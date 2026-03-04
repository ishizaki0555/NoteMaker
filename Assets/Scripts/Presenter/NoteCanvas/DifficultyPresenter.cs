// ========================================
//
// NoteMaker Project
//
// ========================================
//
// DifficultyPresenter.cs
// 難易度（DifficultyName）の選択と、対応する譜面データ（JSON）の読み込みを管理する
// プレゼンターです。Dropdown の変更を Undo/Redo 対応で扱い、
// 選択された難易度の譜面を EditData に反映します。
//
//========================================

using NoteMaker.Common;
using NoteMaker.Model;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 難易度選択 UI（Dropdown）と EditData.DifficultyName を同期させるクラスです。
    /// ・Dropdown の変更を Undo/Redo 対応で反映  
    /// ・難易度ごとの JSON を読み込み、譜面データを更新  
    /// ・該当難易度の JSON が無い場合は空データを生成  
    /// </summary>
    public class DifficultyPresenter : MonoBehaviour
    {
        [SerializeField] Dropdown dropdown = default; // 難易度選択 UI

        void Awake()
        {
            var difficultyName = EditData.DifficultyName;

            //===============================
            // Dropdown → DifficultyName（Undo/Redo 対応）
            //===============================
            dropdown.onValueChanged.AsObservable()
                .Subscribe(index =>
                {
                    var name = dropdown.options[index].text;

                    // 難易度を変更するコマンドを作成し、Undo/Redoに対応させる
                    EditCommandManager.Do(
                        new Command(
                            () =>
                            {
                                // Do時は選択された難易度を反映
                                difficultyName.Value = name;
                                LoadDifficultyChart(name);
                            },
                            () =>
                            {
                                // Undo 時は前の値に戻す
                                difficultyName.Value = difficultyName.Value;
                                LoadDifficultyChart(name);
                            },
                            () =>
                            {
                                // Redo 時は再度この難易度を読み込む
                                difficultyName.Value = name;
                                LoadDifficultyChart(name);
                            }
                        ));
                })
                .AddTo(this);
        }

        /// <summary>
        /// 選択された難易度の JSON を読み込み、EditData に反映します。
        /// </summary>
        void LoadDifficultyChart(string difficultyName)
        {
            // MusicSelector で選択された音源ファイル名から、譜面データの保存先を決定します。
            var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);

            // Notes/曲名/難易度.json のパスを構築
            var notesRoot = Path.Combine(
                Path.GetDirectoryName(MusicSelector.DirectoryPath.Value),
                "Notes");

            // Notes/曲名/ のフォルダを作成
            var musicFolder = Path.Combine(notesRoot, musicName);

            // 譜面ファイルパス
            var jsonPath = Path.Combine(musicFolder, $"{difficultyName}.json");

            // JSONが存在する場合は読み込んでEditDataに反映
            if (File.Exists(jsonPath))
            {
                EditData.Notes.Clear();
                var json = File.ReadAllText(jsonPath);
                EditDataSerializer.Deserialize(json);
            }
            // 存在しない場合はからデータを作成
            else
            {
                // 該当難易度が存在しない場合は空データを作成
                ClearEditData();
            }
        }

        /// <summary>
        /// 譜面データを初期状態にリセットします。
        /// </summary>
        void ClearEditData()
        {
            EditData.BPM.Value = 120;
            EditData.MaxBlock.Value = 4;
            EditData.OffsetSamples.Value = 0;

            EditData.Notes.Clear();
        }
    }
}
