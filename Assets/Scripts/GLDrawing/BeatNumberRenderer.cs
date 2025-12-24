// ========================================
//
// BeatNumberRenderer.cs
//
// ========================================
//
// 拍番号（Beat Number）を描画するためのプール管理付きレンダラー
//
// ========================================

using NoteMaker.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.GLDrawing
{
    public class BeatNumberRenderer : SingletonMonoBehaviour<BeatNumberRenderer>
    {
        [SerializeField] GameObject beatNumberPrefab = default;             // 拍番号表示用プレハブ

        List<RectTransform> rectTransformPool = new List<RectTransform>();  // RectTransform のプール
        List<Text> textPool = new List<Text>();                             // Text のプール

        static int size;                                                    // プールの総数
        static int countPrevActive = 0;                                     // 前フレームで使用された数
        static int countCurrentActive = 0;                                  // 今フレームで使用された数

        /// <summary>
        /// 指定位置に拍番号を描画する。
        /// </summary>
        public static void Render(Vector3 pos, int number)
        {
            // 既存プール内にまだ空きがある場合
            if (countCurrentActive < size)
            {
                // 前フレームより新しく使う場合はアクティブ化
                if (countCurrentActive >= countPrevActive)
                {
                    Instance.textPool[countCurrentActive].gameObject.SetActive(true);
                }

                Instance.rectTransformPool[countCurrentActive].position = pos;
                Instance.textPool[countCurrentActive].text = number.ToString();
            }
            else
            {
                // プールに空きがない場合は新規生成して追加
                var obj = Instantiate(Instance.beatNumberPrefab, pos, Quaternion.identity) as GameObject;
                obj.transform.SetParent(Instance.transform);
                obj.transform.localScale = Vector3.one;

                Instance.rectTransformPool.Add(obj.GetComponent<RectTransform>());
                Instance.textPool.Add(obj.GetComponent<Text>());
                size++;
            }

            countCurrentActive++;
        }

        /// <summary>
        /// 描画開始時に呼び出し、前フレームの使用数を記録する。
        /// </summary>
        public static void Begin()
        {
            countPrevActive = countCurrentActive;
            countCurrentActive = 0;
        }

        /// <summary>
        /// 描画終了時に呼び出し、不要なオブジェクトの非表示・削除を行う。
        /// </summary>
        public static void End()
        {
            // 今フレームで使わなかった分を非表示にする
            if (countCurrentActive < countPrevActive)
            {
                for (int i = countCurrentActive; i < countPrevActive; i++)
                {
                    Instance.textPool[i].gameObject.SetActive(false);
                }
            }

            // プールが過剰に大きい場合は削除して縮小する
            if (countCurrentActive * 2 < size)
            {
                // 余剰分を破棄
                foreach (var text in Instance.textPool.Skip(countCurrentActive + 1))
                {
                    Destroy(text.gameObject);
                }

                Instance.rectTransformPool.RemoveRange(countCurrentActive, size - countCurrentActive);
                Instance.textPool.RemoveRange(countCurrentActive, size - countCurrentActive);
                size = countCurrentActive;
            }
        }
    }
}
