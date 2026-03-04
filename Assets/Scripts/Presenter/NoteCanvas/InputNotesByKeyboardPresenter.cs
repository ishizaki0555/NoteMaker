// ========================================
//
// NoteMaker Project
//
// ========================================
//
// InputNotesByKeyboardPresenter.cs
// キーボード入力によってノーツを配置するプレゼンターです。
// ・設定されたキー入力を監視
// ・現在の再生位置からノーツの拍位置を算出
// ・EditNotesPresenter にノーツ追加要求を送信
// 
//========================================

using NoteMaker.Notes;
using NoteMaker.Model;
using NoteMaker.Utility;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.Presenter
{
    /// <summary>
    /// キーボード入力でノーツを追加するクラスです。
    /// ・設定されたキー（Settings.NoteInputKeyCodes）を監視  
    /// ・再生中は少し前倒しで入力（offset）  
    /// ・現在の timeSamples から拍位置を計算  
    /// ・EditNotesPresenter にノーツ追加要求を送信  
    /// </summary>
    public class InputNotesByKeyboardPresenter : MonoBehaviour
    {
        EditNotesPresenter editPresenter;

        void Awake()
        {
            editPresenter = EditNotesPresenter.Instance;
            Audio.OnLoad.First().Subscribe(_ => Init());
        }

        /// <summary>
        /// キー入力監視ストリームを構築します。
        /// </summary>
        void Init()
        {
            this.UpdateAsObservable()
                .Where(_ => !Settings.IsOpen.Value)         // 設定ウィンドウが開いていない
                .Where(_ => !KeyInput.AltKey())             // Alt 無効
                .Where(_ => !KeyInput.CtrlKey())            // Ctrl 無効
                .Where(_ => !KeyInput.ShiftKey())           // Shift 無効
                .SelectMany(_ => Observable.Range(0, EditData.MaxBlock.Value))
                .Where(block => Input.GetKeyDown(Settings.NoteInputKeyCodes.Value[block]))
                .Subscribe(block => EnterNote(block));
        }

        /// <summary>
        /// キー入力されたブロックにノーツを追加します。
        /// </summary>
        void EnterNote(int block)
        {
            // 再生中は少し前倒しで入力（演奏感向上）
            var offset = -5000;

            // 1 拍あたりのサンプル数
            var unitBeatSamples =
                Audio.Source.clip.frequency * 60f / EditData.BPM.Value / EditData.LPB.Value;

            // 現在のサンプル位置（再生中は offset を加味）
            var timeSamples =
                Audio.Source.timeSamples
                - EditData.OffsetSamples.Value
                + (Audio.IsPlaying.Value ? offset : 0);

            // 拍位置（整数に丸める）
            var beats = Mathf.RoundToInt(timeSamples / unitBeatSamples);

            // ノーツ追加要求
            editPresenter.RequestForEditNote.OnNext(
                new Note(
                    new NotePosition(EditData.LPB.Value, beats, block),
                    EditState.NoteType.Value));
        }
    }
}
