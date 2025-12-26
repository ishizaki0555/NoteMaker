using NoteMaker.Model;
using UniRx;

namespace NoteMaker.Presenter
{
    public class LPBSpinBoxPresenter : SpinBoxPresenterBase
    {
        protected override ReactiveProperty<int> GetReactiveProperty()
        {
            return EditData.LPB;
        }
    }
}
