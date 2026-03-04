// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// NoteRenderer.cs
// ノートオブジェクトをキャンバス座標から画面座標へ変換し、
// GLQuadDrawer を用いて菱形ノーツを描画します。
// 画面内に存在するノーツのみを処理し、Long ノーツの prev 更新も行います。
// 
//========================================

using NoteMaker.Model;
using NoteMaker.Utility;
using UniRx;
using UnityEngine;

namespace NoteMaker.GLDrawing
{
    /// <summary>
    /// ノートオブジェクトを描画するクラスです。
    /// キャンバス座標 → 画面座標への変換、画面内判定、描画サイズ計算、
    /// 菱形ノーツの描画、Long ノーツの prev 更新などを行います。
    /// </summary>
    public class NoteRenderer : MonoBehaviour
    {
        /// <summary>
        /// 毎フレームの LateUpdate でノーツ描画処理を行います。
        /// 画面内に存在するノーツのみを描画し、必要に応じて Long ノーツの prev も更新します。
        /// </summary>
        void LateUpdate()
        {
            if (Audio.Source.clip == null)
                return;

            foreach (var noteObj in EditData.Notes.Values)
            {
                // Canvas 座標を取得
                var canvasPos = ConvertUtils.NoteToCanvasPosition(noteObj.note.position);

                // 画面内判定
                float minY = ConvertUtils.ScreenToCanvasPosition(Vector3.zero).y;
                float maxY = ConvertUtils.ScreenToCanvasPosition(Vector3.up * Screen.height).y * 1.1f;

                if (minY <= canvasPos.y && canvasPos.y <= maxY)
                {
                    // ノーツ固有の LateUpdate を呼ぶ（横版と同じ構造）
                    noteObj.LateUpdateObservable.OnNext(Unit.Default);

                    // 画面座標へ変換
                    var screenPos = ConvertUtils.CanvasToScreenPosition(canvasPos);

                    // 描画サイズ
                    float drawSize = 9f / NoteCanvas.ScaleFactor.Value;

                    // 菱形ノーツを描画
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

                    // Long ノーツの prev 更新
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
