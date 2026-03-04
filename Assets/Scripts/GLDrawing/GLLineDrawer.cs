// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// GLLineDrawer.cs
// GL を用いてライン（線分）を描画するためのレンダラーを提供します。
// オブジェクトプール方式で Line データを管理し、
// 毎フレームの描画負荷を抑えつつ効率的に線を描画します。
// 
//========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace NoteMaker.GLDrawing
{
    /// <summary>
    /// GL を使用して線分を描画するためのクラスです。
    /// Line データをプールし、毎フレームの生成コストを抑えながら
    /// 必要な線分だけを描画します。
    /// 
    /// 描画は Unity の OnRenderObject() タイミングで行われます。
    /// </summary>
    public class GLLineDrawer : SingletonMonoBehaviour<GLLineDrawer>
    {
        [SerializeField] Material mat = default; // 線描画に使用するマテリアル

        List<Line> drawData = new List<Line>(); // 描画対象の Line データを保持するプール

        static int size = 0;     // 今フレームで描画する Line 数
        static int maxSize = 0;  // プールされている Line の最大数

        /// <summary>
        /// Unity の描画イベントで呼ばれ、登録された Line をすべて描画します。
        /// 描画後、今フレームの使用数をリセットします。
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

            // 今フレームの Line を順に描画
            for (int i = 0; i < size; i++)
            {
                var line = drawData[i];
                GL.Color(line.color);
                GL.Vertex(line.start);
                GL.Vertex(line.end);
            }

            GL.End();
            GL.PopMatrix();

            // 次フレームに備えて使用数をリセット
            size = 0;
        }

        /// <summary>
        /// 複数の Line を描画キューに追加します。
        /// </summary>
        /// <param name="lines">描画する Line 配列。</param>
        public static void Draw(Line[] lines)
        {
            foreach (var line in lines)
                Draw(line);
        }

        /// <summary>
        /// 単一の Line を描画キューに追加します。
        /// プールに空きがあれば再利用し、なければ新規追加します。
        /// </summary>
        /// <param name="line">描画する Line。</param>
        public static void Draw(Line line)
        {
            if (size < maxSize)
            {
                // 既存のプールを再利用
                Instance.drawData[size] = line;
            }
            else
            {
                // 新規追加
                Instance.drawData.Add(line);
                maxSize++;
            }

            size++;
        }
    }
}
