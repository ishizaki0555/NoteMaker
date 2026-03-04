// ========================================
//
// NoteMaker Project
//
// ========================================
//
// SettingMaxBlockSpinBoxPresenter.cs
// ノーツ編集における「最大ブロック数（MaxBlock）」を操作する
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
    /// 最大ブロック数（EditData.MaxBlock）を操作するスピンボックスの Presenter です。
    /// ・SpinBoxPresenterBase が提供する増減 UI を利用  
    /// ・MaxBlock の ReactiveProperty を返すだけで連動可能  
    /// </summary>
    public class SettingMaxBlockSpinBoxPresenter : SpinBoxPresenterBase
    {
        /// <summary>
        /// SpinBoxPresenterBase が操作する対象の ReactiveProperty を返します。
        /// </summary>
        protected override ReactiveProperty<int> GetReactiveProperty()
        {
            return EditData.MaxBlock;
        }
    }
}
