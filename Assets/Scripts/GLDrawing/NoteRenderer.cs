// ========================================
//
// NoteRenderer.cs
//
// ========================================
//
// ノートの描画処理を行うクラス。
// キャンバス座標 → 画面座標への変換を行い、GLQuadDrawer で描画する。
//
// ========================================

using NoteMaker.Model;
using NoteMaker.Utility;
using UniRx;
using UnityEngine;

namespace NoteMaker.GLDrawing
{
    public class NoteRenderer : MonoBehaviour
    {
        /// <summary>
        /// 毎フレーム、画面内にあるノートを描画する。
        /// </summary>
        void LateUpdate()
        {
            // 音声がロードされていない場合は描画しない
            if (Audio.Source.clip == null)
                return;

            // すべてのノートを走査
            foreach (var noteObj in EditData.Notes.Values)
            {
                var canvasPosOfNote = ConvertUtils.NoteToCanvasPosition(noteObj.note.position);

                // 画面左端と右端のキャンバス座標を取得
                var min = ConvertUtils.ScreenToCanvasPosition(Vector3.zero).x;
                var max = ConvertUtils.ScreenToCanvasPosition(Vector3.right * Screen.width).x * 1.1f;

                // ノートが画面内にある場合のみ描画
                if (min <= canvasPosOfNote.x && canvasPosOfNote.x <= max)
                {
                    // ノートの LateUpdateObservable を発火
                    noteObj.LateUpdateObservable.OnNext(Unit.Default);

                    // キャンバス座標 → 画面座標へ変換
                    var screenPos = ConvertUtils.CanvasToScreenPosition(canvasPosOfNote);

                    // ノートの描画サイズ
                    var drawSize = 9 / NoteCanvas.ScaleFactor.Value;

                    // ダイヤ型ノートを描画
                    GLQuadDrawer.Draw(new Geometry(
                        new[] {
                            new Vector3(screenPos.x, screenPos.y - drawSize, 0),
                            new Vector3(screenPos.x + drawSize, screenPos.y, 0),
                            new Vector3(screenPos.x, screenPos.y + drawSize, 0),
                            new Vector3(screenPos.x - drawSize, screenPos.y, 0)
                        },
                        noteObj.NoteColor));

                    // ロングノートの場合、前のノートも更新通知を送る
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
