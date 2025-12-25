// ========================================
//
// MusicLoader.cs
//
// ========================================
//
// 楽曲ファイル（wav）と譜面データ（json）の読み込み、
// およびエディタ全体の初期化処理を行うクラス。
// ・音声ファイルのロード
// ・譜面データのロード
// ・エディタ状態のリセット
//
// ========================================

using NoteMaker.Model;
using NoteMaker.Notes;
using System.Collections;
using System.IO;
using UniRx;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class MusicLoader : MonoBehaviour
    {
        private void Awake()
        {
            ResetEditor();
        }

        /// <summary>
        /// 楽曲ファイルを読み込む（コルーチン開始）。
        /// </summary>
        public void Load(string fileName)
        {
            StartCoroutine(LoadMusic(fileName));
        }

        /// <summary>
        /// 音声ファイル（wav）を読み込み、譜面データも読み込む。
        /// </summary>
        IEnumerator LoadMusic(string fileName)
        {
            using (var www = new WWW("file:///" + Path.Combine(MusicSelector.DirectoryPath.Value, fileName)))
            {
                yield return www;

                EditCommandManager.Clear();
                ResetEditor();
                Audio.Source.clip = www.GetAudioClip();

                if (Audio.Source.clip == null)
                {
                    // 読み込み失敗時の処理（未実装）
                }
                else
                {
                    EditData.Name.Value = fileName;
                    LoadEditData();
                    Audio.OnLoad.OnNext(Unit.Default);
                }
            }
        }

        /// <summary>
        /// 譜面データ（json）を読み込む。
        /// </summary>
        public void LoadEditData()
        {
            var fileName = Path.ChangeExtension(EditData.Name.Value, "json");
            var directionPath = Path.Combine(Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Notes");
            var filePath = Path.Combine(directionPath, fileName);

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                EditDataSerializer.Deserialize(json);
            }
        }

        /// <summary>
        /// エディタ全体の状態を初期化する。
        /// </summary>
        public void ResetEditor()
        {
            Audio.TimeSamples.Value = 0;
            Audio.SmoothedTimeSamples.Value = 0;
            Audio.IsPlaying.Value = false;
            Audio.Source.clip = null;

            EditState.NoteType.Value = NoteTypes.Single;
            EditState.LongNoteTailPosition.Value = NotePosition.None;

            EditData.BPM.Value = 120;
            EditData.OffsetSamples.Value = 0;
            EditData.Name.Value = "Note Maker";
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
