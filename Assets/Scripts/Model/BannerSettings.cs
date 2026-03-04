// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// BannerSettings.cs
// 楽曲バナー画像のパスを保持し、エディタ全体から参照できるようにする
// シンプルな設定クラスです。ReactiveProperty により変更を監視できます。
// 
//========================================

using UniRx;
using NoteMaker.Utility;

namespace NoteMaker.Model
{
    /// <summary>
    /// 楽曲バナー画像のパスを管理するクラスです。
    /// ReactiveProperty を用いてパス変更を監視できるため、
    /// UI 側は購読するだけで自動的に更新を反映できます。
    /// </summary>
    public class BannerSettings : SingletonMonoBehaviour<BannerSettings>
    {
        ReactiveProperty<string> bannerPath_ = new ReactiveProperty<string>(""); // バナー画像のファイルパス

        /// <summary>
        /// バナー画像のパスを ReactiveProperty として公開します。
        /// UI や他のシステムは購読することで変更を受け取れます。
        /// </summary>
        public static ReactiveProperty<string> BannerPath => Instance.bannerPath_;
    }
}
