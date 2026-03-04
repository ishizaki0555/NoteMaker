// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// GridLineRenderer.cs
// ノートエディタ画面に表示するグリッド線（拍線・縦線）を生成し、
// マウス位置に応じたハイライト処理や BeatNumber の描画を行います。
// 音声再生位置・キャンバスサイズ・LPB などの変化に応じて
// 毎フレーム動的にラインを更新します。
// 
//========================================

using NoteMaker.Notes;
using NoteMaker.Model;
using NoteMaker.Utility;
using System;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.GLDrawing
{
    /// <summary>
    /// ノートエディタの背景に表示するグリッド線（拍線・縦線）を描画するクラスです。
    /// 音声の再生位置やキャンバスの高さに応じてライン位置を更新し、
    /// マウス位置に応じたハイライト処理も行います。
    /// </summary>
    public class GridLineRenderer : MonoBehaviour
    {
        [SerializeField] Color highlightColor = default;   // マウス付近のラインを強調表示する色
        [SerializeField] Color blockLineColor = default;   // 縦線の基本色
        [SerializeField] Color beatLineColor1 = default;   // 拍線（細線）
        [SerializeField] Color beatLineColor2 = default;   // 拍線（中線）
        [SerializeField] Color beatLineColor3 = default;   // 拍線（太線）

        [SerializeField] float horizontalOffset = 0f;     // グリッド全体を横方向にずらす量
        [SerializeField] float lineWidthFactor = 1f;      // 横線の長さ倍率
        [SerializeField] float blockSpacingFactor = 1f;   // 縦線の間隔倍率

        // 拍番号に応じて線の色を返す
        Color BeatLineColor(int beat) =>
            beat % (EditData.LPB.Value * 4) == 0 ? beatLineColor3 :
            beat % EditData.LPB.Value == 0 ? beatLineColor2 :
            beatLineColor1;

        // 拍番号に応じて線の長さ倍率を返す
        float BeatLineLengthFactor(int beat) =>
            beat % (EditData.LPB.Value * 4) == 0 ? 1.0f :
            beat % EditData.LPB.Value == 0 ? 1.05f :
            1.0f;

        /// <summary>
        /// グリッド線の生成・更新・描画処理をセットアップします。
        /// 音声の再生状態やキャンバスサイズの変化に応じて毎フレーム更新されます。
        /// </summary>
        void Awake()
        {
            var beatSamples = new int[1];   // 各拍のサンプル位置
            var beatLines = new Line[1];    // 横線（拍線）
            var blockLines = new Line[1];   // 縦線（ブロック線）

            float cachedZeroSamplePosY = -1f; // 前フレームの基準位置
            float cachedCanvasHeight = 0f;    // 前フレームのキャンバス高さ

            this.LateUpdateAsObservable()
                .Where(_ => Audio.Source != null && Audio.Source.clip != null)
                .Subscribe(_ =>
                {
                    // 1 拍あたりのサンプル数を計算
                    var unitBeatSamples = Mathf.FloorToInt(Audio.Source.clip.frequency * 60f / EditData.BPM.Value);

                    // 総拍数を算出
                    var beatNum = EditData.LPB.Value * Mathf.CeilToInt(Audio.Source.clip.samples / (float)unitBeatSamples);

                    // ============================
                    // BeatLine（横線）生成
                    // ============================
                    if (beatSamples.Length != beatNum || cachedCanvasHeight != NoteCanvas.Height.Value)
                    {
                        // 各拍のサンプル位置を計算
                        beatSamples = Enumerable.Range(0, beatNum)
                            .Select(i => i * unitBeatSamples / EditData.LPB.Value)
                            .ToArray();

                        // サンプル位置を Y 座標に変換し、Line を生成
                        beatLines = beatSamples
                            .Select(s => ConvertUtils.SamplesToCanvasPositionY(s))
                            .Select((y, i) =>
                            {
                                float len = 140 * BeatLineLengthFactor(i);
                                float halfWidth = len * lineWidthFactor;

                                return new Line(
                                    ConvertUtils.CanvasToScreenPosition(new Vector3(halfWidth + horizontalOffset, y, 0)),
                                    ConvertUtils.CanvasToScreenPosition(new Vector3(-halfWidth + horizontalOffset, y, 0)),
                                    BeatLineColor(i)
                                );
                            })
                            .ToArray();

                        cachedZeroSamplePosY = beatLines[0].start.y;
                        cachedCanvasHeight = NoteCanvas.Height.Value;
                    }
                    else
                    {
                        // キャンバスのスクロールに合わせて Y 座標を更新
                        float currentY = ConvertUtils.CanvasToScreenPosition(Vector3.up * ConvertUtils.SamplesToCanvasPositionY(0)).y;
                        float diffY = currentY - cachedZeroSamplePosY;

                        // Y座標を更新し、色も再設定
                        for (int i = 0; i < beatNum; i++)
                        {
                            beatLines[i].end.y = (beatLines[i].start.y += diffY);
                            beatLines[i].color = BeatLineColor(i);
                        }

                        cachedZeroSamplePosY = currentY;
                    }

                    // ============================
                    // BlockLine（縦線）生成
                    // ============================
                    if (blockLines.Length != EditData.MaxBlock.Value)
                    {
                        // ブロック番号をX座標に変換し、Lineを生成
                        blockLines = Enumerable.Range(0, EditData.MaxBlock.Value)
                           .Select(i => ConvertUtils.BlockNumToCanvasPositionX(i) * blockSpacingFactor + horizontalOffset)
                           .Select(canvasX => ConvertUtils.CanvasToScreenPosition(new Vector3(canvasX, 0, 0)).x)
                           .Select((x, i) => new Line(
                               new Vector3(x, 0, 0),
                               new Vector3(x, Screen.height, 0),
                               blockLineColor))
                           .ToArray();
                    }
                    else
                    {
                        // 色だけ更新
                        for (int i = 0; i < EditData.MaxBlock.Value; i++)
                            blockLines[i].color = blockLineColor;
                    }

                    // ============================
                    // ハイライト判定
                    // ============================
                    if (NoteCanvas.IsMouseOverNotesRegion.Value)
                    {
                        // マウス位置に最も近い拍線と縦線を見つける
                        var mouseY = Input.mousePosition.y;
                        var closestBeatIndex = GetClosestLineIndex(beatLines, c => Mathf.Abs(c.start.y - mouseY));

                        // 縦線はマウスのX位置に基づいて最も近いものを選択
                        var mouseX = Input.mousePosition.x;
                        var closestBlockIndex = GetClosestLineIndex(blockLines, c => Mathf.Abs(c.start.x - mouseX));

                        // マウス位置に最も近い拍線と縦線の距離を計算
                        var distance = new Vector2(blockLines[closestBlockIndex].start.x, beatLines[closestBeatIndex].start.y)
                                     - new Vector2(mouseX, mouseY);

                        // 距離の閾値を拍線と縦線の間隔に基づいて設定
                        var thresholdY = Mathf.Abs(ConvertUtils.SamplesToCanvasPositionY(beatSamples[0]) -
                                                   ConvertUtils.SamplesToCanvasPositionY(beatSamples[1])) / 2f;

                        // 縦線の間隔に基づいて閾値を設定
                        var thresholdX = Mathf.Abs(ConvertUtils.BlockNumToCanvasPositionX(0) -
                                                   ConvertUtils.BlockNumToCanvasPositionX(1)) / 2f;

                        // マウスが近いラインをハイライト
                        if (distance.x < thresholdX && distance.y < thresholdY)
                        {
                            blockLines[closestBlockIndex].color = highlightColor;
                            beatLines[closestBeatIndex].color = highlightColor;

                            NoteCanvas.ClosestNotePosition.Value =
                                new NotePosition(EditData.LPB.Value, closestBeatIndex, closestBlockIndex);
                        }
                        else
                        {
                            NoteCanvas.ClosestNotePosition.Value = NotePosition.None;
                        }
                    }

                    // 拍番号の間引き（近すぎる場合は間隔を広げる）
                    var beatGridInterval = beatLines[EditData.LPB.Value * 4].start.y - beatLines[0].start.y;
                    var beatGridMinInterval = 130;
                    var intervalFactor = beatGridInterval < beatGridMinInterval
                        ? Mathf.RoundToInt(beatGridMinInterval / beatGridInterval)
                        : 1;

                    // ============================
                    // 描画
                    // ============================
                    BeatNumberRenderer.Begin();

                    var screenHeight = Screen.height;

                    // 画面内にある拍線のを描画し、拍番号も表示
                    for (int i = 0, l = beatLines.Length; i < l && beatLines[i].start.y < screenHeight; i++)
                    {
                        // 画面内にある拍線のみ描画
                        if (beatLines[i].start.y > 0)
                        {
                            GLLineDrawer.Draw(beatLines[i]);

                            // 拍番号の描画（中央固定）
                            if (i % (EditData.LPB.Value * 4 * intervalFactor) == 0)
                            {
                                BeatNumberRenderer.Render(
                                    new Vector3(
                                        Screen.width / 2f + 100 / NoteCanvas.ScaleFactor.Value - 100,
                                        beatLines[i].start.y,
                                        0),
                                    i / (EditData.LPB.Value * 4));
                            }
                        }
                    }

                    // 画面全体の縦線を描画
                    BeatNumberRenderer.End();

                    GLLineDrawer.Draw(blockLines);
                });
        }

        /// <summary>
        /// 指定した Line 配列の中から、距離計算関数に基づいて最も近い Line のインデックスを返します。
        /// </summary>
        int GetClosestLineIndex(Line[] lines, Func<Line, float> calcDistance)
        {
            var minValue = lines.Min(calcDistance);
            return Array.FindIndex(lines, c => calcDistance(c) == minValue);
        }
    }
}
