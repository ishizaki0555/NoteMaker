// ========================================
//
// NoteMaker Project
//
// ========================================
//
// ToggleClapSoundEffectEnablePresenter.cs
// 判定時のクラップ音（Clap Sound Effect）の ON/OFF を切り替える
// シンプルなプレゼンターです。
// Toggle の状態を EditorState.ClapSoundEffectEnabled と同期させます。
//
//========================================

using NoteMaker.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// クラップ音の有効／無効を切り替える Toggle の Presenter です。
    /// ・Toggle の変更を EditorState.ClapSoundEffectEnabled に反映  
    /// ・ReactiveProperty によって他のシステムとも自動連動  
    /// </summary>
    public class ToggleClapSoundEffectEnablePresenter : MonoBehaviour
    {
        [SerializeField] Toggle toggle = default; // クラップ音 ON/OFF トグル

        void Awake()
        {
            toggle
                .OnValueChangedAsObservable()
                .Subscribe(isEnabled =>
                    EditorState.ClapSoundEffectEnabled.Value = isEnabled);
        }
    }
}
