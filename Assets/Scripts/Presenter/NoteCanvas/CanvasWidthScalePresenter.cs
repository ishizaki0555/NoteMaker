// ========================================
//
// NoteMaker Project
//
// ========================================
//
// CanvasWidthScalePresenter.cs
// ノーツキャンバスの「縦方向の拡大率（Height）」を、
// ・Ctrl + ホイール
// ・↑ / ↓ キー
// ・スライダー操作
// から変更できるようにするプレゼンターです。
// Undo / Redo にも対応し、操作の連続性を保ちながら Height を更新します。
//
//========================================

using NoteMaker.Common;
using NoteMaker.Model;
using NoteMaker.Utility;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// キャンバスの縦方向スケール（NoteCanvas.Height）を管理するクラスです。
    /// ・Ctrl + ホイールで拡大縮小  
    /// ・↑ / ↓ キーで微調整  
    /// ・スライダーで直接変更  
    /// ・Undo / Redo 対応  
    /// 
    /// Height は「100 サンプルあたりのピクセル量」として扱われます。
    /// </summary>
    public class CanvasWidthScalePresenter : MonoBehaviour
    {
        [SerializeField] CanvasEvents canvasEvents = default;          // マウスイベント
        [SerializeField] Slider canvasWidthScaleController = default;  // スケール調整スライダー

        void Awake()
        {
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// 入力ストリームの構築と Undo/Redo の設定。
        /// </summary>
        void Init()
        {
            //===============================
            // Ctrl + ホイール / ↑ / ↓ キー
            //===============================
            var operateCanvasScaleObservable =
                canvasEvents.MouseScrollWheelObservable
                    .Where(_ => KeyInput.CtrlKey()) // Ctrl + ホイール
                .Merge(
                    this.UpdateAsObservable()
                        .Where(_ => Input.GetKey(KeyCode.UpArrow))
                        .Select(_ => 0.05f))         // ↑ キー
                .Merge(
                    this.UpdateAsObservable()
                        .Where(_ => Input.GetKey(KeyCode.DownArrow))
                        .Select(_ => -0.05f))        // ↓ キー
                .Select(delta => NoteCanvas.Height.Value * (1 + delta)) // 現在値に倍率を掛ける
                .Select(x => x / (Audio.Source.clip.samples / 100f))    // 100 サンプルあたりの値に変換
                .Select(x => Mathf.Clamp(x, 0.1f, 2f))                  // 最小・最大制限
                .Merge(
                    canvasWidthScaleController
                        .OnValueChangedAsObservable()
                        .DistinctUntilChanged())                        // スライダー操作
                .DistinctUntilChanged()
                .Select(x => Audio.Source.clip.samples / 100f * x);     // Height に戻す

            // Height 更新
            operateCanvasScaleObservable
                .Subscribe(x => NoteCanvas.Height.Value = x);

            //===============================
            // Undo / Redo 対応
            //===============================
            operateCanvasScaleObservable
                .Buffer(operateCanvasScaleObservable.ThrottleFrame(2))
                .Where(b => b.Count >= 2)
                .Select(b => new { current = b.Last(), prev = b.First() })
                .Subscribe(x =>
                    EditCommandManager.Do(
                        new Command(
                            () => NoteCanvas.Height.Value = x.current,
                            () => NoteCanvas.Height.Value = x.prev)));
        }
    }
}
