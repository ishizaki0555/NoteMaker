using UnityEngine;
using System.IO;
using System;

namespace NoteMaker.Utility
{
    /// <summary>
    /// AudioClipの指定された範囲をWAV形式でファイルに書き出すユーティリティです。
    /// </summary>
    public static class WavUtility
    {
        /// <summary>
        /// AudioClipの一部を切り取ってWAVファイルとして保存します。
        /// </summary>
        /// <param name="filepath">保存先のファイルパス（例: C:/path/Sample.wav）</param>
        /// <param name="clip">元のAudioClip</param>
        /// <param name="startSample">切り抜き開始サンプル</param>
        /// <param name="lengthSamples">切り抜くサンプル数</param>
        public static void Save(string filepath, AudioClip clip, int startSample, int lengthSamples)
        {
            if (clip == null)
            {
                Debug.LogError("AudioClip is null.");
                return;
            }
            
            if (lengthSamples <= 0 || startSample < 0 || startSample + lengthSamples > clip.samples)
            {
                Debug.LogError("Invalid sample range.");
                return;
            }

            // ディレクトリが存在しない場合は作成
            var dir = Path.GetDirectoryName(filepath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            int channels = clip.channels;
            int frequency = clip.frequency;
            
            // 指定範囲の音声データを取得
            float[] samples = new float[lengthSamples * channels];
            clip.GetData(samples, startSample);

            using (var fileStream = new FileStream(filepath, FileMode.Create))
            using (var writer = new BinaryWriter(fileStream))
            {
                // WAVヘッダ書き込み
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples.Length * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16); // format chunk size
                writer.Write((short)1); // format type (PCM)
                writer.Write((short)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2); // byte rate
                writer.Write((short)(channels * 2)); // block align
                writer.Write((short)16); // bits per sample
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(samples.Length * 2);

                // サンプルデータの変換 (float -> 16bit PCM)
                Int16[] intData = new Int16[samples.Length];
                int rescaleFactor = 32767;
                for (int i = 0; i < samples.Length; i++)
                {
                    // クリッピング対策
                    float val = Mathf.Clamp(samples[i], -1f, 1f);
                    intData[i] = (short)(val * rescaleFactor);
                }

                // 高速なバイト配列コピー
                byte[] byteData = new byte[samples.Length * 2];
                Buffer.BlockCopy(intData, 0, byteData, 0, byteData.Length);
                writer.Write(byteData);
            }
            
            Debug.Log($"WAV saved to: {filepath}");
        }
    }
}
