// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// Line.cs
// GL 描画で使用する 1 本の線分（始点・終点・色）を保持する
// 軽量なデータ構造を提供します。
// 
//========================================

using UnityEngine;

namespace NoteMaker.GLDrawing
{
    /// <summary>
    /// GL 描画で使用する線分データを保持する構造体です。
    /// 始点・終点・色の 3 要素のみを持つ軽量なデータで、
    /// GLLineDrawer などの描画クラスに渡して使用します。
    /// </summary>
    public struct Line
    {
        public Color color;      // 線の描画色
        public Vector3 start;    // 線の始点座標
        public Vector3 end;      // 線の終点座標

        /// <summary>
        /// Line の新しいインスタンスを生成します。
        /// </summary>
        /// <param name="start">線の始点座標。</param>
        /// <param name="end">線の終点座標。</param>
        /// <param name="color">線の描画色。</param>
        public Line(Vector3 start, Vector3 end, Color color)
        {
            this.color = color;
            this.start = start;
            this.end = end;
        }
    }
}
