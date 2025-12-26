// ========================================
//
// GLQuadDrawer.cs
//
// ========================================
//
// GL を使って四角形（QUADS）を描画するためのプール管理付きレンダラー
//
// ========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace NoteMaker.GLDrawing
{
    public class GLQuadDrawer : SingletonMonoBehaviour<GLQuadDrawer>
    {
        [SerializeField] Material mat = default;                    // 描画に使用するマテリアル

        List<Geometry> drawData = new List<Geometry>();             // 描画用 Geometry のプール

        static int size = 0;                                        // 今フレームで使用する数
        static int maxSize = 0;                                     // プールの最大サイズ

        /// <summary>
        /// GL 描画イベント。登録された四角形をすべて描画する。
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

            // 今フレームで登録された四角形をすべて描画する
            for (int i = 0; i < size; i++)
            {
                GL.Color(drawData[i].color);

                // 四角形の頂点を順に描画
                foreach (var vertex in drawData[i].vertices)
                {
                    GL.Vertex(vertex);
                }
            }

            GL.End();
            GL.PopMatrix();

            size = 0;
        }

        /// <summary>
        /// 複数の四角形を描画キューに追加する。
        /// </summary>
        public static void Draw(Geometry[] quads)
        {
            // 配列内のすべての四角形を順に追加
            foreach (var quad in quads)
            {
                Draw(quad);
            }
        }

        /// <summary>
        /// 単一の四角形を描画キューに追加する。
        /// </summary>
        public static void Draw(Geometry quad)
        {
            // 既存プールに空きがある場合は上書き
            if (size < maxSize)
            {
                Instance.drawData[size] = quad;
            }
            else
            {
                // 空きがない場合は新規追加
                Instance.drawData.Add(quad);
                maxSize++;
            }

            size++;
        }
    }
}
