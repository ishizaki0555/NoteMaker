// ========================================
//
// NoteMaker Project
//
// ========================================
//
// VolumePresenter.cs
// 音量スライダーと Audio.Volume / Audio.Source.volume を同期し、
// 現在の音量に応じてアイコンを自動切り替えするプレゼンターです。
// ・スライダー操作 → Audio.Volume に反映
// ・Audio.Volume → Audio.Source.volume に反映
// ・音量値に応じてアイコンを 3 種類に切り替え（ミュート / 小 / 大）
//
//========================================

using NoteMaker.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 音量スライダーと Audio.Volume を同期し、音量に応じてアイコンを切り替えるクラスです。
    /// ・スライダー操作で Audio.Volume を更新  
    /// ・Audio.Volume の変化を Audio.Source.volume に反映  
    /// ・音量値に応じてミュート／小音量／大音量のアイコンを自動切り替え  
    /// </summary>
    public class VolumePresenter : MonoBehaviour
    {
        [SerializeField] Slider volumeController = default; // 音量スライダー
        [SerializeField] Image image = default;             // 音量アイコン表示
        [SerializeField] Sprite iconSound2 = default;       // 大音量アイコン
        [SerializeField] Sprite iconSound = default;        // 小音量アイコン
        [SerializeField] Sprite iconMute = default;         // ミュートアイコン

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        void Init()
        {
            //===============================
            // スライダー操作 → Audio.Volume
            //===============================
            volumeController
                .OnValueChangedAsObservable()
                .Subscribe(volume =>
                    Audio.Volume.Value = volume);

            //===============================
            // Audio.Volume → Audio.Source.volume
            //===============================
            Audio.Volume
                .DistinctUntilChanged()
                .Subscribe(v =>
                    Audio.Source.volume = v);

            //===============================
            // 音量に応じてアイコン切り替え
            //===============================
            Audio.Volume
                .Select(volume =>
                    Mathf.Approximately(volume, 0f)
                        ? iconMute
                        : volume < 0.6f
                            ? iconSound
                            : iconSound2)
                .DistinctUntilChanged()
                .Subscribe(sprite =>
                    image.sprite = sprite);
        }
    }
}