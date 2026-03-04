// ========================================
//
// NoteMaker Project
//
// ========================================
//
// CanvasEvents.cs
// Notes 領域・Waveform 領域・縦ラインなど、エディタ画面上の
// マウスイベントを UniRx の Subject として公開するイベントハブです。
// Presenter から購読され、クリック・ホイール・マウスオーバー状態などを
// 一元的に扱えるようにします。
//
//========================================

using NoteMaker.Model;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// キャンバス上のマウスイベントを Subject として公開するクラスです。
    /// ・Notes 領域の Enter / Exit / Down / Up  
    /// ・Waveform 領域の Enter / Exit / Down  
    /// ・縦ラインのクリック  
    /// ・マウスホイール  
    /// 
    /// NoteCanvas.IsMouseOverNotesRegion / WaveformRegion を更新し、
    /// 他の Presenter が購読して UI 挙動を制御できるようにします。
    /// </summary>
    public class CanvasEvents : MonoBehaviour
    {
        public readonly Subject<Vector3> NotesRegionOnMouseUpObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> NotesRegionOnMouseExitObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> NotesRegionOnMouseDownObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> NotesRegionOnMouseEnterObservable = new Subject<Vector3>();

        public readonly Subject<Vector3> VerticalLineOnMouseDownObservable = new Subject<Vector3>();

        public readonly Subject<Vector3> WaveformRegionOnMouseDownObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> WaveformRegionOnMouseExitObservable = new Subject<Vector3>();
        public readonly Subject<Vector3> WaveformRegionOnMouseEnterObservable = new Subject<Vector3>();

        public readonly Subject<float> MouseScrollWheelObservable = new Subject<float>();

        void Awake()
        {
            //===============================
            // マウスホイール
            //===============================
            this.UpdateAsObservable()
                .Select(_ => Input.GetAxis("Mouse ScrollWheel"))
                .Where(delta => delta != 0)
                .Subscribe(MouseScrollWheelObservable.OnNext);

            //===============================
            // Notes 領域のマウスオーバー状態
            //===============================
            NotesRegionOnMouseExitObservable.Select(_ => false)
                .Merge(NotesRegionOnMouseEnterObservable.Select(_ => true))
                .Subscribe(isMouseOver =>
                    NoteCanvas.IsMouseOverNotesRegion.Value = isMouseOver);

            //===============================
            // Waveform 領域のマウスオーバー状態
            //===============================
            WaveformRegionOnMouseExitObservable.Select(_ => false)
                .Merge(WaveformRegionOnMouseEnterObservable.Select(_ => true))
                .Subscribe(isMouseOver =>
                    NoteCanvas.IsMouseOverWaveformRegion.Value = isMouseOver);
        }

        //===============================
        // Notes 領域イベント
        //===============================
        public void NotesRegionOnMouseUp() { NotesRegionOnMouseUpObservable.OnNext(Input.mousePosition); }
        public void NotesRegionOnMouseExit() { NotesRegionOnMouseExitObservable.OnNext(Input.mousePosition); }
        public void NotesRegionOnMouseDown() { NotesRegionOnMouseDownObservable.OnNext(Input.mousePosition); }
        public void NotesRegionOnMouseEnter() { NotesRegionOnMouseEnterObservable.OnNext(Input.mousePosition); }

        //===============================
        // 縦ラインクリック
        //===============================
        public void VerticalLineOnMouseDown() { VerticalLineOnMouseDownObservable.OnNext(Input.mousePosition); }

        //===============================
        // Waveform 領域イベント
        //===============================
        public void WaveformRegionOnMouseDown() { WaveformRegionOnMouseDownObservable.OnNext(Input.mousePosition); }
        public void WaveformRegionOnMouseExit() { WaveformRegionOnMouseExitObservable.OnNext(Input.mousePosition); }
        public void WaveformRegionOnMouseEnter() { WaveformRegionOnMouseEnterObservable.OnNext(Input.mousePosition); }
    }
}
