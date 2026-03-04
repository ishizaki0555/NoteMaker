// ========================================
//
// NoteMaker Project
//
// ========================================
//
// BeatOffsetSpinBoxPresenter.cs
// ノーツ全体の「オフセット（OffsetSamples）」を調整する
// スピンボックス UI のプレゼンターです。
// SpinBoxPresenterBase が提供する共通 UI ロジックに対して、
// このクラスは「どの値を操作するか」だけを指定します。
//
//========================================

using NoteMaker.Model;
using UniRx;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// ノーツのオフセット（EditData.OffsetSamples）を操作するスピンボックスの Presenter です。
    /// ・SpinBoxPresenterBase の UI 操作で OffsetSamples を増減  
    /// ・ReactiveProperty を返すだけで自動連動  
    /// </summary>
    public class BeatOffsetSpinBoxPresenter : SpinBoxPresenterBase
    {
        /// <summary>
        /// SpinBoxPresenterBase が操作する対象の ReactiveProperty を返します。
        /// </summary>
        protected override ReactiveProperty<int> GetReactiveProperty()
        {
            return EditData.OffsetSamples;
        }
    }
}
