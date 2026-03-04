// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// NoteCanvas.cs
// ノートエディタのキャンバス状態（高さ・幅・オフセット・スケールなど）と、
// マウス位置に関する情報（ノーツ領域・波形領域・最も近いノート位置）を
// ReactiveProperty として管理する中心的なクラスです。
// 
//========================================

using NoteMaker.Notes;
using NoteMaker.Utility;
using UniRx;
using UnityEngine;

namespace NoteMaker.Model
{
    /// <summary>
    /// ノートエディタのキャンバス状態を管理するクラスです。
    /// キャンバスの高さ・幅・スクロール位置・スケール倍率、
    /// マウスがどの領域にいるか、最も近いノート位置などを保持します。
    /// 
    /// ReactiveProperty により、UI や描画処理が自動的に更新される仕組みになっています。
    /// </summary>
    public class NoteCanvas : SingletonMonoBehaviour<NoteCanvas>
    {
        ReactiveProperty<float> height_ = new ReactiveProperty<float>();                 // キャンバスの高さ
        ReactiveProperty<float> width_ = new ReactiveProperty<float>();                  // キャンバスの幅
        ReactiveProperty<float> offsetY_ = new ReactiveProperty<float>();                // キャンバスの縦方向オフセット
        ReactiveProperty<float> scaleFactor_ = new ReactiveProperty<float>();            // キャンバスのスケール倍率
        ReactiveProperty<bool> isMouseOverNotesRegion_ = new ReactiveProperty<bool>();   // マウスがノーツ領域にいるか
        ReactiveProperty<bool> isMouseOverWaveformRegion_ = new ReactiveProperty<bool>(); // マウスが波形領域にいるか
        ReactiveProperty<NotePosition> closestNotePosition_ = new ReactiveProperty<NotePosition>(); // 最も近いノート位置

        /// <summary>キャンバスの高さ。</summary>
        public static ReactiveProperty<float> Height => Instance.height_;

        /// <summary>キャンバスの幅。</summary>
        public static ReactiveProperty<float> Width => Instance.width_;

        /// <summary>キャンバスの縦方向オフセット。</summary>
        public static ReactiveProperty<float> OffsetY => Instance.offsetY_;

        /// <summary>キャンバスのスケール倍率。</summary>
        public static ReactiveProperty<float> ScaleFactor => Instance.scaleFactor_;

        /// <summary>マウスがノーツ領域にいるかどうか。</summary>
        public static ReactiveProperty<bool> IsMouseOverNotesRegion => Instance.isMouseOverNotesRegion_;

        /// <summary>マウスが波形領域にいるかどうか。</summary>
        public static ReactiveProperty<bool> IsMouseOverWaveformRegion => Instance.isMouseOverWaveformRegion_;

        /// <summary>マウス位置に最も近いノートの位置。</summary>
        public static ReactiveProperty<NotePosition> ClosestNotePosition => Instance.closestNotePosition_;

        /// <summary>
        /// 画面高さの変化に応じて ScaleFactor を自動更新します。
        /// 720px を基準としたスケーリングを行います。
        /// </summary>
        void Awake()
        {
            this.ObserveEveryValueChanged(_ => Screen.height)
                .DistinctUntilChanged()
                .Subscribe(h => ScaleFactor.Value = 720f / h);
        }
    }
}
