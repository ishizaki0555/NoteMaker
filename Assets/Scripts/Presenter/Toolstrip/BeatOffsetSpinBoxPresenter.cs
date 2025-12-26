using NoteMaker.Model;
using UniRx;

namespace NoteMaker.Presenter
{
    public class BeatOffsetSpinBoxPresenter : SpinBoxPresenterBase
    {
        protected override ReactiveProperty<int> GetReactiveProperty()
        {
            return EditData.OffsetSamples;
        }
    }
}
