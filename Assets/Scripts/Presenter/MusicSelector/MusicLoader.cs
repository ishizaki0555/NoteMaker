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
        void Awake()
        {
            ResetEditor();
        }

        public void Load(string fileName)
        {
            EditData.Name.Value = Path.GetFileNameWithoutExtension(fileName);
            StartCoroutine(LoadMusic(fileName));
        }

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
                    // TODO: 読み込み失敗時の処理
                }
                else
                {
                    EditData.Name.Value = fileName;
                    var difficultyName = EditData.DifficultyName.Value;
                    LoadEditData(difficultyName);

                    Loadbanner(EditData.Name.Value);

                    Audio.OnLoad.OnNext(Unit.Default);
                }
            }
        }

        void LoadEditData(string difficultyName)
        {
            var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);
            var bannerPath = BannerFileUtility.GetBannerPath(musicName);

            var notesRoot = Path.Combine(Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Notes");
            var musicFolder = Path.Combine(notesRoot, musicName);

            var jsonPath = Path.Combine(musicFolder, $"{difficultyName}.json");

            if(File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                EditDataSerializer.Deserialize(json);
            }
            else
            {
                Debug.LogError($"該当の難易度が見つかりませんでした。{difficultyName}, {jsonPath}");
            }
        }

        void Loadbanner(string musicName)
        {
            var bannerPath = BannerFileUtility.GetBannerPath(musicName);

            if(!string.IsNullOrEmpty(bannerPath))
            {
                BannerSettings.BannerPath.Value = bannerPath;
            }
            else
            {
                BannerSettings.BannerPath.Value = "";
            }
        }

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
            EditData.Name.Value = "";
            EditData.MaxBlock.Value = Settings.MaxBlock;
            EditData.LPB.Value = 4;

            foreach (var note in EditData.Notes.Values)
            {
                note.Dispose();
            }

            EditData.Notes.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}
