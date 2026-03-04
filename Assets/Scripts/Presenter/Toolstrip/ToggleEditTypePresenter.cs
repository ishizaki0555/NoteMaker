// ========================================
//
// NoteMaker Project
//
// ========================================
//
// ToggleEditTypePresenter.cs
// ノーツ編集モード（単ノーツ / ロングノーツ）を切り替えるプレゼンターです。
// ・ボタン押下または Alt キーでノーツ種別をトグル
// ・現在の種別に応じてアイコンとボタン色を変更
//
//========================================

using NoteMaker.Notes;
using NoteMaker.Model;
using NoteMaker.Utility;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// ノーツ編集タイプ（単ノーツ / ロングノーツ）を切り替えるクラスです。
    /// ・ボタン押下または Alt キーで NoteType をトグル  
    /// ・NoteType の変化に応じてアイコンと色を更新  
    /// </summary>
    public class ToggleEditTypePresenter : MonoBehaviour
    {
        [SerializeField] Button editTypeToggleButton = default;       // 種別切り替えボタン
        [SerializeField] Sprite iconLongNotes = default;              // ロングノーツ用アイコン
        [SerializeField] Sprite iconSingleNotes = default;            // 単ノーツ用アイコン
        [SerializeField] Color longTypeStateButtonColor = default;    // ロングノーツ時の色
        [SerializeField] Color singleTypeStateButtonColor = default;  // 単ノーツ時の色

        void Awake()
        {
            //===============================
            // ボタン押下 or Alt キーで種別トグル
            //===============================
            editTypeToggleButton
                .OnClickAsObservable()
                .Merge(this.UpdateAsObservable().Where(_ => KeyInput.AltKeyDown()))
                .Select(_ =>
                    EditState.NoteType.Value == NoteTypes.Single
                        ? NoteTypes.Long
                        : NoteTypes.Single)
                .Subscribe(editType =>
                    EditState.NoteType.Value = editType);

            //===============================
            // 種別に応じてアイコンと色を更新
            //===============================
            var buttonImage = editTypeToggleButton.GetComponent<Image>();

            // NoteTypeを監視し、アイコンと色を切り替える
            EditState.NoteType
                .Select(type => type == NoteTypes.Long)
                .Subscribe(isLongType =>
                {
                    buttonImage.sprite = isLongType ? iconLongNotes : iconSingleNotes;
                    buttonImage.color = isLongType ? longTypeStateButtonColor : singleTypeStateButtonColor;
                });
        }
    }
}
