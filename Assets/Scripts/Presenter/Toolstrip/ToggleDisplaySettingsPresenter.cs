// ========================================
//
// NoteMaker Project
//
// ========================================
//
// ToggleDisplaySettingsPresenter.cs
// 設定ウィンドウ（SettingsWindow）の開閉を管理するプレゼンターです。
// ・ボタン押下で開閉
// ・Esc キー、ウィンドウ外クリックで閉じる
// ・開閉時に SelectedBlock をリセット
// ・ウィンドウのアクティブ状態を同期
//
//========================================

using NoteMaker.Model;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 設定ウィンドウの開閉制御を行うクラスです。
    /// ・ボタン押下で Settings.IsOpen をトグル  
    /// ・Esc キー、ウィンドウ外クリックで閉じる  
    /// ・開閉時に SelectedBlock をリセット  
    /// ・ウィンドウの表示状態を同期  
    /// </summary>
    public class ToggleDisplaySettingsPresenter : MonoBehaviour
    {
        [SerializeField] Button toggleDisplaySettingsButton = default; // 設定ウィンドウ開閉ボタン
        [SerializeField] GameObject settingsWindow = default;          // 設定ウィンドウ本体

        bool isMouseOverSettingsWindow = false; // ウィンドウ上にマウスがあるか

        void Awake()
        {
            //===============================
            // ボタン押下で開閉
            //===============================
            toggleDisplaySettingsButton
                .OnClickAsObservable()
                .Subscribe(_ =>
                    Settings.IsOpen.Value = !Settings.IsOpen.Value);

            //===============================
            // Esc またはウィンドウ外クリックで閉じる
            //===============================
            Observable.Merge(
                    // Esc キー
                    this.UpdateAsObservable()
                        .Where(_ => Settings.IsOpen.Value)
                        .Where(_ => Input.GetKey(KeyCode.Escape)),

                    // ウィンドウ外クリック
                    this.UpdateAsObservable()
                        .Where(_ => Settings.IsOpen.Value)
                        .Where(_ => !isMouseOverSettingsWindow)
                        .Where(_ => Input.GetMouseButtonDown(0)))
                .Subscribe(_ =>
                    Settings.IsOpen.Value = false);

            //===============================
            // 開閉時に選択ブロックをリセット
            //===============================
            Settings.IsOpen
                .Subscribe(_ =>
                    Settings.SelectedBlock.Value = -1);

            //===============================
            // ウィンドウの表示状態を同期
            //===============================
            Settings.IsOpen
                .Subscribe(isOpen =>
                    settingsWindow.SetActive(isOpen));
        }

        /// <summary>
        /// 設定ウィンドウにマウスが入った時。
        /// </summary>
        public void OnMouseEnterSettingsWindow()
        {
            isMouseOverSettingsWindow = true;
        }

        /// <summary>
        /// 設定ウィンドウからマウスが出た時。
        /// </summary>
        public void OnMouseExitSettingsWindow()
        {
            isMouseOverSettingsWindow = false;
        }
    }
}
