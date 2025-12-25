// ========================================
//
// InputNotesByKeyboardPresenter.cs
//
// ========================================
//
// キーボード入力によるノート配置を管理する Presenter。
// ・設定されたキー入力を監視し、ノートを追加
// ・再生中はオフセットを考慮して配置
// ・Audio.OnLoad 後に初期化
//
// ========================================

using NoteMaker.Model;
using NoteMaker.Notes;
using NoteMaker.Utility;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    public class InputNotesByKeyboardPresenter : MonoBehaviour
    {
        EditNotesPresenter editPresenter;

        /// <summary>
        /// コンポーネント生成直後に呼ばれる初期化処理。
        /// EditNotesPresenter の参照取得と、ロード後の初期化登録を行う。
        /// </summary>
        void Awake()
        {
            editPresenter = EditNotesPresenter.Instance;
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// Audio.OnLoad 後に呼ばれる初期化処理。
        /// キーボード入力を監視し、ノート入力処理をセットアップする。
        /// </summary>
        void Init()
        {
            this.UpdateAsObservable()
                .Where(_ => !Settings.IsOpen.Value)          // 設定画面が開いていない
                .Where(_ => !KeyInput.AltKey())              // Alt 無効
                .Where(_ => !KeyInput.CtrlKey())             // Ctrl 無効
                .Where(_ => !KeyInput.ShiftKey())            // Shift 無効
                .SelectMany(_ => Observable.Range(0, EditData.MaxBlock.Value)) // 全ブロックを走査
                .Where(block => Input.GetKeyDown(Settings.NoteInputKeyCodes.Value[block]))
                .Subscribe(block => EnterNote(block));
        }

        /// <summary>
        /// 指定ブロックにノートを入力する。
        /// 再生中はオフセットを考慮して配置位置を補正する。
        /// </summary>
        void EnterNote(int block)
        {
            var offset = -5000;
            var unitBeatSamples = Audio.Source.clip.frequency * 60f / EditData.BPM.Value / EditData.LPB.Value;

            var timeSamples =
                Audio.Source.timeSamples
                - EditData.OffsetSamples.Value
                + (Audio.IsPlaying.Value ? offset : 0);

            var beats = Mathf.RoundToInt(timeSamples / unitBeatSamples);

            editPresenter.RequestForEditNote.OnNext(
                new Note(
                    new NotePosition(EditData.LPB.Value, beats, block),
                    EditState.NoteType.Value));
        }
    }
}
