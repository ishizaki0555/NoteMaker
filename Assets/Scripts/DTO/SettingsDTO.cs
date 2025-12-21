using System.Collections.Generic;

namespace NoteEditor.DTO
{
    [System.Serializable]
    public class SettingDTO
    {
        public string workSpacepath;
        public int maxBlock;
        public List<int> noteInputKeyCodes;

        public static SettingDTO GetDefaultSettings()
        {
            return new SettingDTO
            {
                workSpacepath = "",
                maxBlock = 5,
                noteInputKeyCodes = new List<int> { 114, 99, 103, 121, 98 }
            };
        }
    }
}