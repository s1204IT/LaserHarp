using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LaserHarpDriver
{

    public class ListedItems//List化させるアイテムたち
    {
        public required string name { get; set; }
        public required string filepath { get; set; }
        public double volume { get; set; }
        public required ImageSource imagepath { get; set; }
        public required string imagepath_string { get; set; }//アイテムのファイルソースを保存する場所
    }
    public class ListedJson//Jsonで扱えるアイテム形式
    {
        public required string name { get; set; }
        public required string filepath { get; set; }
        public double volume { get; set; }
        public required string imagepath { get; set; }
    }
    public class DicJson//json内の辞書
    {
        public required string filepath { get; set; }
        public required string hash { get; set; }
    }
    public class DicShow//Listviewに表示する用のデータ
    {
        public required string filepath { get; set; }
        public required ImageSource imageSource { get; set; }
    }
    public class retValue
    {
        public bool witch { get; set; }//trueなら音楽,falseなら画像
        public required string filepath { get; set; }
    }
    static public class Backcode
    {   /// <summary>
        ///
        ///true is music,false is img
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static public ObservableCollection<DicJson> DicRead(bool type)//音楽データ一覧の読み込み
        {
            ObservableCollection<DicJson> turndicJsons = new ObservableCollection<DicJson>();
            StreamReader sr;
            sr = new StreamReader(type ? "./resource/indexs/dic.json" : "./resource/indexs/img.json", Encoding.UTF8);
            string datas = sr.ReadToEnd();
            sr.Close();
            //目的のプリセット番号のjsonファイルを読み込む
            DicJson[] json = JsonSerializer.Deserialize<DicJson[]>(datas);
            foreach (DicJson dicitem in json)
            {
                turndicJsons.Add(new DicJson { filepath = dicitem.filepath, hash = dicitem.hash });
            }
            return turndicJsons;
        }


        static public ObservableCollection<ListedItems> Itemread(int Pri)//Jsonデータからアイテムリストに読み出し
        {//アイテムをjsonから読み込む
            ObservableCollection<ListedItems> collection_ListedItem = new ObservableCollection<ListedItems>();
            ListedJson[] listedItems = new ListedJson[6];
            StreamReader sr = new StreamReader("./resource/indexs/index" + Pri + ".json", Encoding.UTF8);
            string datas = sr.ReadToEnd();
            sr.Close();
            //目的のプリセット番号のjsonファイルを読み込む
            ListedJson[] json = new ListedJson[6];
            json = JsonSerializer.Deserialize<ListedJson[]>(datas);
            //jsonデータをJsonで使えるアイテム配列形式に変換

            ImageSource img;
            foreach (ListedJson item in json)
            {
                using (var stream = System.IO.File.OpenRead("./resource/images/" + item.imagepath))
                { img = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad); }
                //imagesourceにファイルパスから変換
                collection_ListedItem.Add(new ListedItems
                {
                    name = item.name,
                    filepath = item.filepath,
                    volume = item.volume,
                    imagepath = img,
                    imagepath_string = item.imagepath,
                });
                //リスト化させるアイテム型にjson形式から変換
            }
            sr.Close();
            return collection_ListedItem;
        }
        static public void Itemsave(ObservableCollection<ListedItems> items, int Pri)
        //書き込むアイテム,書き込み先のプリセットNo
        {
            ObservableCollection<ListedJson> write_Items = new ObservableCollection<ListedJson>();
            foreach (ListedItems item in items)//書き込める形のデータ形式に変換(Uri書き込めないのつらい)
            {
                write_Items.Add(new ListedJson
                {
                    name = item.name,
                    imagepath = item.imagepath_string,
                    filepath = item.filepath.ToString(),
                    volume = item.volume
                });
            }
            var json = JsonSerializer.Serialize(write_Items);
            //json形式のstringデータに変換
            File.WriteAllText("./resource/indexs/index" + Pri + ".json", json);
            //プリセットのインデックスに非同期書き込み

        }
        /// <summary>
        /// 新しいファイルを追加します。(音楽データ)
        /// もし、返りのintデータが0なら正常に追加されています。
        /// filetypeはtrueなら音楽データ、falseは画像データ
        /// </summary>
        /// <returns></returns>
        static public int AdditionalNewsound(ObservableCollection<DicJson> validate_target, bool filetype)//現在
        //ファイルを新たに追加
        //返り値?の場合エラー表記
        {
            string jsonpath;
            string resourcePath;
            //ファイルの選択はwindows標準のダイアログから
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = filetype ? "音楽データを選択" : "画像データを選択";
            ofd.Filter = filetype ? "音楽ファイル (*.mp3;*.wav;*.wma;*.aac;*.flac;*.ogg;*.m4a;*.aiff;*.alac)|*.mp3;*.wav;*.wma;*.aac;*.flac;*.ogg;*.m4a;*.aiff;*.alac" : "画像ファイル (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.ico)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.ico|すべてのファイル (*.*)|*.*";
            jsonpath = filetype ? "./resource/indexs/dic.json" : "./resource/indexs/img.json";
            resourcePath = filetype ? "./resource/sounds/" : "./resource/images/";
            ofd.InitialDirectory = @"C:\";
            ofd.Multiselect = false;
            ofd.FilterIndex = 1;
            MessageBox.Show(ofd.FileName.ToString());
            bool? selected_result = ofd.ShowDialog();
            if (selected_result == true)
            {
                //ハッシュ処理関係
                int headerSize = 100;//ヘッダのサイズ(バイト単位)
                string validate_hash;
                //ファイル重複性チェック関係
                bool flag_hash = false;
                bool flag_head = false;
                string hash_collision = "0";
                string name_collision = "0";
                int index_num = 0;
                MessageBoxResult result;


                using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))//ファイルを読み取り専用で使用
                {
                    fs.Seek(headerSize, SeekOrigin.Begin);//seekメソッドでファイルのヘッダ部分をスキップします。
                                                          //バイト分だけ読み取り位置を移動、つまり今回の場合は100バイト
                    using (MD5 md5 = MD5.Create())//MD5インスタンスを生成、
                    {
                        byte[] hash = md5.ComputeHash(fs);//シークしたfsファイルストリームをmd5でハッシュ値計算
                        StringBuilder hashString = new StringBuilder();
                        foreach (byte b in hash)
                        {
                            hashString.Append(b.ToString("x2"));//16進数のハッシュ数値をストリング形式でビルダーに追加
                        }
                        validate_hash = hashString.ToString();
                    }
                }
                //ファイルのデータ部と名前の重複をチェック
                //この場合は確認だけでいい
                foreach (DicJson valid_item in validate_target)
                {
                    if (validate_hash == valid_item.hash && Path.GetFileName(ofd.FileName) == valid_item.filepath)//完全重複により終了
                    { flag_hash = true; flag_head = true; break; }
                    else if (Path.GetFileName(ofd.FileName) == valid_item.filepath)//ヘッダ重複により終了
                    { flag_hash = false; flag_head = true; name_collision = valid_item.filepath; hash_collision = valid_item.hash; break; }//ヘッダしかかぶってないのでハッシュはオフにする
                    else if (validate_hash == valid_item.hash)
                    { flag_hash = true; }//データ部の重複確認だけ
                }
                if (flag_hash && flag_head)
                {
                    MessageBox.Show("そのファイルはすでに存在します。", "重複検知");
                    return 1;//追加されなかったためリロード処理を行わない
                }
                else if (flag_head)
                {
                    result = MessageBox.Show("同じファイル名が存在します。" +
                        "ファイルを置き換えますか?(プリセットに登録されている場合はプリセット側も置き換えられます)",
                        "重複検知", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.OK)
                    {
                        foreach (DicJson dicitem_hash in validate_target)
                        {
                            if (dicitem_hash.filepath == name_collision)//名前が重複している配列を探索
                            {
                                validate_target[index_num].hash = hash_collision;//発見したときハッシュ値(データ部)を入れ替えてファイルを上書き
                                File.Copy(ofd.FileName, resourcePath + Path.GetFileName(ofd.FileName), true);
                            }
                            index_num++;
                        }
                    }
                    return 1;//追加されなかったためリロード処理を行わない
                }
                else if (flag_hash)
                {
                    result = MessageBox.Show("すでに重複している以下のファイルが存在します。" +
                        "そのまま追加しますか？",
                        "重複検知", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.OK)
                    {
                        File.Copy(@ofd.FileName, @resourcePath + Path.GetFileName(ofd.FileName), true);//選択されたときの処理
                        validate_target.Add(new DicJson { filepath = Path.GetFileName(ofd.FileName), hash = validate_hash });
                    }
                    return 1;//追加されなかったためリロード処理を行わない
                    //名前は違えど同じ内容のファイルがあるけどいい？という
                }
                else
                {//通常通り追加
                    File.Copy(@ofd.FileName, @resourcePath + Path.GetFileName(ofd.FileName), true);//選択されたときの処理
                    validate_target.Add(new DicJson { filepath = Path.GetFileName(ofd.FileName), hash = validate_hash });
                }
                MessageBox.Show(Path.GetFileName(ofd.FileName) + "\n" + validate_target[0].ToString());
                var json = JsonSerializer.Serialize(validate_target);
                File.WriteAllText(jsonpath, json);
                return 0;

            }
            else
            {
                return 1;//選択されなかった時の処理
            }
        }

    }

}

