using NoteMaker.Model;
using UniRx;

namespace NoteMaker.Presenter
{
    public class SettingMaxBlockSpinBoxPresenter : SpinBoxPresenterBase
    {
        protected override ReactiveProperty<int> GetReactiveProperty()
        {
            return EditData.MaxBlock;
        }
    }
}
