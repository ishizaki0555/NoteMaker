// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// SingletonMonoBehaviour.cs
// 任意の MonoBehaviour をシングルトンとして扱うための基底クラスです。
// シーン内に存在しない場合は自動生成し、常に 1 インスタンスのみを保証します。
// 
//========================================

using UnityEngine;

namespace NoteMaker.Utility
{
    /// <summary>
    /// 任意の MonoBehaviour をシングルトンとして扱うための基底クラスです。
    /// ・シーン内に既存のインスタンスがあればそれを返す  
    /// ・存在しなければ自動生成して返す  
    /// という仕組みにより、どこからでも安全にアクセスできます。
    /// </summary>
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        static T instance_; // シングルトンインスタンス

        /// <summary>
        /// シングルトンインスタンスへのアクセス。
        /// シーン内に存在しない場合は自動生成されます。
        /// </summary>
        public static T Instance
        {
            get
            {
                // インスタンスがまだ存在しない場合はシーン内容から検索
                if (instance_ == null)
                {
                    instance_ = FindAnyObjectByType<T>();
                }

                return instance_ ?? new GameObject(typeof(T).FullName).AddComponent<T>();
            }
        }
    }
}
