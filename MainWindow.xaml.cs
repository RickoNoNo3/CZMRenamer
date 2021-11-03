using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;


namespace CZMRenamer {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private readonly DispatcherTimer Timer = new DispatcherTimer();
        public ObservableCollection<ListItem> Items { get; set; }
        private string FileDir;
        private bool ProcessStopping = false;
        private Thread GithubPageThread;

        public MainWindow() {
            InitializeComponent();
            Items = new ObservableCollection<ListItem>();
            itemsControl.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(".") {
                Source = Items,
                Mode = BindingMode.OneWay
            });

            // 每1秒刷新Items状态
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += Timer_Tick;
            Timer.Start();

            // 定时10min后自动打开Github Page
            GithubPageThread = new Thread(() => {
                for (int i = 0; i < 600; i++) {
                    if (ProcessStopping)
                        return;
                    Thread.Sleep(1000);
                }
                Process.Start("https://github.com/RickoNoNo3/CZMRenamer");
            });
            GithubPageThread.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            ProcessStopping = true;
            Timer.Stop();
        }

        private string GenerateFileNameWithoutExt(string name) {
            string res = "";
            if (this.prefix.Text != "") {
                res += this.prefix.Text + this.separator.Text;
            }
            res += name;
            if (this.suffix.Text != "") {
                res += this.separator.Text + this.suffix.Text;
            }
            return res;
        }

        private string GenerateFileName(string name, string ext) {
            if (ext.StartsWith(".")) {
                ext = ext.Substring(1);
            }
            return GenerateFileNameWithoutExt(name) + '.' + ext;
        }

        private void RefreshItems() {
            if (!Directory.Exists(this.FileDir))
                return;
            List<string> files = new List<string>(Directory.GetFiles(this.FileDir));
            for (int i = 0; i < files.Count; i++) {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            for (int i = 0; i < Items.Count; i++) {
                if (files.Contains(GenerateFileNameWithoutExt(Items[i].Name))) {
                    Items[i].Exists = true;
                } else {
                    Items[i].Exists = false;
                }
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e) {
            var dialog = new Microsoft.Win32.OpenFileDialog {
                Title = "选择名单文件",
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "文本文件(.txt)|*.txt"
            };
            if (dialog.ShowDialog() == true) {
                var fileName = dialog.FileName;
                // 检查文件
                if (!File.Exists(fileName)) {
                    MessageBox.Show("文件不存在!");
                    return;
                }
                // 保存路径
                this.FileDir = Path.GetDirectoryName(fileName);
                // 读取并处理每行
                var lines = File.ReadAllLines(fileName);
                // 使用临时items处理，为了避免数据错误影响界面显示
                List<ListItem> items = new List<ListItem>();
                for (int i = 0; i < lines.Length; i++) {
                    var line = lines[i].Trim();
                    if (line != null && line != "") {
                        var tItem = new ListItem() { Name = line };
                        if (!items.Contains(tItem)) {
                            items.Add(tItem);
                        }
                    }
                }
                // 然后用可产生事件的方式更新this.Items
                this.Items.Clear();
                for (int i = 0; i < items.Count; i++) {
                    this.Items.Add(items[i]);
                }
                RefreshItems();
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show(
                "1. 将名单文件(.txt)放在空文件夹下，名单项按行分隔，然后【选择位置】。\n" +
                "2. 将要命名的文件拖动到窗口中相应的名单项标签上，即可在名单文件所在位置生成指定命名格式的文件。\n" +
                "3. 白色名单项为未生成；绿色名单项为已生成，再次拖至其上可替换。\n" +
                "\n" +
                "注：最终文件名 = [前缀 + 分隔符] + 名单项 + [分隔符 + 后缀]\n" +
                "　　扩展名保留被拖动文件的原始扩展名，请勿在后缀中填写扩展名。\n",
                "帮助",
                MessageBoxButton.OK,
                MessageBoxImage.None,
                MessageBoxResult.OK,
                MessageBoxOptions.ServiceNotification);
        }

        private void Prefix_TextChanged(object sender, TextChangedEventArgs e) {
            RefreshItems();
        }

        private void Separator_TextChanged(object sender, TextChangedEventArgs e) {
            RefreshItems();
        }

        private void Suffix_TextChanged(object sender, TextChangedEventArgs e) {
            RefreshItems();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            RefreshItems();
        }

        private void Label_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                (sender as Label).BorderBrush = Brushes.GreenYellow;
            }
        }

        private void Label_DragLeave(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                (sender as Label).BorderBrush = Brushes.Transparent;
            }
        }

        private void Label_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                (sender as Label).BorderBrush = Brushes.Transparent;
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (filePaths.Length < 1 || !File.Exists(filePaths[0])) {
                    MessageBox.Show("未拖动有效文件");
                } else {
                    string filePath = filePaths[0];
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string fileExt = Path.GetExtension(filePath);
                    int index = Items.IndexOf(new ListItem() { Name = (sender as Label).Content.ToString() });
                    if (index == -1) {
                        MessageBox.Show("无法找到有效文件名");
                    } else {
                        string finalFilePath = Path.Combine(this.FileDir, GenerateFileName(Items[index].Name, fileExt));
                        try {
                            if (File.Exists(finalFilePath)) {
                                var res = MessageBox.Show($"文件\"{finalFilePath}\"已存在，是否替换？", "文件已存在", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, MessageBoxOptions.ServiceNotification);
                                if (res == MessageBoxResult.Yes) {
                                    // 这个过程可以避免同一文件自己替换自己出现的BUG，具体自己推导
                                    // 为什么不直接判断两个FilePath相等？因为可能有软/硬/符号链接。。
                                    File.Copy(filePath, finalFilePath + ".bak");
                                    File.Delete(finalFilePath);
                                    File.Move(finalFilePath + ".bak", finalFilePath);
                                }
                            } else {
                                File.Copy(filePath, finalFilePath);
                            }
                        } catch (Exception ex) {
                            MessageBox.Show($"无法将文件复制到\"{finalFilePath}\"，出现错误：\n{ex.Message}");
                        }
                    }
                }
            }
        }

        private void GithubPage_MouseDown(object sender, MouseButtonEventArgs e) {
            Process.Start("https://github.com/RickoNoNo3/CZMRenamer");
        }
    }
}
