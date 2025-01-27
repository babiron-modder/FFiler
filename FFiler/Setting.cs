using System.Collections.Generic;

namespace FFiler
{
    public class Setting
    {
        public float version;
        public Setting_main setting;
    }
    public class Setting_main
    {
        public int width;
        public int height;
        public List<int> columns_width;
        public List<string[]> bookmarks;
    }
}
