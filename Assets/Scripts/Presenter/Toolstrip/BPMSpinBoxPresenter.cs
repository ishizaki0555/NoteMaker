using NoteMaker.Model;
using UniRx;

namespace NoteMaker.Presenter
{
    public class BPMSpinBoxPresenter : SpinBoxPresenterBase
    {
        protected override ReactiveProperty<int> GetReactiveProperty()
        {
            return EditData.BPM;
        }
    }
}