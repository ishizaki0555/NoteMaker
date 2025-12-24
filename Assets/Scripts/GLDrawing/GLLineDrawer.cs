// ========================================
//
// GLLineDrawer.cs
//
// ========================================
//
// GL を使ってラインを描画するためのプール管理付きレンダラー
//
// ========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace NoteMaker.GLDrawing
{
    public class GLLineDrawer : SingletonMonoBehaviour<GLLineDrawer>
    {
        [SerializeField]
        Material mat = default;                 // 描画に使用するマテリアル

        List<Line> drawData = new List<Line>(); // 描画ラインのプール

        static int size = 0;                    // 今フレームで使用するライン数
        static int maxSize = 0;                 // プールの最大サイズ

        /// <summary>
        /// GL 描画イベント。登録されたラインをすべて描画する。
        /// </summary>
        void OnRenderObject()
        {
            GL.PushMatrix();
            mat.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Begin(GL.LINES);

            // プールが過剰に大きい場合は縮小する
            if (size * 2 < maxSize)
            {
                drawData.RemoveRange(size, maxSize - size);
                maxSize = size;
            }

            // 今フレームで登録されたラインをすべて描画する
            for (int i = 0; i < size; i++)
            {
                var line = drawData[i];
                GL.Color(line.color);
                GL.Vertex(line.start);
                GL.Vertex(line.end);
            }

            GL.End();
            GL.PopMatrix();

            size = 0;
        }

        /// <summary>
        /// 複数のラインを描画キューに追加する。
        /// </summary>
        public static void Draw(Line[] lines)
        {
            // 配列内のすべてのラインを順に追加する
            foreach (var line in lines)
            {
                Draw(line);
            }
        }

        /// <summary>
        /// 単一のラインを描画キューに追加する。
        /// </summary>
        public static void Draw(Line line)
        {
            // 既存プールに空きがある場合は上書き
            if (size < maxSize)
            {
                Instance.drawData[size] = line;
            }
            else
            {
                // 空きがない場合は新規追加
                Instance.drawData.Add(line);
                maxSize++;
            }

            size++;
        }
    }
}
