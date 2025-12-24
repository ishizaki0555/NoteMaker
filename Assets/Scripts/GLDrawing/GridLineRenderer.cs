// ========================================
//
// GridLineRenderer.cs
//
// ========================================
//
// 拍線・ブロック線・ハイライトなど、譜面グリッドの描画を行うクラス
//
// ========================================

using NoteMaker.Model;
using NoteMaker.Notes;
using NoteMaker.Utility;
using System;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.GLDrawing
{
    public class GridLineRenderer : MonoBehaviour
    {
        [SerializeField] Color highlightColor = default;   // マウス付近のラインを強調表示する色
        [SerializeField] Color blockLineColor = default;   // ブロック線の色
        [SerializeField] Color beatLineColor1 = default;   // 通常の拍線
        [SerializeField] Color beatLineColor2 = default;   // 小節線
        [SerializeField] Color beatLineColor3 = default;   // 大小節線

        /// <summary>
        /// 拍番号に応じてラインの色を返す。
        /// </summary>
        Color BeatLineColor(int beat) =>
            beat % (EditData.LPB.Value * 4) == 0 ?
                beatLineColor3 :
            beat % EditData.LPB.Value == 0 ?
                beatLineColor2 :
                beatLineColor1;

        /// <summary>
        /// 拍番号に応じてラインの長さ倍率を返す。
        /// </summary>
        float BeatLineLengthFactor(int beat) =>
            beat % (EditData.LPB.Value * 4) == 0 ?
                1.0f :
            beat % EditData.LPB.Value == 0 ?
                1.05f :
                1.0f;

        void Awake()
        {
            var beatSamples = new int[1];
            var beatLines = new Line[1];
            var blockLines = new Line[1];
            var cachedZeroSamplePosX = -1f;
            var cachedCanvasWidth = 0f;

            this.LateUpdateAsObservable()
                .Where(_ => Audio.Source != null && Audio.Source.clip != null)
                .Subscribe(_ =>
                {
                    // 1 拍あたりのサンプル数を計算
                    var unitBeatSamples = Mathf.FloorToInt(Audio.Source.clip.frequency * 60f / EditData.BPM.Value);

                    // 全拍数を計算
                    var beatNum = EditData.LPB.Value * Mathf.CeilToInt(Audio.Source.clip.samples / (float)unitBeatSamples);

                    // 拍数が変わった or キャンバス幅が変わった場合はラインを再生成
                    if (beatSamples.Length != beatNum || cachedCanvasWidth != NoteCanvas.Width.Value)
                    {
                        beatSamples = Enumerable.Range(0, beatNum)
                            .Select(i => i * unitBeatSamples / EditData.LPB.Value)
                            .ToArray();

                        beatLines = beatSamples
                            .Select(x => ConvertUtils.SamplesToCanvasPositionX(x))
                            .Select((x, i) => new Line(
                                ConvertUtils.CanvasToScreenPosition(new Vector3(x, 140 * BeatLineLengthFactor(i), 0)),
                                ConvertUtils.CanvasToScreenPosition(new Vector3(x, -140 * BeatLineLengthFactor(i), 0)),
                                BeatLineColor(i)))
                            .ToArray();

                        cachedZeroSamplePosX = beatLines[0].start.x;
                        cachedCanvasWidth = NoteCanvas.Width.Value;
                    }
                    else
                    {
                        // 位置の更新だけで済む場合は差分移動
                        float currentX = ConvertUtils.CanvasToScreenPosition(Vector3.right * ConvertUtils.SamplesToCanvasPositionX(0)).x;
                        float diffX = currentX - cachedZeroSamplePosX;

                        // すべての拍線を差分移動
                        for (int i = 0; i < beatNum; i++)
                        {
                            beatLines[i].end.x = (beatLines[i].start.x += diffX);
                            beatLines[i].color = BeatLineColor(i);
                        }

                        cachedZeroSamplePosX = currentX;
                    }

                    // ブロック線の数が変わった場合は再生成
                    if (blockLines.Length != EditData.MaxBlock.Value)
                    {
                        blockLines = Enumerable.Range(0, EditData.MaxBlock.Value)
                            .Select(i => ConvertUtils.BlockNumToCanvasPositionY(i))
                            .Select(i => i + Screen.height * 0.5f)
                            .Select((y, i) => new Line(
                                new Vector3(0, y, 0),
                                new Vector3(Screen.width, y, 0),
                                blockLineColor))
                            .ToArray();
                    }
                    else
                    {
                        // 色だけ更新
                        for (int i = 0; i < EditData.MaxBlock.Value; i++)
                        {
                            blockLines[i].color = blockLineColor;
                        }
                    }

                    // マウス付近のラインをハイライト
                    if (NoteCanvas.IsMouseOverNotesRegion.Value)
                    {
                        var mouseX = Input.mousePosition.x;
                        var closestLineIndex = GetClosestLineIndex(beatLines, c => Mathf.Abs(c.start.x - mouseX));

                        var mouseY = Input.mousePosition.y;
                        var closestBlockIndex = GetClosestLineIndex(blockLines, c => Mathf.Abs(c.start.y - mouseY));

                        var distance = new Vector2(beatLines[closestLineIndex].start.x, blockLines[closestBlockIndex].start.y)
                                     - new Vector2(mouseX, mouseY);

                        var thresholdX = Mathf.Abs(ConvertUtils.SamplesToCanvasPositionX(beatSamples[0]) - ConvertUtils.SamplesToCanvasPositionX(beatSamples[1])) / 2f;
                        var thresholdY = Mathf.Abs(ConvertUtils.BlockNumToCanvasPositionY(0) - ConvertUtils.BlockNumToCanvasPositionY(1)) / 2f;

                        // マウスがラインに近い場合はハイライト
                        if (distance.x < thresholdX && distance.y < thresholdY)
                        {
                            blockLines[closestBlockIndex].color = highlightColor;
                            beatLines[closestLineIndex].color = highlightColor;

                            NoteCanvas.ClosestNotePosition.Value =
                                new NotePosition(EditData.LPB.Value, closestLineIndex, closestBlockIndex);
                        }
                        else
                        {
                            NoteCanvas.ClosestNotePosition.Value = NotePosition.None;
                        }
                    }

                    // 拍番号の描画間隔を調整
                    var beatGridInterval = beatLines[EditData.LPB.Value * 4].start.x - beatLines[0].start.x;
                    var beatGridMinInterval = 100;
                    var intervalFactor = beatGridInterval < beatGridMinInterval
                        ? Mathf.RoundToInt(beatGridMinInterval / beatGridInterval)
                        : 1;

                    // 拍番号の描画
                    BeatNumberRenderer.Begin();
                    var screenWidth = Screen.width;

                    for (int i = 0, l = beatLines.Length; i < l && beatLines[i].start.x < screenWidth; i++)
                    {
                        if (beatLines[i].start.x > 0)
                        {
                            GLLineDrawer.Draw(beatLines[i]);

                            // 拍番号を描画するタイミング
                            if (i % (EditData.LPB.Value * 4 * intervalFactor) == 0)
                            {
                                BeatNumberRenderer.Render(
                                    new Vector3(
                                        beatLines[i].start.x,
                                        Screen.height / 2f + 154 / NoteCanvas.ScaleFactor.Value,
                                        0),
                                    i / (EditData.LPB.Value * 4));
                            }
                        }
                    }
                    BeatNumberRenderer.End();

                    // ブロック線を描画
                    GLLineDrawer.Draw(blockLines);
                });
        }

        /// <summary>
        /// 最も近いラインのインデックスを返す。
        /// </summary>
        int GetClosestLineIndex(Line[] lines, Func<Line, float> calcDistance)
        {
            var minValue = lines.Min(calcDistance);
            return Array.FindIndex(lines, c => calcDistance(c) == minValue);
        }
    }
}
