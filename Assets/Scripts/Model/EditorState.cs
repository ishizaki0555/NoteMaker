// ========================================
//
// EditorState.cs
//
// ========================================
//
// エディター全体の状態（波形表示・効果音など）を保持するシングルトン
//
// ========================================

using NoteMaker.Utility;
using UniRx;

namespace NoteMaker.Model
{
    public class EditorState : SingletonMonoBehaviour<EditorState>
    {
        ReactiveProperty<bool> waveformDisplayEnabled_ = new ReactiveProperty<bool>(true);  // 波形表示の ON/OFF
        ReactiveProperty<bool> clapSoundEffectEnabled_ = new ReactiveProperty<bool>(true);  // ノート配置時の効果音 ON/OFF

        /// <summary>
        /// 波形表示の ON/OFF 状態
        /// </summary>
        public static ReactiveProperty<bool> WaveformDisplayEnabled
        {
            get { return Instance.waveformDisplayEnabled_; }
        }

        /// <summary>
        /// ノート配置時の効果音 ON/OFF 状態
        /// </summary>
        public static ReactiveProperty<bool> ClapSoundEffectEnabled
        {
            get { return Instance.clapSoundEffectEnabled_; }
        }
    }
}
