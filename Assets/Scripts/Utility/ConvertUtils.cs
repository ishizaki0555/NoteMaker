// ========================================
//
// ConvertUtils.cs
//
// ========================================
//
// ノート位置・サンプル位置・キャンバス座標の相互変換を行うユーティリティ
//
// ========================================

using NoteMaker.DTO;
using NoteMaker.Model;
using NoteMaker.Notes;
using UnityEngine;

namespace NoteMaker.Utility
{
    public class ConvertUtils : SingletonMonoBehaviour<ConvertUtils>
    {
        /// <summary>
        /// キャンバス上の X 座標をサンプル数に変換する。
        /// </summary>
        public static int CanvasPositionXToSamples(float x)
        {
            var per = (x - SamplesToCanvasPositionX(0)) / NoteCanvas.Width.Value;
            return Mathf.RoundToInt(Audio.Source.clip.samples * per);
        }

        /// <summary>
        /// サンプル数をキャンバス上の X 座標に変換する。
        /// </summary>
        public static float SamplesToCanvasPositionX(int samples)
        {
            // AudioClip が存在しない場合は 0 を返す
            if (Audio.Source.clip == null)
                return 0;

            return (samples - Audio.SmoothedTimeSamples.Value + EditData.OffsetSamples.Value)
                * NoteCanvas.Width.Value / Audio.Source.clip.samples
                + NoteCanvas.OffsetX.Value;
        }

        /// <summary>
        /// ブロック番号をキャンバス上の Y 座標に変換する。
        /// </summary>
        public static float BlockNumToCanvasPositionY(int blockNum)
        {
            var height = 240f;
            var maxIndex = EditData.MaxBlock.Value - 1;
            return ((maxIndex - blockNum) * height / maxIndex - height / 2) / NoteCanvas.ScaleFactor.Value;
        }

        /// <summary>
        /// ノート位置をキャンバス座標に変換する。
        /// </summary>
        public static Vector3 NoteToCanvasPosition(NotePosition notePosition)
        {
            return new Vector3(
                SamplesToCanvasPositionX(notePosition.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)),
                BlockNumToCanvasPositionY(notePosition.block) * NoteCanvas.ScaleFactor.Value,
                0);
        }

        /// <summary>
        /// 画面座標をキャンバス座標に変換する。
        /// </summary>
        public static Vector3 ScreenToCanvasPosition(Vector3 screenPosition)
        {
            return (screenPosition - new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0))
                * NoteCanvas.ScaleFactor.Value;
        }

        /// <summary>
        /// キャンバス座標を画面座標に変換する。
        /// </summary>
        public static Vector3 CanvasToScreenPosition(Vector3 canvasPosition)
        {
            return (canvasPosition / NoteCanvas.ScaleFactor.Value
                + new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
        }
    }
}