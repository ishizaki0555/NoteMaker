// ========================================
//
// KeyInput.cs
//
// ========================================
//
// Shift / Alt / Ctrl などのキー入力を扱うユーティリティ
//
// ========================================

using UnityEngine;

namespace NoteMaker.Utility
{
    public class KeyInput
    {
        /// <summary>
        /// Shift を押しながら指定キーを押したかどうかを返す。
        /// </summary>
        public static bool ShiftPlus(KeyCode keyCode)
        {
            return ShiftKey() && Input.GetKeyDown(keyCode);
        }

        /// <summary>
        /// Shift キーが押されているかどうかを返す。
        /// </summary>
        public static bool ShiftKey()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        /// <summary>
        /// Shift キーが押された瞬間かどうかを返す。
        /// </summary>
        public static bool ShiftKeyDown()
        {
            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        }

        /// <summary>
        /// Alt を押しながら指定キーを押したかどうかを返す。
        /// </summary>
        public static bool AltPlus(KeyCode keyCode)
        {
            return AltKey() && Input.GetKeyDown(keyCode);
        }

        /// <summary>
        /// Alt キーが押されているかどうかを返す。
        /// </summary>
        public static bool AltKey()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        /// <summary>
        /// Alt キーが押された瞬間かどうかを返す。
        /// </summary>
        public static bool AltKeyDown()
        {
            return Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);
        }

        /// <summary>
        /// Ctrl を押しながら指定キーを押したかどうかを返す。
        /// </summary>
        public static bool CtrlPlus(KeyCode keyCode)
        {
            return CtrlKey() && Input.GetKeyDown(keyCode);
        }

        /// <summary>
        /// Ctrl キーが押された瞬間かどうかを返す。
        /// </summary>
        public static bool CtrlKey()
        {
            return Input.GetKeyDown(KeyCode.LeftControl) ||
                   Input.GetKeyDown(KeyCode.LeftCommand) ||
                   Input.GetKeyDown(KeyCode.RightControl) ||
                   Input.GetKeyDown(KeyCode.RightCommand);
        }

        /// <summary>
        /// 現在押されているキーを 1 つ取得する。
        /// </summary>
        public static KeyCode FetchKey()
        {
            int e = System.Enum.GetNames(typeof(KeyCode)).Length;

            // すべての KeyCode を順にチェックして押されているキーを探す
            for (int i = 0; i < e; i++)
            {
                if (Input.GetKey((KeyCode)i))
                {
                    return (KeyCode)i;
                }
            }

            return KeyCode.None;
        }
    }
}
