// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// BPMUtility.cs
// 曲の途中でBPMが変化する（ソフラン）場合を考慮して、
// ノーツの拍位置（tick/num）と音声サンプル数（samples）の
// 相互変換を行うユーティリティです。
// 
//========================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NoteMaker.Model;
using NoteMaker.DTO;

namespace NoteMaker.Utility
{
    public static class BPMUtility
    {
        /// <summary>
        /// 1つの LPB 単位 (tick) が進むのに必要なサンプル数を計算します
        /// </summary>
        public static float GetSamplesPerTick(int frequency, float bpm, int LPB)
        {
            return frequency * 60f / bpm / LPB;
        }

        /// <summary>
        /// tick (num) から サンプル数への変換を行います。
        /// 途中のBPM変更イベントをすべて考慮して正確な時間を計算します。
        /// </summary>
        public static int CalculateSamples(int frequency, float initialBPM, int LPB, int targetNum, IEnumerable<BpmChange> changes)
        {
            if (changes == null || !changes.Any())
            {
                return Mathf.FloorToInt(targetNum * GetSamplesPerTick(frequency, initialBPM, LPB));
            }

            var sortedChanges = changes.OrderBy(c => c.tick).ToList();
            float currentBpm = initialBPM;
            int currentTick = 0;
            float totalSamples = 0f;

            foreach (var change in sortedChanges)
            {
                if (change.tick >= targetNum) break;

                int ticksInSegment = change.tick - currentTick;
                if (ticksInSegment > 0)
                {
                    totalSamples += ticksInSegment * GetSamplesPerTick(frequency, currentBpm, LPB);
                }

                currentBpm = change.bpm;
                currentTick = change.tick;
            }

            int remainingTicks = targetNum - currentTick;
            if (remainingTicks > 0)
            {
                totalSamples += remainingTicks * GetSamplesPerTick(frequency, currentBpm, LPB);
            }

            return Mathf.FloorToInt(totalSamples);
        }
        
        /// <summary>
        /// サンプル数から 現在の tick (num) への変換を行います。
        /// 途中のBPM変更イベントをすべて考慮して逆算します。
        /// </summary>
        public static int CalculateTick(int frequency, float initialBPM, int LPB, int targetSamples, IEnumerable<BpmChange> changes)
        {
            if (changes == null || !changes.Any())
            {
                return Mathf.FloorToInt(targetSamples / GetSamplesPerTick(frequency, initialBPM, LPB));
            }

            var sortedChanges = changes.OrderBy(c => c.tick).ToList();
            float currentBpm = initialBPM;
            int currentTick = 0;
            float currentSamples = 0f;

            foreach (var change in sortedChanges)
            {
                int ticksInSegment = change.tick - currentTick;
                if (ticksInSegment > 0)
                {
                    float samplesInSegment = ticksInSegment * GetSamplesPerTick(frequency, currentBpm, LPB);
                    if (currentSamples + samplesInSegment >= targetSamples)
                    {
                        break;
                    }
                    currentSamples += samplesInSegment;
                }

                currentBpm = change.bpm;
                currentTick = change.tick;
            }

            float remainingSamples = targetSamples - currentSamples;
            if (remainingSamples > 0)
            {
                int remainingTicks = Mathf.FloorToInt(remainingSamples / GetSamplesPerTick(frequency, currentBpm, LPB));
                return currentTick + remainingTicks;
            }
            
            return currentTick;
        }
    }
}
