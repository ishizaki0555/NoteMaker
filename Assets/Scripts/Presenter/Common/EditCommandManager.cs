using NoteMaker.Common;
using NoteMaker.Model;
using NoteMaker.Utility;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class EditCommandManager : SingletonMonoBehaviour<EditCommandManager>
    {
        CommandManager commandManager = new CommandManager();

        private void Awake()
        {
            
        }
    }
}