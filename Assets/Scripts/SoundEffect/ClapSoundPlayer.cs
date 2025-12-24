// ========================================
//
// ClapSoundPlayer.cs
//
// ========================================
//
// ノート再生時に「クラップ音」を鳴らすクラス。
// 再生中のノート位置を監視し、指定サンプル位置に到達したら効果音を再生する。
// UniRx を用いて、再生状態・編集操作・LateUpdate を組み合わせて制御する。
//
// ========================================

using NoteMaker.Model;
using NoteMaker.Presenter;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteMaker.SoundEffect
{
    public class ClapSoundPlayer : MonoBehaviour
    {
        [SerializeField]
        AudioSource clapAudioSource = default; // 再生するクラップ音

        /// <summary>
        /// 再生中のノート位置に応じてクラップ音を鳴らす処理をセットアップする。
        /// </summary>
        void Awake()
        {
            var editPresenter = EditNotesPresenter.Instance;
            var clapOffsetSamples = 1800; // 音を鳴らすタイミングの補正値

            // 再生中に編集操作が行われたらクラップ処理をリセットする
            var editedDuringPlaybackObservable = Observable.Merge(
                    EditData.OffsetSamples.Select(_ => false),
                    editPresenter.RequestForEditNote.Select(_ => false),
                    editPresenter.RequestForRemoveNote.Select(_ => false),
                    editPresenter.RequestForAddNote.Select(_ => false))
                .Where(_ => Audio.IsPlaying.Value);

            // 再生開始 or 再生中の編集操作をトリガーにクラップキューを生成
            Audio.IsPlaying.Where(isPlaying => isPlaying)
                .Merge(editedDuringPlaybackObservable)
                .Select(_ =>
                    new Queue<int>(
                        EditData.Notes.Values
                            .Select(noteObject =>
                                noteObject.note.position.ToSamples(
                                    Audio.Source.clip.frequency,
                                    EditData.BPM.Value))
                            .Distinct()
                            .Select(samples => samples + EditData.OffsetSamples.Value)
                            .Where(samples => Audio.Source.timeSamples <= samples)
                            .OrderBy(samples => samples)
                            .Select(samples => samples - clapOffsetSamples)))
                // LateUpdate のたびにキューを監視
                .SelectMany(samplesQueue =>
                    this.LateUpdateAsObservable()
                        .TakeWhile(_ => Audio.IsPlaying.Value)
                        .TakeUntil(editedDuringPlaybackObservable.Skip(1))
                        .Select(_ => samplesQueue))
                // キューが空でない
                .Where(samplesQueue => samplesQueue.Count > 0)
                // 次のクラップタイミングに到達した
                .Where(samplesQueue => samplesQueue.Peek() <= Audio.Source.timeSamples)
                .Do(samplesQueue => samplesQueue.Dequeue())
                // 効果音が有効な場合のみ再生
                .Where(_ => EditorState.ClapSoundEffectEnabled.Value)
                .Subscribe(_ => clapAudioSource.PlayOneShot(clapAudioSource.clip, 1));
        }
    }
}
