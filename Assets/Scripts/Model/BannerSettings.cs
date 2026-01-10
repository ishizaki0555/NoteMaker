using UniRx;
using NoteMaker.Utility;

namespace NoteMaker.Model
{
    public class BannerSettings : SingletonMonoBehaviour<BannerSettings>
    {
        ReactiveProperty<string> bannerPath_ = new ReactiveProperty<string>("");
        public static ReactiveProperty<string> BannerPath => Instance.bannerPath_;
    }
}