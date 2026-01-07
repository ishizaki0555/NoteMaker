using NoteMaker.Model;
using NoteMaker.DTO;
using NoteMaker.Notes;
using UnityEngine;

namespace NoteMaker.Utility
{
    public class ConvertUtils : SingletonMonoBehaviour<ConvertUtils>
    {
        public static float canvasOffsetX = 375;
        public static float blockSpacingFactor = 1.5f;

        public static int CanvasPositionYToSamples(float y)
        {
            var per = (y - SamplesToCanvasPositionY(0)) / NoteCanvas.Height.Value;
            return Mathf.RoundToInt(Audio.Source.clip.samples * per);
        }

        public static float SamplesToCanvasPositionY(int samples)
        {
            if (Audio.Source.clip == null)
                return 0;

            return (samples - Audio.SmoothedTimeSamples.Value + EditData.OffsetSamples.Value)
                * NoteCanvas.Height.Value / Audio.Source.clip.samples
                + NoteCanvas.OffsetY.Value;
        }

        public static float BlockNumToCanvasPositionX(int blockNum)
        {
            var width = 240f;
            var maxIndex = EditData.MaxBlock.Value - 1;
            return ((blockNum * width / maxIndex) - width / 2) / NoteCanvas.ScaleFactor.Value;
        }

        public static float NoteCanvasPositionX(int blockNum)
        {
            return BlockNumToCanvasPositionX(blockNum) * blockSpacingFactor + canvasOffsetX;
        }

        // ★ NotePosition → Canvas座標（縦向き）
        public static Vector3 NoteToCanvasPosition(NotePosition notePosition)
        {
            return new Vector3(
                NoteCanvasPositionX(notePosition.block) * NoteCanvas.ScaleFactor.Value,
                SamplesToCanvasPositionY(notePosition.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)),
                0);
        }

        // ★ Screen → Canvas
        public static Vector3 ScreenToCanvasPosition(Vector3 screenPosition)
        {
            return (screenPosition - new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0))
                * NoteCanvas.ScaleFactor.Value;
        }

        // ★ Canvas → Screen
        public static Vector3 CanvasToScreenPosition(Vector3 canvasPosition)
        {
            return (canvasPosition / NoteCanvas.ScaleFactor.Value
                + new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
        }
    }
}
