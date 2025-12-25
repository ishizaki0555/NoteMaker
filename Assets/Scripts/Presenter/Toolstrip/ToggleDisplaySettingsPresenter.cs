using NoteMaker.Model;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.InputSystem.Composites;
using UnityEngine.UI;

public class ToggleDisplaySettingsPresenter : MonoBehaviour
{
    [SerializeField] Button toggleDisplaySettingsPresenter = default;
    [SerializeField] GameObject settingsWindow = default;

    bool isMouseOverSettingsWindow = false;

    private void Awake()
    {
        toggleDisplaySettingsPresenter.OnClickAsObservable()
            .Subscribe(_ => Settings.IsOpen.Value = !Settings.IsOpen.Value);

        Observable.Merge(
            this.UpdateAsObservable()
                .Where(_ => Settings.IsOpen.Value)
                .Where(_ => Input.GetKey(KeyCode.Escape)),
            this.UpdateAsObservable()
                .Where(_ => Settings.IsOpen.Value)
                .Where(_ => !isMouseOverSettingsWindow)
                .Where(_ => Input.GetMouseButton(0)))
            .Subscribe(_ => Settings.IsOpen.Value = false);

        Settings.IsOpen.Subscribe(_ => Settings.SelectedBlock.Value = -1);
        Settings.IsOpen.Subscribe(isOpen => settingsWindow.SetActive(isOpen));
    }

    public void OnMouseEnterSettingsWindow()
    {
        isMouseOverSettingsWindow = true;
    }

    public void OnMouseExitSettingsWindow()
    {
        isMouseOverSettingsWindow = false;
    }
}
