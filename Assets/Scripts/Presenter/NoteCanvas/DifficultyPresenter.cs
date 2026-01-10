using NoteMaker.Common;
using NoteMaker.Model;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class DifficultyPresenter : MonoBehaviour
    {
        [SerializeField]
        Dropdown dropdown = default;

        void Awake()
        {
            var difficultyName = EditData.DifficultyName;


            // Dropdown → ReactiveProperty（Undo/Redo対応）
            dropdown.onValueChanged.AsObservable()
                .Subscribe(index =>
                {
                    var name = dropdown.options[index].text;

                    EditCommandManager.Do(
                        new Command(
                            () =>
                            {
                                difficultyName.Value = name;

                                LoadDifficultyChart(name);
                            },
                            () =>
                            {
                                difficultyName.Value = difficultyName.Value;

                                LoadDifficultyChart(name);
                            },
                            () =>
                            {
                                difficultyName.Value = name;

                                LoadDifficultyChart(name);
                            }
                        ));
                })
                .AddTo(this);
        }

        void LoadDifficultyChart(string difficultyName)
        {
            var musicName = Path.GetFileNameWithoutExtension(EditData.Name.Value);

            var notesRoot = Path.Combine(Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Notes");
            var musicFolder = Path.Combine(notesRoot, musicName);

            var jsonPath = Path.Combine(musicFolder, $"{difficultyName}.json");

            if(File.Exists(jsonPath))
            {
                EditData.Notes.Clear();
                var json = File.ReadAllText(jsonPath);
                EditDataSerializer.Deserialize(json);
            }
            else
            {
                // ファイルがないときは新規譜面として初期化
                ClearEditData();
            }
        }

        void ClearEditData()
        {
            EditData.BPM.Value = 120;
            EditData.MaxBlock.Value = 4;
            EditData.OffsetSamples.Value = 0;

            EditData.Notes.Clear();
        }
    }
}
