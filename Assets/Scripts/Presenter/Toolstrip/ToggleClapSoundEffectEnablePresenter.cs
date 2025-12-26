using NoteMaker.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class ToggleClapSoundEffectEnablePresenter : MonoBehaviour
    {
        [SerializeField]
        Toggle toggle = default;

        void Awake()
        {
            toggle.OnValueChangedAsObservable()
                .Subscribe(isEnabled => EditorState.ClapSoundEffectEnabled.Value = isEnabled);
        }
    }
}
