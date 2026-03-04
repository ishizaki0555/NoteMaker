// ========================================
// 
// NoteMaker Project
// 
// ========================================
// 
// ConvertUtils.cs
// ノート位置・サンプル位置・キャンバス座標・スクリーン座標の相互変換を行う
// エディタ全体で使用されるユーティリティクラスです。
// ノート描画・クリック判定・ロングノーツ線描画など、
// ほぼすべての座標計算がここを経由します。
// 
//========================================

using NoteMaker.Model;
using NoteMaker.DTO;
using NoteMaker.Notes;
using UnityEngine;

namespace NoteMaker.Utility
{
    /// <summary>
    /// ノート位置・サンプル位置・キャンバス座標・スクリーン座標の変換を行うユーティリティです。
    /// ・サンプル位置 ⇄ キャンバス Y 座標  
    /// ・ブロック番号 → キャンバス X 座標  
    /// ・NotePosition → Canvas 座標  
    /// ・Screen ⇄ Canvas 座標  
    /// といった変換を一元管理します。
    /// </summary>
    public class ConvertUtils : SingletonMonoBehaviour<ConvertUtils>
    {
        public static float canvasOffsetX = 375;          // キャンバスの X オフセット
        public static float blockSpacingFactor = 1.5f;    // ブロック間の間隔倍率

        /// <summary>
        /// キャンバス上の Y 座標をサンプル位置に変換します。
        /// </summary>
        public static int CanvasPositionYToSamples(float y)
        {
            var per = (y - SamplesToCanvasPositionY(0)) / NoteCanvas.Height.Value;
            return Mathf.RoundToInt(Audio.Source.clip.samples * per);
        }

        /// <summary>
        /// サンプル位置をキャンバス上の Y 座標に変換します。
        /// 再生位置（SmoothedTimeSamples）とオフセットを考慮します。
        /// </summary>
        public static float SamplesToCanvasPositionY(int samples)
        {
            // 音源がロードされていない場合は０を返す
            if (Audio.Source.clip == null)
                return 0;

            return (samples - Audio.SmoothedTimeSamples.Value + EditData.OffsetSamples.Value)
                * NoteCanvas.Height.Value / Audio.Source.clip.samples
                + NoteCanvas.OffsetY.Value;
        }

        /// <summary>
        /// ブロック番号をキャンバス X 座標に変換します（スケール前）。
        /// </summary>
        public static float BlockNumToCanvasPositionX(int blockNum)
        {
            var width = 240f;
            var maxIndex = EditData.MaxBlock.Value - 1;
            return ((blockNum * width / maxIndex) - width / 2) / NoteCanvas.ScaleFactor.Value;
        }

        /// <summary>
        /// ブロック番号をキャンバス X 座標に変換します（スケール後）。
        /// </summary>
        public static float NoteCanvasPositionX(int blockNum)
        {
            return BlockNumToCanvasPositionX(blockNum) * blockSpacingFactor + canvasOffsetX;
        }

        /// <summary>
        /// NotePosition → Canvas 座標（縦向きエディタ用）。
        /// </summary>
        public static Vector3 NoteToCanvasPosition(NotePosition notePosition)
        {
            return new Vector3(
                NoteCanvasPositionX(notePosition.block) * NoteCanvas.ScaleFactor.Value,
                SamplesToCanvasPositionY(
                    notePosition.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)),
                0);
        }

        /// <summary>
        /// Screen 座標 → Canvas 座標。
        /// </summary>
        public static Vector3 ScreenToCanvasPosition(Vector3 screenPosition)
        {
            return (screenPosition - new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0))
                * NoteCanvas.ScaleFactor.Value;
        }

        /// <summary>
        /// Canvas 座標 → Screen 座標。
        /// </summary>
        public static Vector3 CanvasToScreenPosition(Vector3 canvasPosition)
        {
            return (canvasPosition / NoteCanvas.ScaleFactor.Value
                + new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
        }
    }
}
