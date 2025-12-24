// ========================================
//
// Geometry.cs
//
// ========================================
//
// GL 描画用の色と頂点情報を保持するクラス
//
// ========================================

using UnityEngine;

namespace NoteMaker.GLDrawing
{
    public class Geometry
    {
        public Color color;          // 描画色
        public Vector3[] vertices;   // 頂点配列

        /// <summary>
        /// 頂点配列と色を指定して Geometry を生成する。
        /// </summary>
        public Geometry(Vector3[] vertices, Color color)
        {
            this.vertices = vertices;
            this.color = color;
        }
    }
}
