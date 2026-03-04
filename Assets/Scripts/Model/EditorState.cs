// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// EditorState.cs
// エディタ全体の状態（波形表示の ON/OFF、クラップ音の ON/OFF）を保持する
// シンプルなステート管理クラスです。
// ReactiveProperty により UI や描画処理が自動的に更新されます。
// 
//========================================

using NoteMaker.Utility;
using UniRx;

namespace NoteMaker.Model
{
    /// <summary>
    /// エディタの状態を管理するクラスです。
    /// 波形表示の有効/無効、クラップ音の有効/無効といった
    /// UI や描画処理に関わるフラグを ReactiveProperty として公開します。
    /// </summary>
    public class EditorState : SingletonMonoBehaviour<EditorState>
    {
        ReactiveProperty<bool> waveformDisplayEnabled_ = new ReactiveProperty<bool>(true); // 波形表示の ON/OFF
        ReactiveProperty<bool> clapSoundEffectEnabled_ = new ReactiveProperty<bool>(true); // クラップ音の ON/OFF

        /// <summary>
        /// 波形表示の ON/OFF を管理する ReactiveProperty。
        /// </summary>
        public static ReactiveProperty<bool> WaveformDisplayEnabled => Instance.waveformDisplayEnabled_;

        /// <summary>
        /// クラップ音の ON/OFF を管理する ReactiveProperty。
        /// </summary>
        public static ReactiveProperty<bool> ClapSoundEffectEnabled => Instance.clapSoundEffectEnabled_;
    }
}
