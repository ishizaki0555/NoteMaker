// ========================================
//
// Audio.cs
//
// ========================================
//
// 音声再生を管理するシングルトン
//
// ========================================

using NoteMaker.Utility;
using UniRx;
using UnityEngine;

namespace NoteMaker.Model
{
    public class Audio : SingletonMonoBehaviour<Audio>
    {
        AudioSource source_;                                                            // AudioSource 本体
        Subject<Unit> onLoad = new Subject<Unit>();                                     // 音声ロード完了通知
        ReactiveProperty<float> volume_ = new ReactiveProperty<float>(1);               // 音量
        ReactiveProperty<bool> isPlaying_ = new ReactiveProperty<bool>(false);          // 再生中フラグ
        ReactiveProperty<int> timeSamples_ = new ReactiveProperty<int>(0);              // 現在のサンプル位置
        ReactiveProperty<float> smoothedTimeSamples_ = new ReactiveProperty<float>(0);  // 補間サンプル位置

        public static AudioSource Source
        {
            get { return Instance.source_ ?? (Instance.source_ = Instance.gameObject.AddComponent<AudioSource>()); }
        }

        public static Subject<Unit> OnLoad { get { return Instance.onLoad; } }
        public static ReactiveProperty<float> Volume { get { return Instance.volume_; } }
        public static ReactiveProperty<bool> IsPlaying { get { return Instance.isPlaying_; } }
        public static ReactiveProperty<int> TimeSamples { get { return Instance.timeSamples_; } }
        public static ReactiveProperty<float> SmoothedTimeSamples { get { return Instance.smoothedTimeSamples_; } }
    }
}
