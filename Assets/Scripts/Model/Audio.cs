// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// Audio.cs
// 音声再生に関する状態（AudioSource・再生位置・音量・再生中フラグなど）を
// ReactiveProperty を用いて管理するシングルトンコンポーネントです。
// 他の描画クラス（波形・グリッド線・ノーツ描画など）が参照する
// 中央的なオーディオ状態管理クラスとして機能します。
// 
//========================================

using NoteMaker.Utility;
using UniRx;
using UnityEngine;

namespace NoteMaker.Model
{
    /// <summary>
    /// 音声再生に関する状態を管理するクラスです。
    /// AudioSource の生成、再生位置（サンプル）、音量、再生中フラグなどを
    /// ReactiveProperty で公開し、他のシステムが購読できるようにします。
    /// </summary>
    public class Audio : SingletonMonoBehaviour<Audio>
    {
        AudioSource source_;                           // 実際の音声再生を行う AudioSource
        Subject<Unit> onLoad = new Subject<Unit>();    // 音声読み込み完了イベント
        ReactiveProperty<float> volume_ = new ReactiveProperty<float>(1);               // 音量
        ReactiveProperty<bool> isPlaying_ = new ReactiveProperty<bool>(false);          // 再生中フラグ
        ReactiveProperty<int> timeSamples_ = new ReactiveProperty<int>(0);              // 現在のサンプル位置
        ReactiveProperty<float> smoothedTimeSamples_ = new ReactiveProperty<float>(0);  // 補間されたサンプル位置

        /// <summary>
        /// AudioSource を取得します。
        /// 未生成の場合は自動的に AddComponent して生成します。
        /// </summary>
        public static AudioSource Source
        {
            get { return Instance.source_ ?? (Instance.source_ = Instance.gameObject.AddComponent<AudioSource>()); }
        }

        /// <summary>
        /// 音声読み込み完了イベントを購読できます。
        /// </summary>
        public static Subject<Unit> OnLoad { get { return Instance.onLoad; } }

        /// <summary>
        /// 音量を ReactiveProperty として公開します。
        /// </summary>
        public static ReactiveProperty<float> Volume { get { return Instance.volume_; } }

        /// <summary>
        /// 再生中かどうかを ReactiveProperty として公開します。
        /// </summary>
        public static ReactiveProperty<bool> IsPlaying { get { return Instance.isPlaying_; } }

        /// <summary>
        /// 現在のサンプル位置を ReactiveProperty として公開します。
        /// </summary>
        public static ReactiveProperty<int> TimeSamples { get { return Instance.timeSamples_; } }

        /// <summary>
        /// 補間されたサンプル位置を ReactiveProperty として公開します。
        /// 波形描画やスクロール処理で滑らかな動きを実現するために使用されます。
        /// </summary>
        public static ReactiveProperty<float> SmoothedTimeSamples { get { return Instance.smoothedTimeSamples_; } }
    }
}
