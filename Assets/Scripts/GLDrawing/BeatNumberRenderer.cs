using NoteMaker.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.GLDrawing
{
    public class BeatNumberRenderer : SingletonMonoBehaviour<BeatNumberRenderer>
    {
        [SerializeField]
        GameObject beatNumberPrefab = default;

        List<RectTransform> rectTransformPool = new List<RectTransform>();
        List<Text> textPool = new List<Text>();

        static int size = 0;
        static int countPrevActive = 0;
        static int countCurrentActive = 0;

        // ============================
        // ★ Begin：横版と同じ構造
        // ============================
        public static void Begin()
        {
            countPrevActive = countCurrentActive;
            countCurrentActive = 0;
        }

        // ============================
        // ★ Render：横版構造に統一
        // ============================
        public static void Render(Vector3 pos, int number)
        {
            if (countCurrentActive < size)
            {
                // 以前非アクティブだったものを再利用
                if (countCurrentActive >= countPrevActive)
                    Instance.textPool[countCurrentActive].gameObject.SetActive(true);

                Instance.rectTransformPool[countCurrentActive].position = pos;
                Instance.textPool[countCurrentActive].text = number.ToString();
            }
            else
            {
                // 新規生成（横版と同じ構造）
                var obj = Instantiate(Instance.beatNumberPrefab, pos, Quaternion.identity);
                obj.transform.SetParent(Instance.transform, false);
                obj.transform.localScale = Vector3.one;

                Instance.rectTransformPool.Add(obj.GetComponent<RectTransform>());
                Instance.textPool.Add(obj.GetComponent<Text>());
                size++;
            }

            countCurrentActive++;
        }

        // ============================
        // ★ End：横版の縮小ロジックに統一
        // ============================
        public static void End()
        {
            // 余ったものを非アクティブ化
            if (countCurrentActive < countPrevActive)
            {
                for (int i = countCurrentActive; i < countPrevActive; i++)
                    Instance.textPool[i].gameObject.SetActive(false);
            }

            // プール縮小（横版と同じ条件）
            if (countCurrentActive * 2 < size)
            {
                int removeCount = size - countCurrentActive;

                // 正しい範囲で削除（Skip の off-by-one 問題を修正）
                for (int i = countCurrentActive; i < size; i++)
                    Destroy(Instance.textPool[i].gameObject);

                Instance.rectTransformPool.RemoveRange(countCurrentActive, removeCount);
                Instance.textPool.RemoveRange(countCurrentActive, removeCount);

                size = countCurrentActive;
            }
        }
    }
}
