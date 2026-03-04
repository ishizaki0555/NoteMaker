// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// GLQuadDrawer.cs
// GL を用いて四角形（Quad）を描画するためのレンダラーを提供します。
// Geometry データをオブジェクトプール方式で管理し、
// 毎フレームの描画負荷を抑えつつ効率的に Quad を描画します。
// 
//========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace NoteMaker.GLDrawing
{
    /// <summary>
    /// GL を使用して四角形（Quad）を描画するためのクラスです。
    /// Geometry データをプールし、毎フレームの生成コストを抑えながら
    /// 必要な Quad を描画します。
    /// 
    /// 描画は Unity の OnRenderObject() タイミングで行われます。
    /// </summary>
    public class GLQuadDrawer : SingletonMonoBehaviour<GLQuadDrawer>
    {
        [SerializeField] Material mat = default; // Quad 描画に使用するマテリアル

        List<Geometry> drawData = new List<Geometry>(); // 描画対象の Geometry データを保持するプール

        static int size = 0;     // 今フレームで描画する Quad 数
        static int maxSize = 0;  // プールされている Quad の最大数

        /// <summary>
        /// Unity の描画イベントで呼ばれ、登録された Quad をすべて描画します。
        /// 描画後、今フレームの使用数をリセットします。
        /// </summary>
        void OnRenderObject()
        {
            GL.PushMatrix();
            mat.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Begin(GL.QUADS);

            // プールが過剰に大きい場合は縮小する
            if (size * 2 < maxSize)
            {
                drawData.RemoveRange(size, maxSize - size);
                maxSize = size;
            }

            // 今フレームの Quad を順に描画
            for (int i = 0; i < size; i++)
            {
                GL.Color(drawData[i].color);

                // Geometry に含まれる頂点を順に描画
                foreach (var v in drawData[i].vertices)
                    GL.Vertex(v);
            }

            GL.End();
            GL.PopMatrix();

            // 次フレームに備えて使用数をリセット
            size = 0;
        }

        /// <summary>
        /// 複数の Quad を描画キューに追加します。
        /// </summary>
        /// <param name="quads">描画する Geometry 配列。</param>
        public static void Draw(Geometry[] quads)
        {
            foreach (var q in quads)
                Draw(q);
        }

        /// <summary>
        /// 単一の Quad を描画キューに追加します。
        /// プールに空きがあれば再利用し、なければ新規追加します。
        /// </summary>
        /// <param name="quad">描画する Geometry。</param>
        public static void Draw(Geometry quad)
        {
            if (size < maxSize)
            {
                // 既存のプールを再利用
                Instance.drawData[size] = quad;
            }
            else
            {
                // 新規追加
                Instance.drawData.Add(quad);
                maxSize++;
            }

            size++;
        }
    }
}
