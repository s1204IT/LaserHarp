using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO.Ports;
using System.Windows.Controls.Primitives;

namespace LaserHarpDriver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //ListViewにBindするためにObservableCollection使用
        ObservableCollection<ListedItems> SoundItem = new ObservableCollection<ListedItems>();
        int Pri = 1;
        SerialPort serialPort = new SerialPort();

        public MainWindow()
        {
            InitializeComponent();
            ListItem_Load();
            ItemvolumeSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(ItemvolumeSlider_DragCompleted), true);
            PriNum.Content = Pri;
        }

        private void ListItem_Load()
        {
            try
            {
                SoundItem = Backcode.Itemread(Pri);
                SoundListView.ItemsSource = SoundItem;
                var players = new[] { Player1, Player2, Player3, Player4, Player5, Player6 };
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].Source = new Uri($"./resource/sounds/{SoundItem[i].filepath}", UriKind.RelativeOrAbsolute);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //debugコードどける
        private void ChangepassBT_Click(object sender, RoutedEventArgs e)
        {//クリックされた時の選択されているアイテムの情報から何番目の配列かを読み取り置き換え
           
            if (SoundListView.SelectedIndex < 0)
            {
                MessageBox.Show("変更する音楽を選択してください", "選択エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            retValue retValue_recive = SettingDialog();
            if (retValue_recive == null)
                return;
            if (retValue_recive.witch)
            {
                SoundItem[SoundListView.SelectedIndex].filepath = retValue_recive.filepath;
                SoundItem[SoundListView.SelectedIndex].name = retValue_recive.filepath;
            }
            else
            {
                using (var stream = System.IO.File.OpenRead("./resource/images/" + retValue_recive.filepath))
                { SoundItem[SoundListView.SelectedIndex].imagepath = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad); }
                SoundItem[SoundListView.SelectedIndex].imagepath_string = retValue_recive.filepath;
            }
            Backcode.Itemsave(SoundItem, Pri);
            ListItem_Load();
        }
        private retValue SettingDialog()
        {
            //dialogウィンドウを定義
            var dialog = new screens.SettingScreen(this);
            return ((bool)dialog.ShowDialog()) ? dialog.retvalue : null;
        }


        private void PortBT_DropDownOpened(object sender, EventArgs e)
        {
            PortBT.Items.Clear();
            foreach (var ENport in SerialPort.GetPortNames())
            {
                PortBT.Items.Add(ENport);
            }
            return;
        }
        //修正、backcodeに転送
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            //シリアルポートの定義
            //シリアルデータ受け取り時のイベントを定義
            try
            {
                serialPort.Close();
                serialPort.PortName = PortBT.SelectedItem.ToString();
                serialPort.BaudRate = 9600;
                serialPort.DataBits = 8;
                serialPort.Parity = Parity.None;
                serialPort.StopBits = StopBits.One;
                serialPort.WriteTimeout = 4000;
                serialPort.ReadTimeout = 4000;
                serialPort.Encoding = Encoding.UTF8;
                serialPort.Open();
                serialPort.DataReceived += new SerialDataReceivedEventHandler(portReceive); // DataReceivedイベントを設定

                MessageBox.Show("Data was sent!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return;
        }
        private void portReceive(object sender, SerialDataReceivedEventArgs e)
        {
            // 受信したデータをバイト配列として読み込む
            int bytesToRead = serialPort.BytesToRead;
            byte[] buffer = new byte[bytesToRead];
            serialPort.Read(buffer, 0, bytesToRead);

            // バイト配列をUTF-8文字列として解釈
            string receivedData = Encoding.UTF8.GetString(buffer);

            // UTF-8で解釈できない場合、ASCIIとして解釈
            if (string.IsNullOrEmpty(receivedData) || !IsValidUtf8(buffer))
            {
                receivedData = Encoding.ASCII.GetString(buffer);
            }

            // 受信したデータに応じてMediaElementを再生
            switch (receivedData)
            {
                case "A":
                case "B":
                case "C":
                case "D":
                case "E":
                case "F":
                    int playerIndex = receivedData[0] - 'A';
                    var players = new[] { Player1, Player2, Player3, Player4, Player5, Player6 };
                    players[playerIndex].Play();
                    break;
                default:
                    // その他の文字は無視する
                    break;
            }
        }

        // UTF-8バイト配列が有効か確認するメソッド
        private bool IsValidUtf8(byte[] bytes)
        {
            try
            {
                // バイト配列をUTF-8としてデコードしてみる
                Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch (DecoderFallbackException)
            {
                // デコードできない場合は無効
                return false;
            }
        }
        private void NextBT_Click(object sender, RoutedEventArgs e)
        {
            if(Pri < 6)
            {
                Backcode.Itemsave(SoundItem, Pri);
                Pri++;
                ListItem_Load();
            }
            PriNum.Content = Pri;
        }

        private void BackBT_Click(object sender, RoutedEventArgs e)
        {
            if (Pri > 1)
            {
                Backcode.Itemsave(SoundItem, Pri);
                Pri--;
                ListItem_Load();
            }
            PriNum.Content = Pri;
        }

        private void SoundListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var players = new[] { Player1, Player2, Player3, Player4, Player5, Player6 };
            if (SoundListView.SelectedIndex >= 0 && SoundListView.SelectedIndex < players.Length)
            {
                ItemvolumeSlider.Value = players[SoundListView.SelectedIndex].Volume * 100;
            }
        }

        private void commandBox_KeyDown(object sender, KeyEventArgs e)
        {
            //MessageBox.Show("Hello", e.Key.ToString());
            var players = new[] { Player1, Player2, Player3, Player4, Player5, Player6 };
            if (e.Key >= Key.D1 && e.Key <= Key.D6)
            {
                int index = e.Key - Key.D1;
                players[index].Stop();
                players[index].Play();
            }
        }
        private void ItemvolumeSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //選択しているアイテムに割り当てられているmediaElementのボリュームをスライダーの値に変更
            var players = new[] { Player1, Player2, Player3, Player4, Player5, Player6 };
            if (SoundListView.SelectedIndex >= 0 && SoundListView.SelectedIndex < players.Length)
                players[SoundListView.SelectedIndex].Volume = ItemvolumeSlider.Value / 100;
            //音量をアイテム側に保存
            SoundItem[SoundListView.SelectedIndex].volume = ItemvolumeSlider.Value / 100;
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //ページが閉じたとき、つまり終了処理、、だね
            Backcode.Itemsave(SoundItem, Pri);
        }

        private void stopBT_Click(object sender, RoutedEventArgs e)
        {
            var players = new[] { Player1, Player2, Player3, Player4, Player5, Player6 };
            if (SoundListView.SelectedIndex >= 0 && SoundListView.SelectedIndex < players.Length)
            {
                players[SoundListView.SelectedIndex].Stop();
            }
        }

        private void SettingBT_Click(object sender, RoutedEventArgs e)
        {
            SoundItem[SoundListView.SelectedIndex].name = settingBox.Text;
            Backcode.Itemsave(SoundItem,Pri);
            ListItem_Load();
            
        }
    }
}