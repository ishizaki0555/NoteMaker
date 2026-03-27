// ========================================
//
// NoteMaker Project
//
// ========================================
//
// BpmInputPresenter.cs
// BPM調整ラインがクリックされた際に表示されるダイアログの管理クラスです。
// 入力された数値を新しいBPMとし、OKボタン押下で適用します。
// 
//========================================

using NoteMaker.Model;
using NoteMaker.DTO;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// BPM入力用UIの制御を行うクラスです。
    /// 指定されたtickに、新しく入力されたBPMの変更情報を追加します。
    /// </summary>
    public class BpmInputPresenter : MonoBehaviour
    {
        [SerializeField] InputField inputField = default;
        [SerializeField] Button okButton = default;
        [SerializeField] Button cancelButton = default;
        [SerializeField] Button deleteButton = default;

        int currentTick = 0;

        void Awake()
        {
            if (okButton != null)
                okButton.OnClickAsObservable().Subscribe(_ => OnOkButtonClicked()).AddTo(this);

            if (cancelButton != null)
                cancelButton.OnClickAsObservable().Subscribe(_ => OnCancelButtonClicked()).AddTo(this);

            if (deleteButton != null)
                deleteButton.OnClickAsObservable().Subscribe(_ => OnDeleteButtonClicked()).AddTo(this);
        }


        /// <summary>
        /// 指定された位置（tick）に対してBPM入力を受け付けるため、UIを表示します。
        /// </summary>
        public void Show(int tick)
        {
            currentTick = tick;
            
            if (inputField != null)
            {
                // 初期値として現在の開始BPM（または適切な値）を入れておく
                inputField.text = EditData.BPM.Value.ToString();
            }

            gameObject.SetActive(true);

            if (inputField != null)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        void OnOkButtonClicked()
        {
            if (inputField != null && float.TryParse(inputField.text, out float newBpm))
            {
                // EditNotesPresenter経由でBPMの追加を要求（Undo/Redo対応）
                EditNotesPresenter.Instance.RequestForAddBpmChange.OnNext(new BpmChange(currentTick, newBpm));
            }

            // 自身を非表示にする
            gameObject.SetActive(false);
        }

        void OnCancelButtonClicked()
        {
            gameObject.SetActive(false);
        }

        void OnDeleteButtonClicked()
        {
            // 現在の tick にある BPMChange を探す
            var target = EditData.BpmChanges.FirstOrDefault(x => x.tick == currentTick);

            if (target != null)
            {
                EditNotesPresenter.Instance.RequestForRemoveBpmChange.OnNext(target);
            }

            gameObject.SetActive(false);
        }
    }
}
