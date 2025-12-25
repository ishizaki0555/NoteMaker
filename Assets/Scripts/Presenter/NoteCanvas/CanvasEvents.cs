// ========================================
//
// CanvasEvents.cs
//
// ========================================
//
// キャンバス上のマウスイベントを UniRx の Subject として公開するクラス。
// ・ノート領域（NotesRegion）
// ・波形領域（WaveformRegion）
// ・縦線（VerticalLine）
// ・マウスホイール
// などのイベントを Presenter 層へ通知する役割を持つ。
//
// NoteCanvas の「マウスが領域上にあるかどうか」もここで更新する。
//
// ========================================

using NoteMaker.Model;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class CanvasEvents : MonoBehaviour
    {
        // Notes 領域のイベント
        public readonly Subject<Vector3> NotesRegionOnMouseUpObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> NotesRegionOnMouseExitObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> NotesRegionOnMouseDownObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> NotesRegionOnMouseEnterObservable = new Subject<Vector3>();

        // 縦線クリック
        public readonly Subject<Vector3> VerticalLineOnMouseDownObservable = new Subject<Vector3>();

        // Waveform 領域のイベント
        public readonly Subject<Vector3> WaveformRegionOnMouseDownObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> WaveformRegionOnMouseExitObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> WaveformRegionOnMouseEnterObservable = new Subject<Vector3>();

        // マウスホイール
        public readonly Subject<float> MouseScrollWheelObservable = new Subject<float>();

        /// <summary>
        /// マウスホイールと領域ホバー状態の監視をセットアップする。
        /// </summary>
        void Awake()
        {
            // -----------------------------
            // マウスホイール
            // -----------------------------
            this.UpdateAsObservable()
                .Select(_ => Input.GetAxis("Mouse ScrollWheel"))
                .Where(delta => delta != 0)
                .Subscribe(MouseScrollWheelObservable.OnNext);

            // -----------------------------
            // Notes 領域のホバー状態
            // -----------------------------
            NotesRegionOnMouseExitObservable.Select(_ => false)
                .Merge(NotesRegionOnMouseEnterObservable.Select(_ => true))
                .Subscribe(isMouseOver => NoteCanvas.IsMouseOverNotesRegion.Value = isMouseOver);

            // -----------------------------
            // Waveform 領域のホバー状態
            // -----------------------------
            WaveformRegionOnMouseExitObservable.Select(_ => false)
                .Merge(WaveformRegionOnMouseEnterObservable.Select(_ => true))
                .Subscribe(isMouseOver => NoteCanvas.IsMouseOverWaveformRegion.Value = isMouseOver);
        }

        // -----------------------------
        // Notes 領域イベント
        // -----------------------------
        public void NotesRegionOnMouseUp() { NotesRegionOnMouseUpObservable.OnNext(Input.mousePosition); }
        public void NotesRegionOnMouseExit() { NotesRegionOnMouseExitObservable.OnNext(Input.mousePosition); }
        public void NotesRegionOnMouseDown() { NotesRegionOnMouseDownObservable.OnNext(Input.mousePosition); }
        public void NotesRegionOnMouseEnter() { NotesRegionOnMouseEnterObservable.OnNext(Input.mousePosition); }

        // -----------------------------
        // 縦線クリック
        // -----------------------------
        public void VerticalLineOnMouseDown() { VerticalLineOnMouseDownObservable.OnNext(Input.mousePosition); }

        // -----------------------------
        // Waveform 領域イベント
        // -----------------------------
        public void WaveformRegionOnMouseDown() { WaveformRegionOnMouseDownObservable.OnNext(Input.mousePosition); }
        public void WaveformRegionOnMouseExit() { WaveformRegionOnMouseExitObservable.OnNext(Input.mousePosition); }
        public void WaveformRegionOnMouseEnter() { WaveformRegionOnMouseEnterObservable.OnNext(Input.mousePosition); }
    }
}
