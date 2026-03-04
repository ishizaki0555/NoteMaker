// ========================================
//
// NoteMaker Project
//
// ========================================
//
// SettingsWindowPresenter.cs
// ノーツ入力設定ウィンドウ全体の管理を行うプレゼンターです。
// ・設定ファイル（settings.json）の読み込み／保存
// ・最大ブロック数に応じたキー設定アイテムの生成
// ・キー設定変更時の自動保存
// ・ワークスペースパス変更時の保存
//
//========================================

using NoteMaker.DTO;
using NoteMaker.Model;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 設定ウィンドウの初期化・項目生成・設定保存を管理するクラスです。
    /// ・settings.json の読み込み／生成  
    /// ・最大ブロック数に応じて InputNoteKeyCodeSettingsItem を生成  
    /// ・キー設定変更や MaxBlock 変更時に自動保存  
    /// </summary>
    public class SettingsWindowPresenter : MonoBehaviour
    {
        [SerializeField] GameObject itemPrefab = default;             // キー設定アイテムのプレハブ
        [SerializeField] Transform itemContentTransform = default;    // アイテム配置先

        static string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Settings");
        static string fileName = "settings.json";
        static string filePath = Path.Combine(directoryPath, fileName);

        /// <summary>
        /// 設定ファイルを読み込み、存在しなければデフォルト設定を生成します。
        /// </summary>
        public static string LoadSettingsJson()
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!File.Exists(filePath))
                File.WriteAllText(filePath,
                    JsonUtility.ToJson(SettingsDTO.GetDefaultSettings()),
                    System.Text.Encoding.UTF8);

            return File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 設定を JSON として保存します。
        /// </summary>
        void SaveSettings()
        {
            File.WriteAllText(filePath, SettingsSerializer.Serialize(), System.Text.Encoding.UTF8);
        }

        void Awake()
        {
            //===============================
            // 設定ファイル読み込み
            //===============================
            SettingsSerializer.Deserialize(LoadSettingsJson());

            //===============================
            // MaxBlock 変更時 → UI 再生成
            //===============================
            EditData.MaxBlock
                .Do(_ =>
                {
                    // 既存アイテム削除
                    Enumerable.Range(0, itemContentTransform.childCount)
                        .Select(i => itemContentTransform.GetChild(i))
                        .ToList()
                        .ForEach(child => Destroy(child.gameObject));
                })
                .Do(maxNum =>
                {
                    // キー設定リストが不足している場合は補充
                    if (Settings.NoteInputKeyCodes.Value.Count < maxNum)
                    {
                        Settings.NoteInputKeyCodes.Value.AddRange(
                            Enumerable.Range(0, maxNum - Settings.NoteInputKeyCodes.Value.Count)
                                .Select(_ => KeyCode.None));
                    }
                })
                .SelectMany(maxNum => Enumerable.Range(0, maxNum))
                .Subscribe(num =>
                {
                    var obj = Instantiate(itemPrefab);
                    obj.transform.SetParent(itemContentTransform);

                    var item = obj.GetComponent<InputNoteKeyCodeSettingsItem>();
                    var key = num < Settings.NoteInputKeyCodes.Value.Count
                        ? Settings.NoteInputKeyCodes.Value[num]
                        : KeyCode.None;

                    item.SetData(num, key);
                });

            //===============================
            // 設定変更時の自動保存
            //===============================
            Observable.Merge(
                    Settings.RequestForChangeInputNoteKeyCode.AsUnitObservable(),
                    EditData.MaxBlock.AsUnitObservable(),
                    Settings.WorkSpacePath.AsUnitObservable())
                .Where(_ => Settings.IsOpen.Value) // 設定ウィンドウが開いている時のみ保存
                .DelayFrame(1)                     // UI 更新後に保存
                .Subscribe(_ => SaveSettings());
        }
    }
}
