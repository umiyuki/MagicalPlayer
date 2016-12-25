// Project: Meta.Vlc (https://github.com/higankanshi/Meta.Vlc)
// Filename: MainWindow.xaml.cs
// Version: 20160404

using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

public class JsonComment
{
    public string time;
    public string comment;
    //public string date;
    //public string _id;
}

namespace Meta.Vlc.Wpf.Sample
{
    public partial class MainWindow : Window
    {
        //VlcPlayer Player = null; //uncomment if adding the player dynamically or use other control to render video

        List<Label> showingCommentList = new List<Label>(); //表示中のコメントのLabelのList
        Dictionary<long, List<string>> commentDictionary = new Dictionary<long, List<string>>();    //取得したコメントを格納する。longが秒数でList<string>がコメント文字列

        #region --- Initialization ---

        DispatcherTimer dispatcherTimer;
        public MainWindow()
        {
            InitializeComponent();
            //uncomment if adding the player dynamically
            /*
            Player = new VlcPlayer();
            Player.SetValue(Canvas.ZIndexProperty, -1);
            LayoutParent.Children.Add(Player);
            */

            //uncomment if you use Image or ThreadSeparatedImage to render video
            /*
            Player.Initialize(@"..\..\libvlc", new string[] { "-I", "dummy", "--ignore-config", "--no-video-title" });
            Player.VideoSourceChanged += PlayerOnVideoSourceChanged;
            */

            infoLabel.Content = "";

            //ボリュームを保存値から読み込み
            volumeSlider.Value = Properties.Settings.Default.SaveVolume;

            //updateタイマー作成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 16);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Start();

            //コメント取得
            Task getCommentTask = getComment();
            //getCommentTask.Wait();

            //前回のパスを再生
            string lastPath = Properties.Settings.Default.SavePath;
            if (lastPath != "")
            {
                //string path = System.IO.Path.GetDirectoryName(openfiles.FileName);
                //path = System.IO.Path.GetDirectoryName(path + "/../../");
                //Console.Write(path);
                //Player.Stop();
                Player.LoadMedia("dvdsimple:///" + lastPath);
                //Thread.Sleep(1000);
                //Player.Play();
                //Properties.Settings.Default.SavePath = path;
                //UIの各種ボタンを有効化
                playButton.IsEnabled = true;
                pauseButton.IsEnabled = true;
                commentButton.IsEnabled = true;
            }

        }

        //コメント取得
        private async Task getComment()
        {
            //コメント取得中
            infoLabel.Content = "コメント取得中...";
            string result = await getrequest();
            if (result == null)
            {
                infoLabel.Content = "コメント取得に失敗しました";
            }
            else
            {
                commentDictionary.Clear();  //コメントクリア
                var deserialized = JsonConvert.DeserializeObject<List<JsonComment>>(result);
                if (deserialized != null)
                {

                    for (int i = 0; i < deserialized.Count; i++)
                    {
                        JsonComment jsonComment = deserialized[i];
                        long time = 0;
                        if (!long.TryParse(jsonComment.time, out time))//timeの文字列をパース
                        {
                            //失敗
                            continue;
                        }

                        List<string> newCommentList = null;
                        if (commentDictionary.TryGetValue(time, out newCommentList))//すでにある
                        {
                            newCommentList.Add(jsonComment.comment);
                        }
                        else//まだない
                        {
                            newCommentList = new List<string>();
                            newCommentList.Add(jsonComment.comment);
                            commentDictionary.Add(time, newCommentList);
                        }
                    }

                    infoLabel.Content = "コメント取得成功！(" + deserialized.Count + "件)";
                }
                else
                {
                    infoLabel.Content = "コメント取得成功！(0件)";
                }
            }
            await Task.Delay(3000);
            infoLabel.Content = "";
        }

        //コメント取得タスク
        //親となる呼び出しメソッドには非同期メソッドである必要があります(asyncが必要)
        private async Task<string> getrequest()
        {
            var client = new HttpClient();
            var response = new HttpResponseMessage();

            string result = null; ;
            //awaitが前に必要、非同期処理であるという目印になります
            try
            {
                response = await client.GetAsync("http://soysoftware.net:5902");
                if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsStringAsync();
                }
            }
            catch(Exception e)
            {
                Console.Write("エラー："+e.Message + e.StackTrace);
            }

            return result;
        }

        //コメント送信
        private async Task postComment(string date, string time, string comment)
        {
            //コメント取得中
            //infoLabel.Content = "コメント取得中...";
            string result = await postrequest(date,time,comment);
            if (result == null)
            {
                infoLabel.Content = "コメント送信に失敗しました";
            }
            else
            {
                //infoLabel.Content = "コメント取得成功！";
                //commentDictionary.Clear();  //コメントクリア
                //TODO
            }
            await Task.Delay(3000);
            infoLabel.Content = "";
        }

        //コメント送信タスク
        private async Task<string> postrequest(string date, string time, string comment)
        {
            var client = new HttpClient();
            var response = new HttpResponseMessage();
            var video = "rebellion";
            var content = new FormUrlEncodedContent(new SortedDictionary<string, string>
            {
                //送り付けたいことを書く
                { "video", video},    //動画の種類（コレクション名にする）
                //{ "date" , date },          //日時
                { "time" , time },          //動画時間
                { "comment", comment },     //コメント
            });

            string result = null;
            try{
                response = await client.PostAsync("http://soysoftware.net:5902", content);
                result = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                Console.Write("エラー：" + e.Message + e.StackTrace);
            }

            return result;
        }

        long lastSeconds = -1;
        //update(16ms毎に実行　1/60秒）
        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            for (int i= showingCommentList.Count -1; i>=0; i--)
            {
                var comment = showingCommentList[i];
                int commentLength = ((string)comment.Content).Length;
                double marginLeft = comment.Margin.Left - comment.FontSize * 0.15f;
                double commentRight = commentGrid.ActualWidth - marginLeft - (comment.FontSize * commentLength);
                if (commentRight > commentGrid.ActualWidth)//左へ過ぎ去ったら削除
                {
                    commentGrid.Children.Remove(comment);
                    showingCommentList.RemoveAt(i);
                    //Console.WriteLine("削除");
                }
                else
                {
                    
                    comment.Margin = new Thickness(marginLeft, comment.Margin.Top, commentRight, comment.Margin.Bottom);
                }
            }

            if (Player.State == Interop.Media.MediaState.Playing) //再生中で
            {
                //読み込んだコメントを表示
                long newSeconds = (long)Math.Floor(Player.Time.TotalSeconds);
                if (newSeconds > lastSeconds)
                {
                    List<string> commentList = null;
                    if (commentDictionary.TryGetValue(newSeconds, out commentList))
                    {
                        for (int i = 0; i < commentList.Count; i++)
                        {
                            showComment(commentList[i]);
                        }
                    }
                }
                lastSeconds = newSeconds;
            }
        }

        //uncomment if you use Image or ThreadSeparatedImage to render video
        /*
        private void PlayerOnVideoSourceChanged(object sender, VideoSourceChangedEventArgs videoSourceChangedEventArgs)
        {
            DisplayImage.Dispatcher.BeginInvoke(new Action(() =>
            {
                DisplayImage.Source = videoSourceChangedEventArgs.NewVideoSource;
            }));
        }
        */

        #endregion --- Initialization ---

        #region --- Cleanup ---

        protected override void OnClosing(CancelEventArgs e)
        {
            //タイマー停止
            dispatcherTimer.Stop();

            //ボリュームを保存
            Properties.Settings.Default.SaveVolume = volumeSlider.Value;
            Properties.Settings.Default.Save();

            //開放
            Player.Dispose();
            ApiManager.ReleaseAll();
            base.OnClosing(e);
        }

        #endregion --- Cleanup ---

        #region --- Events ---

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var openfiles = new OpenFileDialog();
            openfiles.Title = "DVDドライブの中のVTS_01_0.IFOファイルを選択してください";
            if (openfiles.ShowDialog() == true)
            {
                string path = System.IO.Path.GetDirectoryName(openfiles.FileName);
                path = System.IO.Path.GetDirectoryName(path + "/../../");
                //Console.Write(path);
                Player.Stop();
                Player.LoadMedia("dvdsimple:///" + path);
                Thread.Sleep(1000);
                Player.Play();
                Properties.Settings.Default.SavePath = path;
            }
            
            //UIの各種ボタンを有効化
            playButton.IsEnabled = true;
            pauseButton.IsEnabled = true;
            commentButton.IsEnabled = true;

            //var openDirectory = new System.Windows.Forms.FolderBrowserDialog();
            //System.Windows.Forms.DialogResult result = openDirectory.ShowDialog();
            //if (result == System.Windows.Forms.DialogResult.OK)
            //{
            //Player.Stop();
            //    Player.LoadMedia("dvdsimple:///J:/.");
            //    Player.Play();
            //}
            return;

            /*
            String pathString = path.Text;

            Uri uri = null;
            if (!Uri.TryCreate(pathString, UriKind.Absolute, out uri)) return;

            Player.Stop();
            Player.LoadMedia(uri);
            //if you pass a string instead of a Uri, LoadMedia will see if it is an absolute Uri, else will treat it as a file path
            Player.Play();
            */
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            //Thread.Sleep(10000);
            Player.Play();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            Player.PauseOrResume();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Player.Stop();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close(); //closing the main window will also terminate the application
        }

        private void AspectRatio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Player == null) return;
            switch ((sender as ComboBox).SelectedIndex)
            {
                case 0:
                    Player.AspectRatio = AspectRatio.Default;
                    break;

                case 1:
                    Player.AspectRatio = AspectRatio._16_9;
                    break;

                case 2:
                    Player.AspectRatio = AspectRatio._4_3;
                    break;
            }
        }

        private void ProgressBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var value = (float)(e.GetPosition(ProgressBar).X / ProgressBar.ActualWidth);
            ProgressBar.Value = value;
        }

        #endregion --- Events ---

        private void commentButton_Click(object sender, RoutedEventArgs e)
        {
            string comment = commentText.Text;
            if (comment == "")
            {
                return;
            }
            
            string dateString = DateTime.UtcNow.ToString("u");  //現在時刻をISO8601形式で文字列化
            string commentTime = ((int)Player.Time.TotalSeconds).ToString(); //コメント時間（トータル秒）
            Console.WriteLine("コメント時間:" + commentTime);
            //Console.WriteLine("時刻" + dateString);
            commentText.Text = "";

            Task task = postComment(dateString, commentTime, comment);
            //task.Wait();
            showComment(comment);
        }

        private void showComment(string comment)
        {

            //ラベル作成
            //for (int i = 0; i < 10; i++)
            //{
            var label = new Label();
            string labelString = comment;
            label.Content = labelString;
            label.Foreground = System.Windows.Media.Brushes.White;
            //Style style = this.FindResource("WhiteStyle") as Style;
            //label.Style = style;
            System.Windows.Media.Effects.DropShadowEffect effect = new System.Windows.Media.Effects.DropShadowEffect();
            effect.BlurRadius = 0;
            effect.Color = Colors.Black;
            effect.Opacity = 0.8;
            effect.ShadowDepth = 2;
            label.Effect = effect;
            label.FontSize = Player.ActualHeight / 16f;
            commentGrid.Children.Add(label);
            Random random = new Random(DateTime.Now.Millisecond);
            double fontHeight = label.FontSize * 2f;
            fontHeight = (fontHeight > 5.0) ? fontHeight : 5.0;
            float height = random.Next(0, (int)commentGrid.ActualHeight - (int)label.FontSize * 2);
            label.Margin = new Thickness(commentGrid.ActualWidth, height, commentGrid.ActualWidth - commentGrid.ActualWidth - (label.FontSize * labelString.Length), commentGrid.ActualHeight - height - fontHeight);
            showingCommentList.Add(label);
            //Console.WriteLine("きちょる" + label.Margin);
            //}
        }
    }
}
