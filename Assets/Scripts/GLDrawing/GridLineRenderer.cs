
using NoteMaker.Notes;
using NoteMaker.Model;
using NoteMaker.Utility;
using System;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using System.Xml.Serialization;

namespace NoteMaker.GLDrawing
{
    public class GridLineRenderer : MonoBehaviour
    {
        [SerializeField] Color highlightColor = default;
        [SerializeField] Color blockLineColor = default;
        [SerializeField] Color beatLineColor1 = default;
        [SerializeField] Color beatLineColor2 = default;
        [SerializeField] Color beatLineColor3 = default;

        [SerializeField] float horizontalOffset = 0f;     // 縦線・横線を横にずらす
        [SerializeField] float lineWidthFactor = 1f;      // 横線の横幅を広げる
        [SerializeField] float blockSpacingFactor = 1f;   // 縦線の間隔を広げる

        Color BeatLineColor(int beat) =>
            beat % (EditData.LPB.Value * 4) == 0 ? beatLineColor3 :
            beat % EditData.LPB.Value == 0 ? beatLineColor2 :
            beatLineColor1;

        float BeatLineLengthFactor(int beat) =>
            beat % (EditData.LPB.Value * 4) == 0 ? 1.0f :
            beat % EditData.LPB.Value == 0 ? 1.05f :
            1.0f;

        void Awake()
        {
            var beatSamples = new int[1];
            var beatLines = new Line[1];
            var blockLines = new Line[1];
            float cachedZeroSamplePosY = -1f;
            float cachedCanvasHeight = 0f;

            this.LateUpdateAsObservable()
                .Where(_ => Audio.Source != null && Audio.Source.clip != null)
                .Subscribe(_ =>
                {
                    var unitBeatSamples = Mathf.FloorToInt(Audio.Source.clip.frequency * 60f / EditData.BPM.Value);
                    var beatNum = EditData.LPB.Value * Mathf.CeilToInt(Audio.Source.clip.samples / (float)unitBeatSamples);

                    // ============================
                    // ★ BeatLine（横線）生成
                    // ============================
                    if (beatSamples.Length != beatNum || cachedCanvasHeight != NoteCanvas.Height.Value)
                    {
                        beatSamples = Enumerable.Range(0, beatNum)
                            .Select(i => i * unitBeatSamples / EditData.LPB.Value)
                            .ToArray();

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
                        float currentY = ConvertUtils.CanvasToScreenPosition(Vector3.up * ConvertUtils.SamplesToCanvasPositionY(0)).y;
                        float diffY = currentY - cachedZeroSamplePosY;

                        for (int i = 0; i < beatNum; i++)
                        {
                            beatLines[i].end.y = (beatLines[i].start.y += diffY);
                            beatLines[i].color = BeatLineColor(i);
                        }

                        cachedZeroSamplePosY = currentY;
                    }

                    // ============================
                    // ★ BlockLine（縦線）生成
                    // ============================
                    if (blockLines.Length != EditData.MaxBlock.Value)
                    {
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
                        for (int i = 0; i < EditData.MaxBlock.Value; i++)
                            blockLines[i].color = blockLineColor;
                    }

                    // ============================
                    // ★ ハイライト判定
                    // ============================
                    if (NoteCanvas.IsMouseOverNotesRegion.Value)
                    {
                        var mouseY = Input.mousePosition.y;
                        var closestBeatIndex = GetClosestLineIndex(beatLines, c => Mathf.Abs(c.start.y - mouseY));

                        var mouseX = Input.mousePosition.x;
                        var closestBlockIndex = GetClosestLineIndex(blockLines, c => Mathf.Abs(c.start.x - mouseX));

                        var distance = new Vector2(blockLines[closestBlockIndex].start.x, beatLines[closestBeatIndex].start.y)
                                     - new Vector2(mouseX, mouseY);

                        var thresholdY = Mathf.Abs(ConvertUtils.SamplesToCanvasPositionY(beatSamples[0]) -
                                                   ConvertUtils.SamplesToCanvasPositionY(beatSamples[1])) / 2f;

                        var thresholdX = Mathf.Abs(ConvertUtils.BlockNumToCanvasPositionX(0) -
                                                   ConvertUtils.BlockNumToCanvasPositionX(1)) / 2f;

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

                    var beatGridInterval = beatLines[EditData.LPB.Value * 4].start.y - beatLines[0].start.y;
                    var beatGridMinInterval = 130;
                    var intervalFactor = beatGridInterval < beatGridMinInterval
                        ? Mathf.RoundToInt(beatGridMinInterval / beatGridInterval)
                        : 1;

                    // ============================
                    // ★ 描画
                    // ============================
                    BeatNumberRenderer.Begin();

                    var screenHeight = Screen.height;

                    for (int i = 0, l = beatLines.Length; i < l && beatLines[i].start.y < screenHeight; i++)
                    {
                        if (beatLines[i].start.y > 0)
                        {
                            GLLineDrawer.Draw(beatLines[i]);

                            // ★ 番号は horizontalOffset を適用しない（中央固定）
                            if (i % (EditData.LPB.Value * 4 * intervalFactor) == 0)
                            {
                                BeatNumberRenderer.Render(
                                    new Vector3(
                                        Screen.width / 2f + 100 / NoteCanvas.ScaleFactor.Value,
                                        beatLines[i].start.y,
                                        0),
                                    i / (EditData.LPB.Value * 4));
                            }
                        }
                    }

                    BeatNumberRenderer.End();

                    GLLineDrawer.Draw(blockLines);
                });
        }

        int GetClosestLineIndex(Line[] lines, Func<Line, float> calcDistance)
        {
            var minValue = lines.Min(calcDistance);
            return Array.FindIndex(lines, c => calcDistance(c) == minValue);
        }
    }
}
