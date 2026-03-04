// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// BeatNumberRenderer.cs
// 拍番号（Beat Number）を GL 描画領域に表示するための
// オブジェクトプール付きレンダラーを提供します。
// Begin → Render → End の 3 ステップで描画を管理し、
// 毎フレームの生成コストを抑えつつ効率的に UI を更新します。
// 
//========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.GLDrawing
{
    /// <summary>
    /// 拍番号（Beat Number）を描画するためのレンダラーです。
    /// オブジェクトプールを利用して、毎フレームの生成コストを削減しつつ
    /// 必要な数だけ UI を表示します。
    /// 
    /// 描画は以下の 3 ステップで行います：
    /// 1. Begin()  : 前フレームの使用数を記録し、カウンタをリセット
    /// 2. Render() : 必要な数だけ UI を再利用または生成
    /// 3. End()    : 余った UI を非アクティブ化し、必要ならプール縮小
    /// </summary>
    public class BeatNumberRenderer : SingletonMonoBehaviour<BeatNumberRenderer>
    {
        [SerializeField] GameObject beatNumberPrefab = default; // 拍番号 UI のプレハブ

        List<RectTransform> rectTransformPool = new List<RectTransform>(); // RectTransform のプール
        List<Text> textPool = new List<Text>();                             // Text コンポーネントのプール

        static int size = 0;                // プールの総数
        static int countPrevActive = 0;     // 前フレームで使用された数
        static int countCurrentActive = 0;  // 今フレームで使用された数

        /// <summary>
        /// 描画開始処理。
        /// 前フレームの使用数を記録し、今フレームの使用カウンタをリセットします。
        /// </summary>
        public static void Begin()
        {
            countPrevActive = countCurrentActive;
            countCurrentActive = 0;
        }

        /// <summary>
        /// 拍番号を 1 つ描画します。
        /// 既存オブジェクトを再利用し、足りない場合は新規生成します。
        /// </summary>
        /// <param name="pos">表示位置（ワールド座標）</param>
        /// <param name="number">表示する拍番号</param>
        public static void Render(Vector3 pos, int number)
        {
            if (countCurrentActive < size)
            {
                // 非アクティブだった UI を再利用する場合はアクティブ化
                if (countCurrentActive >= countPrevActive)
                    Instance.textPool[countCurrentActive].gameObject.SetActive(true);

                // 位置と表示内容を更新
                Instance.rectTransformPool[countCurrentActive].position = pos;
                Instance.textPool[countCurrentActive].text = number.ToString();
            }
            else
            {
                // 新規 UI を生成してプールに追加
                var obj = Instantiate(Instance.beatNumberPrefab, pos, Quaternion.identity);
                obj.transform.SetParent(Instance.transform, false);
                obj.transform.localScale = Vector3.one;

                Instance.rectTransformPool.Add(obj.GetComponent<RectTransform>());
                Instance.textPool.Add(obj.GetComponent<Text>());
                size++;
            }

            countCurrentActive++;
        }

        /// <summary>
        /// 描画終了処理。
        /// 今フレームで使用しなかった UI を非アクティブ化し、
        /// 必要に応じてプールを縮小します。
        /// </summary>
        public static void End()
        {
            // 今フレームで使わなかった UI を非アクティブ化
            if (countCurrentActive < countPrevActive)
            {
                for (int i = countCurrentActive; i < countPrevActive; i++)
                    Instance.textPool[i].gameObject.SetActive(false);
            }

            // プールが過剰に大きい場合は縮小
            if (countCurrentActive * 2 < size)
            {
                int removeCount = size - countCurrentActive;

                // 余った UI を破棄
                for (int i = countCurrentActive; i < size; i++)
                    Destroy(Instance.textPool[i].gameObject);

                // プールから削除
                Instance.rectTransformPool.RemoveRange(countCurrentActive, removeCount);
                Instance.textPool.RemoveRange(countCurrentActive, removeCount);

                size = countCurrentActive;
            }
        }
    }
}
