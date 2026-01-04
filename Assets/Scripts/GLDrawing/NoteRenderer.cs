using NoteMaker.Model;
using NoteMaker.Utility;
using UniRx;
using UnityEngine;

namespace NoteMaker.GLDrawing
{
    public class NoteRenderer : MonoBehaviour
    {
        void LateUpdate()
        {
            if (Audio.Source.clip == null)
                return;

            foreach (var noteObj in EditData.Notes.Values)
            {
                // ============================
                // ★ Canvas座標を取得
                // ============================
                var canvasPos = ConvertUtils.NoteToCanvasPosition(noteObj.note.position);

                // ============================
                // ★ 画面内判定（縦スクロール版）
                // ============================
                float minY = ConvertUtils.ScreenToCanvasPosition(Vector3.zero).y;
                float maxY = ConvertUtils.ScreenToCanvasPosition(Vector3.up * Screen.height).y * 1.1f;

                if (minY <= canvasPos.y && canvasPos.y <= maxY)
                {
                    // ノーツ固有の LateUpdate を呼ぶ（横版と同じ構造）
                    noteObj.LateUpdateObservable.OnNext(Unit.Default);

                    // ============================
                    // ★ 画面座標へ変換
                    // ============================
                    var screenPos = ConvertUtils.CanvasToScreenPosition(canvasPos);

                    // ============================
                    // ★ 描画サイズ（横版と同じ構造）
                    // ============================
                    float drawSize = 9f / NoteCanvas.ScaleFactor.Value;

                    // ============================
                    // ★ 菱形ノーツを描画（縦方向）
                    // ============================
                    GLQuadDrawer.Draw(new Geometry(
                        new[]
                        {
                            new Vector3(screenPos.x - drawSize, screenPos.y, 0),
                            new Vector3(screenPos.x, screenPos.y + drawSize, 0),
                            new Vector3(screenPos.x + drawSize, screenPos.y, 0),
                            new Vector3(screenPos.x, screenPos.y - drawSize, 0)
                        },
                        noteObj.NoteColor
                    ));

                    // ============================
                    // ★ Longノーツの prev 更新（横版と同じ構造）
                    // ============================
                    if (noteObj.note.type == Notes.NoteTypes.Long &&
                        EditData.Notes.ContainsKey(noteObj.note.prev))
                    {
                        EditData.Notes[noteObj.note.prev].LateUpdateObservable.OnNext(Unit.Default);
                    }
                }
            }
        }
    }
}
