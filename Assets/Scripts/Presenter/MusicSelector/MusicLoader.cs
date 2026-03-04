// ========================================
//
// NoteMaker Project
//
// ========================================
//
// MusicLoader.cs
// 楽曲ファイル（WAV）と対応する編集データ（JSON）、バナー画像を読み込み、
// エディタ全体の状態を初期化・更新するプレゼンターです。
// ・音声読み込み（WWW）
// ・EditData の復元
// ・バナー画像の読み込み
// ・エディタ状態のリセット
// など、楽曲切り替え時の中心的な処理を担当します。
//
//========================================

using NoteMaker.Model;
using NoteMaker.Notes;
using System.Collections;
using System.IO;
using UniRx;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 楽曲ファイルと編集データを読み込み、エディタ状態を更新するクラスです。
    /// ・Load() で楽曲名を設定し、コルーチンで音声を読み込み  
    /// ・音声読み込み後に EditData（JSON）とバナーを読み込み  
    /// ・ResetEditor() でエディタ全体を初期化  
    /// </summary>
    public class MusicLoader : MonoBehaviour
    {
        /// <summary>
        /// エディタの状態を初期化します
        /// </summary>
        void Awake()
        {
            ResetEditor();
        }

        /// <summary>
        /// 楽曲ファイル名を受け取り、読み込み処理を開始します。
        /// </summary>
        public void Load(string fileName)
        {
            EditData.Name.Value = Path.GetFileNameWithoutExtension(fileName);
            StartCoroutine(LoadMusic(fileName));
        }

        /// <summary>
        /// 音声ファイル（WAV）を読み込み、対応する編集データとバナーをロードします。
        /// </summary>
        IEnumerator LoadMusic(string fileName)
        {
            // WWWクラスを使用してローカルファイルから音声を読み込みます
            using (var www = new WWW("file:///" + Path.Combine(MusicSelector.DirectoryPath.Value, fileName)))
            {
                yield return www;

                // 音声を読み込んだ後、エディタ全体の状態を初期化し、EditDataとバナーを読み込みます
                EditCommandManager.Clear();
                ResetEditor();
                Audio.Source.clip = www.GetAudioClip();

                // 音声の読み込みに失敗した場合の処理
                if (Audio.Source.clip == null)
                {
                    // TODO: 読み込み失敗時の処理
                }
                // 音声の読み込みに成功した場合、EditDataとバナーを読み込みます
                else
                {
                    // EditDataに曲名を設定
                    EditData.Name.Value = Path.GetFileNameWithoutExtension(fileName);

                    var difficultyName = EditData.DifficultyName.Value;
                    LoadEditData(difficultyName);

                    LoadBanner(EditData.Name.Value);

                    // 読み込み完了通知
                    Audio.OnLoad.OnNext(Unit.Default);
                }
            }
        }

        /// <summary>
        /// 難易度名に対応する JSON を読み込み、EditData に反映します。
        /// </summary>
        void LoadEditData(string difficultyName)
        {
            // 楽曲名からNotes/曲名/難易度名.json のパスを生成し、存在する場合は読み込んでEditDataに反映します
            var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);
            var notesRoot = Path.Combine(Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Notes");
            var musicFolder = Path.Combine(notesRoot, musicName);

            // 譜面ファイルのパスを生成
            var jsonPath = Path.Combine(musicFolder, $"{difficultyName}.json");

            // JSONファイルが存在する場合は読み込んでEditDataに反映します。存在しない場合はエラーログを出力します
            if (File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                EditDataSerializer.Deserialize(json);
            }
            else
            {
#if ENABLE_UNITYEVENTS
                Debug.LogError($"該当の難易度が見つかりませんでした。{difficultyName}, {jsonPath}");
#endif
            }
        }

        /// <summary>
        /// 楽曲フォルダ内のバナー画像を読み込み、BannerSettings に反映します。
        /// </summary>
        void LoadBanner(string musicName)
        {
            var bannerPath = BannerFileUtility.GetBannerPath(musicName);
            BannerSettings.BannerPath.Value = bannerPath ?? "";
        }

        /// <summary>
        /// エディタ全体の状態を初期化します。
        /// 楽曲切り替え時や起動時に呼び出されます。
        /// </summary>
        public void ResetEditor()
        {
            // エディタ状態の初期化
            Audio.TimeSamples.Value = 0;
            Audio.SmoothedTimeSamples.Value = 0;
            Audio.IsPlaying.Value = false;
            Audio.Source.clip = null;
            EditState.NoteType.Value = NoteTypes.Single;
            EditState.LongNoteTailPosition.Value = NotePosition.None;
            EditData.BPM.Value = 120;
            EditData.OffsetSamples.Value = 0;
            EditData.Name.Value = "";
            EditData.MaxBlock.Value = Settings.MaxBlock;
            EditData.LPB.Value = 4;

            // 既存ノートの破棄
            foreach (var note in EditData.Notes.Values)
            {
                note.Dispose();
            }

            EditData.Notes.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}
