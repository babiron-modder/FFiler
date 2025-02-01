using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace FFiler
{
    public partial class Form1 : Form
    {
        // SHGetFileInfo関数
        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, int hToken = 0);

        // SHGetFileInfo関数で使用するフラグ
        private const uint SHGFI_ICON = 0x100; // アイコン・リソースの取得
        private const uint SHGFI_LARGEICON = 0x0; // 大きいアイコン
        private const uint SHGFI_SMALLICON = 0x1; // 小さいアイコン
        private const int MAX_PATH = 32767;
        private Guid FOLDERID_Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");


        SHFILEINFO shinfo = new SHFILEINFO();
        Main main_window;
        string guid;
        bool textbox_is_focused_flag = false;

        // SHGetFileInfo関数で使用する構造体
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };


        class DataTag : IDisposable
        {
            public bool is_folder;
            public string path;
            public DataTag(bool _is_folder, string _path)
            {
                is_folder = _is_folder;
                path = _path;
            }
            ~DataTag() {
                path = null;
            }

            public void Dispose()
            {
                path = null;
            }
        }

        string current_path = "";
        List<string> path_history = new List<string>();
        int history_index = 0;
        public Form1(Main mainwindow, string inguid, string path="")
        {
            main_window = mainwindow;
            guid = inguid;
            if (string.IsNullOrEmpty(path))
            {
                current_path = Directory.GetCurrentDirectory();
            }
            else if (Directory.Exists(path))
            {
                current_path = Path.GetFullPath(path);
            }
            else
            {
                current_path = Directory.GetCurrentDirectory();
            }
            path_history.Add(current_path);
            InitializeComponent();
            ExtractAssociatedIconEx(current_path);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // ドライブ
            foreach (var drive in Directory.GetLogicalDrives())
            {
                toolStrip1.Items.Add(new ToolStripSeparator());
                addToolStripButton(drive, drive);
            }
            toolStrip1.Items.Add(new ToolStripSeparator());

            // OneDrive
            var tmp_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");
            if (Directory.Exists(tmp_path))
            {
                addToolStripButton("OneDrive", tmp_path);
                toolStrip1.Items.Add(new ToolStripSeparator());
            }

            // Box
            tmp_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Box");
            if (Directory.Exists(tmp_path))
            {
                addToolStripButton("Box", tmp_path);
                toolStrip1.Items.Add(new ToolStripSeparator());
            }

            // DropBox
            tmp_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Dropbox");
            if (Directory.Exists(tmp_path))
            {
                addToolStripButton("Dropbox", tmp_path);
                toolStrip1.Items.Add(new ToolStripSeparator());
            }

            // 各フォルダ
            addToolStripButton("デスクトップ", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            addToolStripButton("ダウンロード", SHGetKnownFolderPath(FOLDERID_Downloads, 0));
            addToolStripButton("ドキュメント", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            addToolStripButton("ピクチャ", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            addToolStripButton("ミュージック", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            addToolStripButton("ビデオ", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));

            toolStrip1.Items.Add(new ToolStripSeparator());
            try
            {
                if (main_window.setting != null)
                {
                    Width = main_window.setting.setting.width;
                    Height = main_window.setting.setting.height;
                    for (int i = 0; i < main_window.setting.setting.columns_width.Count; ++i)
                    {
                        listView2.Columns[i].Width = main_window.setting.setting.columns_width[i];
                    }

                    for (int i = 0; i < main_window.setting.setting.bookmarks.Count; ++i)
                    {
                        addToolStripButton(main_window.setting.setting.bookmarks[i][0], main_window.setting.setting.bookmarks[i][1]);
                    }
                }
            }
            catch
            {

            }
        }

        private void addToolStripButton(string name, string path)
        {
            var toolbutton = new ToolStripButton();
            toolbutton.ToolTipText = name;
            IntPtr hSuccess = SHGetFileInfo(path, 0,
                        ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
            if (hSuccess != IntPtr.Zero)
            {
                toolbutton.Image = Icon.FromHandle(shinfo.hIcon).ToBitmap();
            }
            toolbutton.Tag = new DataTag(true, path);
            toolbutton.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    var tmp_path = (DataTag)((ToolStripButton)sender).Tag;
                    if (current_path != tmp_path.path)
                    {
                        current_path = tmp_path.path;
                        path_history.RemoveRange(history_index + 1, path_history.Count - 1 - history_index);
                        path_history.Add(current_path);
                        history_index++;
                        ExtractAssociatedIconEx(current_path);
                    }
                }
                else if (e.Button == MouseButtons.Middle)
                {
                    var tmp_path = (DataTag)((ToolStripButton)sender).Tag;
                    main_window.Add(tmp_path.path);
                }
            };
            toolStrip1.Items.Add(toolbutton);
        }

        class ListViewItemOver : ListViewItem
        {
            ~ListViewItemOver()
            {
                for(int i = 0; i< SubItems.Count; i++)
                {
                    SubItems[i].Text = null;
                }
                SubItems.Clear();
                Tag = null;
            }
        }



        public void ExtractAssociatedIconEx(string path)
        {
            // Get the c:\ directory.
            var dir = new DirectoryInfo(path);
            textBox1.Text = dir.FullName;
            Text = dir.Name;

            ListViewItem item;
            listView2.BeginUpdate();
            listView2.Items.Clear();
            GC.Collect();

            // Set a default icon for the file.
            Icon iconForFile = SystemIcons.WinLogo;
            if (!imageList1.Images.ContainsKey("F_")){
                imageList1.Images.Add("F_", iconForFile.ToBitmap());
            }
            if (!imageList1.Images.ContainsKey("D_"))
            {
                IntPtr hSuccess = SHGetFileInfo(@"C:\Windows", 0,
                        ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
                if (hSuccess != IntPtr.Zero)
                {
                    iconForFile = Icon.FromHandle(shinfo.hIcon);
                    imageList1.Images.Add("D_", iconForFile.ToBitmap());
                    iconForFile.Dispose();
                }
            }

            // For each file in the c:\ directory, create a ListViewItem
            // and set the icon to the icon extracted from the file.
            try
            {
                ListViewItem.ListViewSubItem listviewsubitem;
                var directorys = dir.GetDirectories();
                foreach (var direc in directorys)
                {
                    item = new ListViewItem(direc.Name, 1);
                    listviewsubitem = new ListViewItem.ListViewSubItem();
                    listviewsubitem.Text = direc.LastWriteTime.ToString("yyyy/MM/dd HH:mm");
                    item.SubItems.Add(listviewsubitem);
                    listviewsubitem = new ListViewItem.ListViewSubItem();
                    listviewsubitem.Text = "ファイル フォルダー";
                    item.SubItems.Add(listviewsubitem);
                    listviewsubitem = new ListViewItem.ListViewSubItem();
                    listviewsubitem.Text = "";
                    item.SubItems.Add(listviewsubitem);

                    item.Tag = new DataTag(true, direc.FullName);
                    item.ImageKey = "D_";
                    listView2.Items.Add(item);
                }

                var files = dir.GetFiles();
                var tmp_icon_filename = "";
                foreach (var file in files)
                {
                    item = new ListViewItem(file.Name, 1);
                    listviewsubitem = new ListViewItem.ListViewSubItem();
                    listviewsubitem.Text = file.LastWriteTime.ToString("yyyy/MM/dd HH:mm");
                    item.SubItems.Add(listviewsubitem);
                    listviewsubitem = new ListViewItem.ListViewSubItem();
                    listviewsubitem.Text = file.Extension.Trim('.').ToUpper() + " ファイル";
                    item.SubItems.Add(listviewsubitem);
                    listviewsubitem = new ListViewItem.ListViewSubItem();
                    listviewsubitem.Text = String.Format("{0:N0} B", file.Length);
                    item.SubItems.Add(listviewsubitem);
                    item.Tag = new DataTag(false, file.FullName);

                    // TODO: アイコンの作成は設定で変更できるようにする
                    // TODO: フォルダのアイコンもね
                    if(file.Extension.ToLower() == ".exe" || file.Extension.ToLower() == ".lnk")
                    {
                        tmp_icon_filename = file.Name;
                    }
                    else
                    {
                        tmp_icon_filename = "";
                    }
                    if (!imageList1.Images.ContainsKey("F_" + tmp_icon_filename+ file.Extension))
                    {
                        IntPtr hSuccess = SHGetFileInfo(file.FullName, 0,
                            ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
                        if (hSuccess != IntPtr.Zero)
                        {
                            iconForFile = Icon.FromHandle(shinfo.hIcon);
                            imageList1.Images.Add("F_" + tmp_icon_filename + file.Extension, iconForFile.ToBitmap());
                            iconForFile.Dispose();
                        }
                    }
                    item.ImageKey = "F_" + tmp_icon_filename + file.Extension;
                    listView2.Items.Add(item);
                }
            }
            catch { MessageBox.Show("アクセスが拒否されました"); }

            listView2.EndUpdate();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var tmp = Directory.GetParent(current_path);
            if (tmp != null)
            {
                current_path = tmp.FullName;
                path_history.RemoveRange(history_index + 1, path_history.Count - 1 - history_index);
                path_history.Add(current_path);
                history_index++;
                ExtractAssociatedIconEx(current_path);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ExtractAssociatedIconEx(current_path);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(history_index != 0)
            {
                history_index--;
                current_path = path_history[history_index];
                ExtractAssociatedIconEx(current_path);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (history_index != path_history.Count-1)
            {
                history_index++;
                current_path = path_history[history_index];
                ExtractAssociatedIconEx(current_path);
            }
        }

        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 1)
            {
                var tmp = (DataTag)listView2.SelectedItems[0].Tag;
                if (tmp.is_folder)
                {
                    current_path = tmp.path;
                    // 移動
                    path_history.RemoveRange(history_index + 1, path_history.Count - 1 - history_index);
                    path_history.Add(current_path);
                    history_index++;
                    ExtractAssociatedIconEx(current_path);
                }
                else if (Path.GetExtension(tmp.path) == ".lnk")
                {
                    dynamic shell = null;   // IWshRuntimeLibrary.WshShell
                    dynamic lnk = null;     // IWshRuntimeLibrary.IWshShortcut
                    try
                    {
                        var type = Type.GetTypeFromProgID("WScript.Shell");
                        shell = Activator.CreateInstance(type);
                        lnk = shell.CreateShortcut(tmp.path);

                        if (string.IsNullOrEmpty(lnk.TargetPath))
                            return;

                        var result = new
                        {
                            lnk.Arguments,
                            lnk.Description,
                            lnk.FullName,
                            lnk.Hotkey,
                            lnk.IconLocation,
                            lnk.TargetPath,
                            lnk.WindowStyle,
                            lnk.WorkingDirectory
                        };

                        if (Directory.Exists(result.TargetPath.ToString())){

                            current_path = result.TargetPath.ToString();
                            // 移動
                            path_history.RemoveRange(history_index + 1, path_history.Count - 1 - history_index);
                            path_history.Add(current_path);
                            history_index++;
                            ExtractAssociatedIconEx(current_path);
                        }
                        else if (File.Exists(result.TargetPath.ToString()))
                        {
                            var app = new ProcessStartInfo();
                            app.FileName = result.TargetPath.ToString();
                            app.UseShellExecute = true;

                            Process.Start(app);
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("ファイルを開くことができません");
                    }
                    finally
                    {
                        if (lnk != null) Marshal.ReleaseComObject(lnk);
                        if (shell != null) Marshal.ReleaseComObject(shell);
                    }


                }
                else
                {
                    var app = new ProcessStartInfo();
                    app.FileName = tmp.path;
                    app.UseShellExecute = true;
                    app.WorkingDirectory = current_path;
                    Process.Start(app);
                }

            }
        }


        private void 新しいウィンドウで開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var items = listView2.SelectedItems;
            if (items.Count == 0)
            {
                main_window.Add(current_path);
            }
            else
            {
                foreach (var item in items)
                {
                    var tmp = ((ListViewItem)item).Tag as DataTag;
                    if (tmp.is_folder)
                    {
                        main_window.Add(tmp.path);
                    }
                }
            }
        }
        private void パスをコピーToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var items = listView2.SelectedItems;
            if (items.Count == 0)
            {
                Clipboard.SetText(current_path);
            }
            else
            {
                var str = new StringBuilder();
                for(int i=0; i<items.Count; ++i)
                {
                    var tmp = items[i].Tag as DataTag;
                    str.Append(tmp.path);
                    if (i == items.Count - 1)
                    {
                        break;
                    }
                    str.Append("\n");
                }
                Clipboard.SetText(str.ToString());
            }
        }

        private void ファイルを作成するToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var filename = "tmp_";
            for (int i = 0; i < 30000; ++i)
            {
                if(!File.Exists(Path.Combine(current_path, filename + i.ToString("D3"))))
                {
                    try
                    {
                        File.Create(Path.Combine(current_path, filename + i.ToString("D3")));
                        break;
                    } catch { }
                }
            }
            button4.PerformClick();
        }

        private void フォルダを作成するToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var filename = "dir_";
            for (int i = 0; i < 30000; ++i)
            {
                if (!Directory.Exists(Path.Combine(current_path, filename + i.ToString("D3"))))
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(current_path, filename + i.ToString("D3")));
                        break;
                    }
                    catch { }
                }
            }
            button4.PerformClick();
        }
        private void エクスプローラーで開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var items = listView2.SelectedItems;
            if (items.Count == 0)
            {
                var app = new ProcessStartInfo();
                app.FileName = "explorer";

                app.Arguments = current_path;
                Process.Start(app);
            }
            else
            {
                var tmp = items[0].Tag as DataTag;
                if (tmp.is_folder)
                {
                    var app = new ProcessStartInfo();
                    app.FileName = "explorer";

                    app.Arguments = tmp.path;
                    Process.Start(app);
                }
            }
        }
        private void 削除するToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var items = listView2.SelectedItems;
            if (items.Count == 0)
            {

            }
            else
            {
                var dr = MessageBox.Show("確認せずに削除しますか？", "ファイル削除", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Cancel)
                {
                    return;
                }
                // ファイルの削除
                var app = new ProcessStartInfo();
                app.FileName = "cmd";
                var str = new StringBuilder();
                str.Append("/c del ");
                if(dr == DialogResult.No)
                {
                    str.Append("/p ");
                }
                foreach(var item in listView2.SelectedItems)
                {
                    if (File.Exists(((DataTag)((ListViewItem)item).Tag).path))
                    {
                        str.Append("\"");
                        str.Append(((DataTag)((ListViewItem)item).Tag).path);
                        str.Append("\" ");
                    }
                }
                app.Arguments = str.ToString();
                var pr = Process.Start(app);
                pr.WaitForExit();

                // フォルダの削除
                app = new ProcessStartInfo();
                app.FileName = "cmd";
                str = new StringBuilder();
                str.Append("/c rd /s ");
                if (dr == DialogResult.Yes)
                {
                    str.Append("/q ");
                }
                foreach (var item in listView2.SelectedItems)
                {
                    if (Directory.Exists(((DataTag)((ListViewItem)item).Tag).path))
                    {
                        str.Append("\"");
                        str.Append(((DataTag)((ListViewItem)item).Tag).path);
                        str.Append("\" ");
                    }
                }
                app.Arguments = str.ToString();
                pr = Process.Start(app);
                pr.WaitForExit();

                ExtractAssociatedIconEx(current_path);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            main_window.Delete(guid);
        }

        private void listView2_MouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.XButton1)
            {
                button1.PerformClick();
            }
            else if(e.Button == MouseButtons.XButton2)
            {
                button2.PerformClick();
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (Directory.Exists(textBox1.Text))
                {
                    current_path = Path.GetFullPath(textBox1.Text);
                    // 移動
                    path_history.RemoveRange(history_index + 1, path_history.Count - 1 - history_index);
                    path_history.Add(current_path);
                    history_index++;
                    ExtractAssociatedIconEx(current_path);
                }
                else if (textBox1.Text.StartsWith("Box\\"))
                {
                    var tmppath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), textBox1.Text);
                    if (Directory.Exists(tmppath))
                    {
                        current_path = Path.GetFullPath(tmppath);
                        // 移動
                        path_history.RemoveRange(history_index + 1, path_history.Count - 1 - history_index);
                        path_history.Add(current_path);
                        history_index++;
                        ExtractAssociatedIconEx(current_path);
                    }
                    if (File.Exists(tmppath))
                    {
                        var app = new ProcessStartInfo();
                        app.FileName = textBox1.Text;
                        app.WorkingDirectory = current_path;
                        app.UseShellExecute = true;
                        try
                        {
                            Process.Start(app);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("実行に失敗しました");
                            textBox1.Text = current_path;
                        }
                    }
                }
                else if (textBox1.Text.StartsWith("Dropbox\\"))
                {
                    var tmppath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), textBox1.Text);
                    if (Directory.Exists(tmppath))
                    {
                        current_path = Path.GetFullPath(tmppath);
                        // 移動
                        path_history.RemoveRange(history_index + 1, path_history.Count - 1 - history_index);
                        path_history.Add(current_path);
                        history_index++;
                        ExtractAssociatedIconEx(current_path);
                    }
                }
                else
                {
                    var app = new ProcessStartInfo();
                    app.FileName = textBox1.Text;
                    app.WorkingDirectory = current_path;
                    app.UseShellExecute = true;
                    try
                    {
                        Process.Start(app);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("実行に失敗しました");
                        textBox1.Text = current_path;
                    }
                }
            }
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            textbox_is_focused_flag = false;
        }

        private void textBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (textbox_is_focused_flag == false)
            {
                textbox_is_focused_flag = true;
                textBox1.SelectAll();
            }
        }

        private void listView2_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Control) == Keys.Control)
            {
                if(e.KeyCode == Keys.C) {
                    if (listView2.SelectedItems.Count != 0)
                    {
                        var files = new StringCollection();
                        foreach (ListViewItem item in listView2.SelectedItems)
                        {
                            files.Add(((DataTag)item.Tag).path);
                        }
                        //クリップボードにコピーする
                        Clipboard.SetFileDropList(files);
                    }
                }
                if (e.KeyCode == Keys.V)
                {
                    if (Clipboard.ContainsFileDropList())
                    {
                        //データを取得する（取得できなかった時はnull）
                        var files = Clipboard.GetFileDropList();

                        if(files != null)
                        {
                            //取得したファイル名を列挙する
                            foreach (string fileName in files)
                            {

                                var app = new ProcessStartInfo();
                                app.FileName = "cmd";

                                app.WorkingDirectory = current_path;
                                app.Arguments = "/c xcopy \"" + fileName + "\" \"" + current_path + "\"";
                                app.UseShellExecute = true;
                                try
                                {
                                    var pro = Process.Start(app);
                                    pro.WaitForExit();
                                }
                                catch
                                {

                                }
                            }
                            button4.PerformClick();
                        }
                        
                    }
                }
            }

            if (e.KeyCode == Keys.Delete)
            {
                削除するToolStripMenuItem_Click(sender,e);
            }
            if (e.KeyCode == Keys.Enter)
            {
                listView2_DoubleClick(sender,e);
            }

        }

        private void listView2_DragEnter(object sender, DragEventArgs e)
        {
            //ドラッグされているデータがstring型か調べ、
            //そうであればドロップ効果をMoveにする
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Move;
            else
                //string型でなければ受け入れない
                e.Effect = DragDropEffects.None;
        }

        private void listView2_DragDrop(object sender, DragEventArgs e)
        {
            // ドラッグアンドドロップされた場合
            var pathes = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (var file_path in pathes)
            {
                var app = new ProcessStartInfo();
                app.FileName = "cmd";
                app.WorkingDirectory = current_path;
                app.UseShellExecute = true;
                app.Arguments = "/c move \"" + file_path + "\" \"" + current_path + "\"";
                try
                {
                    var pr = Process.Start(app);
                    pr.WaitForExit();
                    button4.PerformClick();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("実行に失敗しました");
                }
            }
            return;
        }

        private void listView2_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (listView2.SelectedItems.Count != 0)
            {
                // var files = new StringCollection();
                var files = new string[listView2.SelectedItems.Count];
                for (int i = 0; i < listView2.SelectedItems.Count; i++)
                {
                    files[i] = ((DataTag)listView2.SelectedItems[i].Tag).path;
                }
                //ドラッグアンドドロップする
                ListView lbx = (ListView)sender;
                DragDropEffects dde = lbx.DoDragDrop(new DataObject(DataFormats.FileDrop, files), DragDropEffects.Move);
                ExtractAssociatedIconEx(current_path);
            }
        }

        private void リネームToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(listView2.SelectedItems.Count == 1)
            {
                var tmp = (DataTag)listView2.SelectedItems[0].Tag;
                var rename = new Rename(tmp.path);
                rename.ShowDialog();
                if (rename.Result)
                {
                    var app = new ProcessStartInfo();
                    app.FileName = "cmd";
                    app.WorkingDirectory = current_path;
                    app.UseShellExecute = true;
                    app.Arguments = "/c move \"" + tmp.path + "\" \"" + rename.Result_Text + "\"";
                    try
                    {
                        var pr = Process.Start(app);
                        pr.WaitForExit();
                        button4.PerformClick();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }
    }
}
