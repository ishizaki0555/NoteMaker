// ========================================
//
// NoteMaker Project
//
// ========================================
//
// PlaybackPositionPresenter.cs
// 再生位置（timeSamples）を「入力 → Audio → Model → UI」の流れで同期させる
// プレゼンタークラスです。矢印キー・スクロール・スライダー操作など
// 多様な入力を統合し、Undo/Redo にも対応した再生位置制御を行います。
// 
//========================================

using NoteMaker.Common;
using NoteMaker.Model;
using NoteMaker.Utility;
using System;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// 再生位置（timeSamples）を管理し、入力・Audio・UI を同期させるクラスです。
    /// ・矢印キー / スクロール / スライダー操作で再生位置を変更  
    /// ・再生中の操作は一時停止し、操作終了後に再開  
    /// ・Undo/Redo に対応した再生位置変更コマンドを発行  
    /// ・再生位置を UI（スライダー / 時刻表示）へ反映  
    /// </summary>
    public class PlaybackPositionPresenter : MonoBehaviour
    {
        [SerializeField] CanvasEvents canvasEvents = default;          // キャンバスイベント
        [SerializeField] Slider playbackPositionController = default;  // 再生位置スライダー
        [SerializeField] Text playbackTimeDisplayText = default;       // 再生時間表示

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// 再生位置制御の初期化処理。
        /// 入力 → Audio → Model → UI の同期ルールをすべて設定します。
        /// </summary>
        void Init()
        {
            // スライダー最大値を曲の総サンプル数に同期
            this.UpdateAsObservable()
                .Where(_ => Audio.Source.clip != null)
                .Select(_ => Audio.Source.clip.samples)
                .Subscribe(samples => playbackPositionController.maxValue = samples);

            //===============================
            // 入力（矢印キー）
            //===============================
            var operateArrowKeyObservable = Observable.Merge(
                    this.UpdateAsObservable().Where(_ => Input.GetKey(KeyCode.UpArrow)).Select(_ => 7),
                    this.UpdateAsObservable().Where(_ => Input.GetKey(KeyCode.DownArrow)).Select(_ => -7))
                .Select(delta => delta * (KeyInput.CtrlKey() ? 5 : 1))
                .Select(delta => delta
                    / NoteCanvas.Height.Value
                    * NoteCanvas.ScaleFactor.Value
                    * Audio.Source.clip.samples)
                .Select(delta => Audio.Source.timeSamples + delta);

            // 再生中に操作 → 一時停止
            operateArrowKeyObservable.Where(_ => Audio.IsPlaying.Value)
                .Do(_ => Audio.IsPlaying.Value = false)
                .Subscribe(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = true);

            // 操作終了後に再生再開
            operateArrowKeyObservable.Where(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Do(_ => Audio.IsPlaying.Value = true)
                .Subscribe(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = false);

            //===============================
            // 入力（スクロールパッド）
            //===============================
            var operateScrollPadObservable = this.UpdateAsObservable()
                .SkipUntil(canvasEvents.WaveformRegionOnMouseDownObservable
                    .Where(_ => !Input.GetMouseButtonDown(1)))
                .TakeWhile(_ => !Input.GetMouseButtonUp(0))
                .Select(_ => Input.mousePosition.y)
                .Buffer(2, 1).Where(b => 2 <= b.Count)
                .RepeatSafe()
                .Select(b => (b[0] - b[1])
                    / NoteCanvas.Height.Value
                    * NoteCanvas.ScaleFactor.Value
                    * Audio.Source.clip.samples)
                .Select(delta => Audio.Source.timeSamples + delta);

            canvasEvents.WaveformRegionOnMouseDownObservable
                .Where(_ => Audio.IsPlaying.Value)
                .Do(_ => Audio.IsPlaying.Value = false)
                .Subscribe(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = true);

            this.UpdateAsObservable()
                .Where(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value)
                .Where(_ => Input.GetMouseButtonUp(0))
                .Do(_ => Audio.IsPlaying.Value = true)
                .Subscribe(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = false);

            //===============================
            // 入力（マウスホイール）
            //===============================
            
            var operateMouseScrollWheelObservable = canvasEvents.MouseScrollWheelObservable
                .Where(_ => !KeyInput.CtrlKey())
                .Select(delta => Audio.Source.clip.samples / 100f * delta)
                .Select(deltaSamples => Audio.Source.timeSamples + deltaSamples);

            operateMouseScrollWheelObservable.Where(_ => Audio.IsPlaying.Value)
                .Do(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = true)
                .Subscribe(_ => Audio.IsPlaying.Value = false);

            operateMouseScrollWheelObservable.Throttle(TimeSpan.FromMilliseconds(350))
                .Where(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value)
                .Do(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = false)
                .Subscribe(_ => Audio.IsPlaying.Value = true);

            //===============================
            // 入力（スライダー）
            //===============================
            var operatePlayPositionSliderObservable =
                playbackPositionController.OnValueChangedAsObservable()
                    .DistinctUntilChanged();

            //===============================
            // 入力 → Audio.timeSamples
            //===============================
            var operatePlaybackPositionObservable = Observable.Merge(
                    operateArrowKeyObservable,
                    operateScrollPadObservable,
                    operateMouseScrollWheelObservable,
                    operatePlayPositionSliderObservable)
                .Select(timeSamples => Mathf.FloorToInt(timeSamples))
                .Select(timeSamples => Mathf.Clamp(timeSamples, 0, Audio.Source.clip.samples - 1));

            operatePlaybackPositionObservable
                .Subscribe(timeSamples => Audio.Source.timeSamples = timeSamples);

            // Undo/Redo 対応
            var isRedoUndoAction = false;

            operatePlaybackPositionObservable
                .Buffer(operatePlaybackPositionObservable.ThrottleFrame(10))
                .Where(_ => isRedoUndoAction ? (isRedoUndoAction = false) : true)
                .Where(b => 2 <= b.Count)
                .Select(x => new { current = x.Last(), prev = x.First() })
                .Subscribe(x => EditCommandManager.Do(
                    new Command(
                        () => Audio.TimeSamples.Value = x.current,
                        () => { isRedoUndoAction = true; Audio.TimeSamples.Value = x.prev; },
                        () => { isRedoUndoAction = true; Audio.TimeSamples.Value = x.current; }
                    )));

            //===============================
            // Audio.timeSamples → Model
            //===============================
            Audio.Source.ObserveEveryValueChanged(audio => audio.timeSamples)
                .DistinctUntilChanged()
                .Subscribe(timeSamples => Audio.TimeSamples.Value = timeSamples);

            // 曲の終端に到達したら停止
            this.UpdateAsObservable()
                .Where(_ => Audio.Source.clip != null)
                .Where(_ => Audio.Source.timeSamples > Audio.Source.clip.samples - 1)
                .Subscribe(_ => Audio.IsPlaying.Value = false);

            //===============================
            // Model → UI（スライダー）
            //===============================
            Audio.TimeSamples.Subscribe(timeSamples =>
                playbackPositionController.value = timeSamples);

            //===============================
            // Model → UI（時間表示）
            //===============================
            Audio.TimeSamples
                .Select(_ => TimeSpan.FromSeconds(Audio.Source.time).ToString().Substring(3, 5))
                .DistinctUntilChanged()
                .Select(elapsed =>
                    elapsed + " / " +
                    TimeSpan.FromSeconds(
                        Audio.Source.clip.samples / (float)Audio.Source.clip.frequency
                    ).ToString().Substring(3, 5))
                .SubscribeToText(playbackTimeDisplayText);
        }

        /// <summary>
        /// スライダーを押した瞬間に再生を一時停止します。
        /// </summary>
        public void PlaybackPositionControllerOnMouseDown()
        {
            // スライダーを押したときに再生中だった場合のみ一時停止する
            if (Audio.IsPlaying.Value)
            {
                EditState.IsOperatingPlaybackPositionDuringPlay.Value = true;
                Audio.IsPlaying.Value = false;
            }
        }

        /// <summary>
        /// スライダー操作終了後に再生を再開します。
        /// </summary>
        public void PlaybackPositionControllerOnMouseUp()
        {
            // スライダーを押したときに再生中だった場合のみ再開する
            if (EditState.IsOperatingPlaybackPositionDuringPlay.Value)
            {
                Audio.IsPlaying.Value = true;
                EditState.IsOperatingPlaybackPositionDuringPlay.Value = false;
            }
        }
    }
}
