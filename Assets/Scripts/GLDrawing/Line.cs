// ========================================
//
// Line.cs
//
// ========================================
//
// GL 描画用のライン（開始点・終了点・色）を表す構造体
//
// ========================================

using UnityEngine;

namespace NoteMaker.GLDrawing
{
    public struct Line
    {
        public Color color;     // ラインの色
        public Vector3 start;   // 開始座標
        public Vector3 end;     // 終了座標

        /// <summary>
        /// 開始点・終了点・色を指定してラインを生成する。
        /// </summary>
        public Line(Vector3 start, Vector3 end, Color color)
        {
            this.color = color;
            this.start = start;
            this.end = end;
        }
    }
}
