// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// ClapSoundPlayer.cs
// ノート再生時にクラップ音を鳴らすためのサウンドプレイヤーです。
// 再生中の Audio.Source.timeSamples を監視し、
// ノート位置（サンプル位置）に到達したタイミングでクラップ音を再生します。
// 編集操作が行われた場合はキューを再構築し、正しいタイミングを維持します。
// 
//========================================

using NoteMaker.Model;
using NoteMaker.Presenter;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.SoundEffect
{
    /// <summary>
    /// ノート再生時にクラップ音を鳴らすクラスです。
    /// ・ノート位置をサンプル単位で計算  
    /// ・再生中にノート位置へ到達したらクラップ音を再生  
    /// ・編集中にノートが変化した場合はキューを再構築  
    /// ・クラップ音の ON/OFF は EditorState で制御  
    /// </summary>
    public class ClapSoundPlayer : MonoBehaviour
    {
        [SerializeField]
        AudioSource clapAudioSource = default; // 再生するクラップ音の AudioSource

        /// <summary>
        /// 初期化処理で、再生中のノート位置を監視し、区ラップ音を生成するためのキューを構築します
        /// </summary>
        void Awake()
        {
            var editPresenter = EditNotesPresenter.Instance;
            var clapOffsetSamples = 1800; // クラップ音を少し早めに鳴らすためのオフセット

            // 再生中に編集操作が行われたらキューを再構築するトリガー
            var editedDuringPlaybackObservable = Observable.Merge(
                    EditData.BPM.Skip(1).Select(_ => false),
                    EditData.BpmChanges.ObserveAdd().Select(_ => false),
                    EditData.BpmChanges.ObserveRemove().Select(_ => false),
                    EditData.BpmChanges.ObserveReplace().Select(_ => false),
                    EditData.BpmChanges.ObserveReset().Select(_ => false),
                    EditData.OffsetSamples.Skip(1).Select(_ => false),
                    editPresenter.RequestForEditNote.Select(_ => false),
                    editPresenter.RequestForRemoveNote.Select(_ => false),
                    editPresenter.RequestForAddNote.Select(_ => false))
                .Where(_ => Audio.IsPlaying.Value);

            // 再生開始 or 編集操作 → ノート位置キューを作り直す
            Audio.IsPlaying.Where(isPlaying => isPlaying)
                .Merge(editedDuringPlaybackObservable)
                .Select(_ =>
                    new Queue<int>(
                        EditData.Notes.Values
                            .Select(noteObject =>
                                noteObject.note.position.ToSamples(
                                    Audio.Source.clip.frequency,
                                    EditData.BPM.Value,
                                    EditData.BpmChanges))
                            .Distinct()
                            .Select(samples => samples + EditData.OffsetSamples.Value)
                            .Where(samples => Audio.Source.timeSamples <= samples)
                            .OrderBy(samples => samples)
                            .Select(samples => samples - clapOffsetSamples)))
                // LateUpdate で監視し続ける
                .SelectMany(samplesQueue =>
                    this.LateUpdateAsObservable()
                        .TakeWhile(_ => Audio.IsPlaying.Value)
                        .TakeUntil(editedDuringPlaybackObservable)
                        .Select(_ => samplesQueue))
                // キューが空でない
                .Where(samplesQueue => samplesQueue.Count > 0)
                // 次のノート位置に到達した
                .Where(samplesQueue => samplesQueue.Peek() <= Audio.Source.timeSamples)
                .Do(samplesQueue => samplesQueue.Dequeue())
                // クラップ音が有効な場合のみ再生
                .Where(_ => EditorState.ClapSoundEffectEnabled.Value)
                .Subscribe(_ => clapAudioSource.PlayOneShot(clapAudioSource.clip, 1));
        }
    }
}
