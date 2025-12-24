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

        public void Load(string fileName)
        {
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

                if(Audio.Source.clip == null)
                {
                    // Ž¸”s
                }
                else
                {
                    EditData.Name.Value = fileName;
                    LoadEditData();
                    Audio.OnLoad.OnNext(Unit.Default);
                }
            }
        }

        public void LoadEditData()
        {
            var fileName = Path.ChangeExtension(EditData.Name.Value, "json");
            var directionPath = Path.Combine(Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Notes");
            var filePath = Path.Combine(directionPath, fileName);

            if(File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                EditDataSerializer.Deserialize(json);
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
            EditData.Name.Value = "Note Maker";
            EditData.MaxBlock.Value = Settings.MaxBlock;
            EditData.LPB.Value = 4;

            foreach(var note in EditData.Notes.Values)
            {
                note.Dispose();
            }

            EditData.Notes.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}