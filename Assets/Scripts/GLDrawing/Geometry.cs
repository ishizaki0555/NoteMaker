// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// Geometry.cs
// GL 描画で使用する単一の図形データ（色と頂点情報）を保持する
// シンプルなデータコンテナクラスを提供します。
// 
//========================================

using UnityEngine;

namespace NoteMaker.GLDrawing
{
    /// <summary>
    /// GL 描画に使用する図形データを保持するクラスです。
    /// 色と頂点配列をセットで管理し、描画処理に渡すための
    /// 軽量なデータコンテナとして機能します。
    /// </summary>
    public class Geometry
    {
        public Color color;          // 図形の描画色
        public Vector3[] vertices;   // 図形を構成する頂点配列

        /// <summary>
        /// Geometry の新しいインスタンスを生成します。
        /// </summary>
        /// <param name="vertices">図形を構成する頂点配列。</param>
        /// <param name="color">図形の描画色。</param>
        public Geometry(Vector3[] vertices, Color color)
        {
            this.color = color;
            this.vertices = vertices;
        }
    }
}
