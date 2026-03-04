// ========================================
//
// NoteMaker Project
//
// ========================================
//
// SettingWorkSpacePathPresenter.cs
// ワークスペースパス（楽曲フォルダのルート）を入力・検証し、
// Settings.WorkSpacePath と同期させるプレゼンターです。
// ・入力欄のリアルタイム検証（存在するパスかどうか）
// ・有効なパスのみ Settings に反映
// ・Settings 側の変更を UI に反映
//
//========================================

using NoteMaker.Model;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// ワークスペースパス入力欄の制御を行うクラスです。
    /// ・入力されたパスが存在するかをリアルタイムで検証  
    /// ・有効なパスのみ Settings.WorkSpacePath に反映  
    /// ・Settings 側の変更を UI に反映  
    /// </summary>
    public class SettingWorkSpacePathPresenter : MonoBehaviour
    {
        [SerializeField] InputField workSpacePathInputField = default;     // 入力欄
        [SerializeField] Text workSpacePathInputFieldText = default;       // 入力欄のテキスト
        [SerializeField] Color defaultTextColor = default;                 // 有効パス時の色
        [SerializeField] Color invalidStateTextColor = default;            // 無効パス時の色

        void Awake()
        {
            //===============================
            // 入力欄のパス検証（存在チェック）
            //===============================
            workSpacePathInputField
                .OnValueChangedAsObservable()
                .Select(path => Directory.Exists(path))
                .Subscribe(exists =>
                    workSpacePathInputFieldText.color =
                        exists ? defaultTextColor : invalidStateTextColor);

            //===============================
            // 有効なパスのみ Settings に反映
            //===============================
            workSpacePathInputField
                .OnValueChangedAsObservable()
                .Where(path => Directory.Exists(path))
                .Subscribe(path =>
                    Settings.WorkSpacePath.Value = path);

            //===============================
            // Settings 側の変更を UI に反映
            //===============================
            Settings.WorkSpacePath
                .DistinctUntilChanged()
                .Subscribe(path =>
                    workSpacePathInputField.text = path);
        }
    }
}
