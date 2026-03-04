// ========================================
//
// NoteMaker Project
//
// ========================================
//
// ToggleWaveformDisplayPresenter.cs
// 波形表示（Waveform）の ON/OFF を切り替えるプレゼンターです。
// ・Toggle の状態を EditorState.WaveformDisplayEnabled と同期
// ・UI → 状態 の一方向同期（状態 → UI の同期は別 Presenter が担当）
//
//========================================

using NoteMaker.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 波形表示の有効／無効を切り替える Toggle の Presenter です。
    /// ・Toggle の変更を EditorState.WaveformDisplayEnabled に反映  
    /// ・ReactiveProperty によって他システムと自動連動  
    /// </summary>
    public class ToggleWaveformDisplayPresenter : MonoBehaviour
    {
        [SerializeField] Toggle toggle = default; // 波形表示 ON/OFF トグル

        void Awake()
        {
            toggle
                .OnValueChangedAsObservable()
                .Subscribe(isEnabled =>
                    EditorState.WaveformDisplayEnabled.Value = isEnabled);
        }
    }
}
