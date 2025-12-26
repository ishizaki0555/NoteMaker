using NoteMaker.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class ToggleWaveformDisplayPresenter : MonoBehaviour
    {
        [SerializeField]
        Toggle toggle = default;

        void Awake()
        {
            toggle.OnValueChangedAsObservable()
                .Subscribe(x => EditorState.WaveformDisplayEnabled.Value = x);
        }
    }
}
