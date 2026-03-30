// ========================================
//
// NoteMaker Project
//
// ========================================
//
// BPMSpinBoxPresenter.cs
// ノーツ編集における BPM（EditData.BPM）を調整する
// スピンボックス UI のプレゼンターです。
// SpinBoxPresenterBase が提供する共通 UI ロジックに対して、
// このクラスは「どの値を操作するか」だけを指定します。
//
//========================================

using NoteMaker.Model;
using UniRx;

namespace NoteMaker.Presenter
{
    public class BPMSpinBoxPresenter : FloatSpinBoxPresenterBase
    {
        protected override ReactiveProperty<float> GetReactiveProperty()
        {
            return EditData.BPM;
        }
    }
}
