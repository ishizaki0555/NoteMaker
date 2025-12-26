using NoteMaker.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace NoteMaker.Presenter
{
    public class MusicNameTextPresenter : MonoBehaviour
    {
        [SerializeField]
        Text musicNameText = default;

        void Awake()
        {
            EditData.Name.SubscribeToText(musicNameText);
        }
    }
}
