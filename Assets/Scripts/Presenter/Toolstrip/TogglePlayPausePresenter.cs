// ========================================
//
// NoteMaker Project
//
// ========================================
//
// TogglePlayPausePresenter.cs
// 再生／一時停止を切り替えるプレゼンターです。
// ・Space キーまたはボタン押下で再生状態をトグル
// ・再生状態に応じてアイコンを変更
// ・Audio.Source の Play / Pause を同期
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
    /// 再生／一時停止の切り替えを行うクラスです。
    /// ・Space キーまたはボタン押下で Audio.IsPlaying をトグル  
    /// ・IsPlaying の変化に応じてアイコンと Audio.Source を制御  
    /// </summary>
    public class TogglePlayPausePresenter : MonoBehaviour
    {
        [SerializeField] Button togglePlayPauseButton = default; // 再生／停止ボタン
        [SerializeField] Sprite iconPlay = default;              // 再生アイコン
        [SerializeField] Sprite iconPause = default;             // 一時停止アイコン

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        void Init()
        {
            //===============================
            // Space キー or ボタン押下で再生状態トグル
            //===============================
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Space))
                .Merge(togglePlayPauseButton.OnClickAsObservable())
                .Subscribe(_ =>
                    Audio.IsPlaying.Value = !Audio.IsPlaying.Value);

            //===============================
            // 再生状態に応じてアイコンと Audio.Source を制御
            //===============================
            Audio.IsPlaying.Subscribe(playing =>
            {
                var playButtonImage = togglePlayPauseButton.GetComponent<Image>();

                if (playing)
                {
                    Audio.Source.Play();
                    playButtonImage.sprite = iconPause;
                }
                else
                {
                    Audio.Source.Pause();
                    playButtonImage.sprite = iconPlay;
                }
            });
        }
    }
}
