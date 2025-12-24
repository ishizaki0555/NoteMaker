// ========================================
//
// SingletonMonoBehaviour.cs
//
// ========================================
//
// MonoBehaviour を継承したシングルトン基底クラス
//
// ========================================

using UnityEngine;

namespace NoteMaker.Utility
{
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        static T instance_;   // シングルトンインスタンス

        /// <summary>
        /// シングルトンインスタンスを取得する。
        /// 存在しない場合はシーン内から検索し、見つからなければ新規生成する。
        /// </summary>
        public static T Instance
        {
            get
            {
                // まだインスタンスが存在しない場合はシーンから検索する
                if (instance_ == null)
                {
                    instance_ = FindAnyObjectByType<T>();
                }

                // 見つからなければ新しい GameObject を生成してアタッチする
                return instance_ ?? new GameObject(typeof(T).FullName).AddComponent<T>();
            }
        }
    }
}
