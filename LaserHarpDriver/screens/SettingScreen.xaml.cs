using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LaserHarpDriver.screens
{
    /// <summary>
    /// settingscreen.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingScreen : Window
    {
        //リストアップされる音楽データ
        ObservableCollection<DicJson> DicItem = new ObservableCollection<DicJson>();
        ObservableCollection<DicShow> DicItemImg = new ObservableCollection<DicShow>();
        //外部参照を有効にするファイルパス情報、MainWindowから引き継げるようにする
        //public retValue File_path { get { return new retValue { filepath = DicItem[AllSound.SelectedIndex].filepath, witch = (Radio_which_sound.IsChecked == true) ? true :false};  set { new retValue { filepath = DicItem[AllSound.SelectedIndex].filepath, witch = (Radio_which_sound.IsChecked == true) ? true : false }; }
        public retValue retvalue
        {
            get
            {
                return new retValue
                {
                    witch = (Radio_which_sound.IsChecked == true) ? true : false,
                    filepath = (Radio_which_sound.IsChecked == true) ? DicItem[AllSound.SelectedIndex].filepath : DicItemImg[AllImage.SelectedIndex].filepath
                };
            }
            set { retvalue = value; }
        }
        public SettingScreen(Window owner)
        {
            InitializeComponent();

            this.Owner = owner;
            //引数から親のウィンドウを設定する

            DicItem = Backcode.DicRead(true);
            AllSound.ItemsSource = DicItem;
            Radio_which_sound.IsChecked = true;

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen; //起動時の表示位置を親画面の中央に合わせる
        }
        private void test_Play_Click(object sender, RoutedEventArgs e)
        {
            //テスト再生
            //スライダーの値を反映してから再生、、さては再生中に音量変更できないな？、、、許して
            //if(null!= AllSound.SelectedIndex)
            {
                Media_test.Source = new Uri("resource/sounds/"+DicItem[AllSound.SelectedIndex].filepath, UriKind.RelativeOrAbsolute);
                Media_test.Volume = Play_bar.Value / 100;
                Media_test.Stop();
                Media_test.Play();
            }

        }

        private void return_filepath_Click(object sender, RoutedEventArgs e)
        {
            if (AllSound.SelectedIndex >= 0 && Radio_which_sound.IsChecked == true)
                DialogResult = true;
            else if (AllImage.SelectedIndex >= 1 && Radio_which_image.IsChecked == true)
                DialogResult = true;
            else
                MessageBox.Show("変更先を選択してください\nもし、選択しない場合はウィンドウを閉じて、どうぞ","不正な操作");
        }

        private void new_file_Click(object sender, RoutedEventArgs e)
        {
            int result = Backcode.AdditionalNewsound(DicItem, ((bool)Radio_which_sound.IsChecked ? true : false));
            if (result == 0)
            {
                //MessageBox.Show("追加されました");
                if (Radio_which_sound.IsChecked == true)//ラジオボタンコントロールがnull許容なのめんどくせえ！
                {
                    DicItem = Backcode.DicRead(true);
                    AllSound.ItemsSource = DicItem;
                }else if(Radio_which_image.IsChecked == true)
                {
                    DicItem = Backcode.DicRead(false);
                    List_Image_Load(sender, e);
                }
            }
            else
            {
                //MessageBox.Show("追加されませんでした");
            }
        }

        private void Radio_which_image_Checked(object sender, RoutedEventArgs e)
        {
            List_Image_Load(sender,e);
            AllSound.ItemsSource = DicItem;
            AllSound.Margin = new Thickness(1000, 75, 200, 0);
            AllImage.Margin = new Thickness(0, 75, 200, 0);
        }

        private void Radio_which_sound_Checked(object sender, RoutedEventArgs e)
        {
            DicItem = Backcode.DicRead(true);
            AllSound.ItemsSource = DicItem;
            AllImage.Margin = new Thickness(1000, 75, 200, 0);
            AllSound.Margin = new Thickness(0, 75, 200, 0);
        }
        private void List_Image_Load(object sender, RoutedEventArgs e)
        {
            DicItemImg.Clear();
            DicItem = Backcode.DicRead(false);
            for (int i = 0; i < DicItem.Count; i++)//Dicitemのファイルパスからimagesourceに変換してる
            {
                ImageSource img;
                using (var stream = System.IO.File.OpenRead("./resource/images/" + DicItem[i].filepath)){
                    img = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
                DicItemImg.Add(new DicShow { filepath = DicItem[i].filepath, imageSource = img});
            }
            AllImage.ItemsSource = DicItemImg;
            // turndicJsons.Add(new DicJson {filepath = dicitem.filepath, hash = dicitem.hash });
        }

        private void test_Stop_Click(object sender, RoutedEventArgs e)
        {
            Media_test.Stop();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Media_test.Stop();
        }

    }
}
