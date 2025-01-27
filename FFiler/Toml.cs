using System.Collections.Generic;

namespace FFiler
{
    internal class Toml
    {
        public static Setting Load(string str){
            var dic = new Setting();
            dic.setting = new Setting_main();
            dic.setting.columns_width = new List<int>();
            dic.setting.bookmarks = new List<string[]>();

            string tmp_str = "";
            string area_name = "";
            foreach (string item in str.Split('\n'))
            {
                tmp_str = item.Trim().Trim('\r');

                if(tmp_str.Length == 0) continue;
                if(tmp_str.StartsWith("#")) continue;


                if(tmp_str == "[version]")
                {
                    area_name = tmp_str;
                    continue;
                }
                else if(tmp_str == "[setting]")
                {
                    area_name = tmp_str;
                    continue;
                }


                switch (area_name)
                {
                    case "[version]":
                        dic.version = float.Parse(tmp_str.Split('=')[1].Trim().Trim('"'));
                        break;
                    case "[setting]":
                        if (tmp_str.StartsWith("["))
                        {
                            var ccc = tmp_str.Trim(',').Trim('[').Trim(']').Split(',');
                            var ss = new string[] { ccc[0].Trim().Trim('"'), ccc[1].Trim().Trim('"') };
                            dic.setting.bookmarks.Add(ss);
                        }
                        else if (tmp_str.StartsWith("]"))
                        {

                        }
                        else
                        {
                            var aaa = tmp_str.Split('=')[0].Trim();
                            switch (aaa)
                            {
                                case "width":
                                    dic.setting.width = int.Parse(tmp_str.Split('=')[1].Trim());
                                    break;
                                case "height":
                                    dic.setting.height = int.Parse(tmp_str.Split('=')[1].Trim());
                                    break;
                                case "columns_width":
                                    var bbb = tmp_str.Split('=')[1].Trim().Trim('[').Trim(']').Split(',');
                                    foreach (var b in bbb)
                                    {
                                        dic.setting.columns_width.Add(int.Parse(b.Trim()));
                                    }
                                    break;
                            }
                            
                        }
                        break;
                }
            }


            return dic;
        }
    }
}
