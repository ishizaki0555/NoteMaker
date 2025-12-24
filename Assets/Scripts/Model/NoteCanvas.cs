// ========================================
//
// NoteCanvas.cs
//
// ========================================
//
// ノートエディタのキャンバス状態（幅・オフセット・スケール・マウス位置など）
// を保持するシングルトン
//
// ========================================

using NoteMaker.Notes;
using NoteMaker.Utility;
using UniRx;
using UnityEngine;

namespace NoteMaker.Model
{
    public class NoteCanvas : SingletonMonoBehaviour<NoteCanvas>
    {
        ReactiveProperty<float> width_ = new ReactiveProperty<float>();                             // キャンバスの幅
        ReactiveProperty<float> offsetX_ = new ReactiveProperty<float>();                           // キャンバスの X オフセット
        ReactiveProperty<float> scaleFactor_ = new ReactiveProperty<float>();                       // 描画スケール
        ReactiveProperty<bool> isMouseOverNotesRegion_ = new ReactiveProperty<bool>();              // マウスがノート領域上にあるか
        ReactiveProperty<bool> isMouseOverWaveformRegion_ = new ReactiveProperty<bool>();           // マウスが波形領域上にあるか
        ReactiveProperty<NotePosition> closestNotePosition_ = new ReactiveProperty<NotePosition>(); // マウスに最も近いノート位置

        /// <summary>
        /// キャンバスの幅
        /// </summary>
        public static ReactiveProperty<float> Width
        {
            get { return Instance.width_; }
        }

        /// <summary>
        /// キャンバスの X オフセット
        /// </summary>
        public static ReactiveProperty<float> OffsetX
        {
            get { return Instance.offsetX_; }
        }

        /// <summary>
        /// 描画スケール
        /// </summary>
        public static ReactiveProperty<float> ScaleFactor
        {
            get { return Instance.scaleFactor_; }
        }

        /// <summary>
        /// マウスがノート領域上にあるかどうか
        /// </summary>
        public static ReactiveProperty<bool> IsMouseOverNotesRegion
        {
            get { return Instance.isMouseOverNotesRegion_; }
        }

        /// <summary>
        /// マウスが波形領域上にあるかどうか
        /// </summary>
        public static ReactiveProperty<bool> IsMouseOverWaveformRegion
        {
            get { return Instance.isMouseOverWaveformRegion_; }
        }

        /// <summary>
        /// マウスに最も近いノート位置
        /// </summary>
        public static ReactiveProperty<NotePosition> ClosestNotePosition
        {
            get { return Instance.closestNotePosition_; }
        }

        /// <summary>
        /// 画面幅の変化に応じてスケールを自動調整する。
        /// </summary>
        void Awake()
        {
            this.ObserveEveryValueChanged(_ => Screen.width)
                .DistinctUntilChanged()
                .Subscribe(w => ScaleFactor.Value = 1280f / w);

            // 以前のコード:
            // .Subscribe(w => NoteCanvas.ScaleFactor.Value = canvasScaler.referenceResolution.x / w);
        }
    }
}
