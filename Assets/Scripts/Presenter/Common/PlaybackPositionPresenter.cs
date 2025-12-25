// ========================================
//
// PlaybackPositionPresenter.cs
//
// ========================================
//
// 再生位置（timeSamples）の操作と UI（スライダー・時間表示）の同期を行うクラス。
// 入力デバイス（矢印キー・スクロール・ドラッグ・スライダー）からの操作を統合し、
// Audio / Model / UI を UniRx で連動させる。
//
// ========================================

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
    public class PlaybackPositionPresenter : MonoBehaviour
    {
        [SerializeField] CanvasEvents canvasEvents = default;           // キャンバスのマウスイベント
        [SerializeField] Slider playbackPositionController = default;   // 再生位置スライダー
        [SerializeField] Text playbackTimeDisplayText = default;        // 再生時間表示

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// 再生位置操作と UI 同期の初期化処理。
        /// </summary>
        void Init()
        {
            // クリップ読み込み後にスライダー最大値を設定
            this.UpdateAsObservable()
                .Where(_ => Audio.Source.clip != null)
                .Select(_ => Audio.Source.clip.samples)
                .Subscribe(samples => playbackPositionController.maxValue = samples);

            // ============================================================
            // Input → Audio.timeSamples
            // ============================================================

            // --- 矢印キー操作 ---
            var operateArrowKeyObservable = Observable.Merge(
                    this.UpdateAsObservable().Where(_ => Input.GetKey(KeyCode.RightArrow)).Select(_ => 7),
                    this.UpdateAsObservable().Where(_ => Input.GetKey(KeyCode.LeftArrow)).Select(_ => -7))
                .Select(delta => delta * (KeyInput.CtrlKey() ? 5 : 1))
                .Select(delta => delta
                    / NoteCanvas.Width.Value
                    * NoteCanvas.ScaleFactor.Value
                    * Audio.Source.clip.samples)
                .Select(delta => Audio.Source.timeSamples + delta);

            // 再生中に矢印キー操作 → 一時停止
            operateArrowKeyObservable.Where(_ => Audio.IsPlaying.Value)
                .Do(_ => Audio.IsPlaying.Value = false)
                .Subscribe(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = true);

            // 操作終了後に再生再開
            operateArrowKeyObservable.Where(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Do(_ => Audio.IsPlaying.Value = true)
                .Subscribe(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = false);

            // --- スクロールパッド（ドラッグ） ---
            var operateScrollPadObservable = this.UpdateAsObservable()
                .SkipUntil(canvasEvents.WaveformRegionOnMouseDownObservable
                    .Where(_ => !Input.GetMouseButtonDown(1)))
                .TakeWhile(_ => !Input.GetMouseButtonUp(0))
                .Select(_ => Input.mousePosition.x)
                .Buffer(2, 1).Where(b => 2 <= b.Count)
                .RepeatSafe()
                .Select(b => (b[0] - b[1])
                    / NoteCanvas.Width.Value
                    * NoteCanvas.ScaleFactor.Value
                    * Audio.Source.clip.samples)
                .Select(delta => Audio.Source.timeSamples + delta);

            // 再生中にドラッグ開始 → 一時停止
            canvasEvents.WaveformRegionOnMouseDownObservable
                .Where(_ => Audio.IsPlaying.Value)
                .Do(_ => Audio.IsPlaying.Value = false)
                .Subscribe(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = true);

            // ドラッグ終了 → 再生再開
            this.UpdateAsObservable()
                .Where(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value)
                .Where(_ => Input.GetMouseButtonUp(0))
                .Do(_ => Audio.IsPlaying.Value = true)
                .Subscribe(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = false);

            // --- マウスホイール ---
            var operateMouseScrollWheelObservable = canvasEvents.MouseScrollWheelObservable
                .Where(_ => !KeyInput.CtrlKey())
                .Select(delta => Audio.Source.clip.samples / 100f * -delta)
                .Select(deltaSamples => Audio.Source.timeSamples + deltaSamples);

            // 再生中にホイール操作 → 一時停止
            operateMouseScrollWheelObservable.Where(_ => Audio.IsPlaying.Value)
                .Do(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = true)
                .Subscribe(_ => Audio.IsPlaying.Value = false);

            // ホイール停止後 → 再生再開
            operateMouseScrollWheelObservable.Throttle(TimeSpan.FromMilliseconds(350))
                .Where(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value)
                .Do(_ => EditState.IsOperatingPlaybackPositionDuringPlay.Value = false)
                .Subscribe(_ => Audio.IsPlaying.Value = true);

            // --- スライダー操作 ---
            var operatePlayPositionSliderObservable = playbackPositionController
                .OnValueChangedAsObservable()
                .DistinctUntilChanged();

            // --- すべての入力を統合 ---
            var operatePlaybackPositionObservable = Observable.Merge(
                    operateArrowKeyObservable,
                    operateScrollPadObservable,
                    operateMouseScrollWheelObservable,
                    operatePlayPositionSliderObservable)
                .Select(timeSamples => Mathf.FloorToInt(timeSamples))
                .Select(timeSamples => Mathf.Clamp(timeSamples, 0, Audio.Source.clip.samples - 1));

            // Audio.timeSamples に反映
            operatePlaybackPositionObservable.Subscribe(timeSamples => Audio.Source.timeSamples = timeSamples);

            // Undo / Redo 用に操作履歴を記録
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
                        () => { isRedoUndoAction = true; Audio.TimeSamples.Value = x.current; })));

            // ============================================================
            // Audio.timeSamples → Model.timeSamples
            // ============================================================
            Audio.Source.ObserveEveryValueChanged(audio => audio.timeSamples)
                .DistinctUntilChanged()
                .Subscribe(timeSamples => Audio.TimeSamples.Value = timeSamples);

            // 再生が終端に達したら停止
            this.UpdateAsObservable()
                .Where(_ => Audio.Source.clip != null)
                .Where(_ => Audio.Source.timeSamples > Audio.Source.clip.samples - 1)
                .Subscribe(_ => Audio.IsPlaying.Value = false);

            // ============================================================
            // Model.timeSamples → UI
            // ============================================================

            // スライダー
            Audio.TimeSamples.Subscribe(timeSamples => playbackPositionController.value = timeSamples);

            // 時間表示
            Audio.TimeSamples
                .Select(_ => TimeSpan.FromSeconds(Audio.Source.time).ToString().Substring(3, 5))
                .DistinctUntilChanged()
                .Select(elapsedTime =>
                    elapsedTime + " / "
                    + TimeSpan.FromSeconds(Audio.Source.clip.samples / (float)Audio.Source.clip.frequency)
                        .ToString().Substring(3, 5))
                .SubscribeToText(playbackTimeDisplayText);
        }

        /// <summary>
        /// スライダーを押した瞬間の処理。
        /// </summary>
        public void PlaybackPositionControllerOnMouseDown()
        {
            if (Audio.IsPlaying.Value)
            {
                EditState.IsOperatingPlaybackPositionDuringPlay.Value = true;
                Audio.IsPlaying.Value = false;
            }
        }

        /// <summary>
        /// スライダーを離した瞬間の処理。
        /// </summary>
        public void PlaybackPositionControllerOnMouseUp()
        {
            if (EditState.IsOperatingPlaybackPositionDuringPlay.Value)
            {
                Audio.IsPlaying.Value = true;
                EditState.IsOperatingPlaybackPositionDuringPlay.Value = false;
            }
        }
    }
}
