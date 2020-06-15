﻿using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TikTokTools.Model;
using TikTokTools.Util;

namespace TikTokTools
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Task task;

        delegate void SetTextCallback(string text);

        delegate void BinData(List<VideoInfo> awemes);

        public bool IsFilePath { get; set; }

        public void Logout(string msg)
        {
            if (this.LogBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(Logout);
                this.Invoke(d, new object[] { msg });
            }
            else
            {
                this.LogBox.AppendText(msg + "\r\n");
            }
        }

        public void ChangeStatus(string msg)
        {
            if (this.LogBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(ChangeStatus);
                this.Invoke(d, new object[] { msg });
            }
            else
            {
                if (msg == "start")
                {
                    btn_Stop.Visible = true;
                    btn_Start.Visible = false;
                }
                else
                {
                    btn_Start.Visible = true;
                    btn_Stop.Visible = false;
                }
            }
        }

        public void BinTableData(List<VideoInfo> awemes)
        {
            if (this.table_Video.InvokeRequired)
            {
                BinData d = new BinData(BinTableData);
                this.Invoke(d, new object[] { awemes });
            }
            else
            {
                this.table_Video.DataSource = awemes;
            }
        }

        CancellationTokenSource cts;

        CancellationToken ct
        {
            get { return cts.Token; }
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            LogBox.Clear();
            btn_Stop.Visible = true;
            btn_Start.Visible = false;
            var configEntity = GetConfigEntity(txt_url.Text);

            var tab = tab_model.SelectedTab.Name;

            cts = new CancellationTokenSource();

            var videoInfoList = GetAlarmConfirmModelsByDGVCheckbox();
            task = new Task(() =>
             {
                 if (tab == "tabPage2")
                 {
                     string WebVideoPath = configEntity.LocalPath + "\\WebVideo\\";
                     if (false == System.IO.Directory.Exists(WebVideoPath))
                     {
                         System.IO.Directory.CreateDirectory(WebVideoPath);
                     }
                     if (configEntity.VideoSource == VideoSource.DouYin)
                     {
                         configEntity.SourcePath = "";

                         Logout("共找到" + videoInfoList.Count + "个视频");
                         foreach (var item in videoInfoList)
                         {
                             ct.ThrowIfCancellationRequested();
                             var filename = configEntity.LocalPath + "\\WebVideo\\" + item.Desc.Trim() + ".mp4";
                             if (configEntity.VideoSource == VideoSource.TikTok)
                             {
                                 BuildVideo(string.Format("https://www.iesdouyin.com/share/video/{0}/?region=CN&mid={1}&u_code=17fk746j0&titleType=title&timestamp={1}&utm_campaign=client_share&app=aweme&utm_medium=ios&tt_from=copy&utm_source=copy", item.AwemeId, Timestamp()), filename);
                             }
                             else
                             {
                                 try
                                 {
                                     switch (configEntity.VideoSource)
                                     {
                                         case VideoSource.DouYin:
                                             GetVideo(string.Format("https://www.iesdouyin.com/share/video/{0}/?region=CN&mid={1}&u_code=17fk746j0&titleType=title&timestamp={1}&utm_campaign=client_share&app=aweme&utm_medium=ios&tt_from=copy&utm_source=copy", item.AwemeId, Timestamp()), filename, configEntity);
                                             break;
                                         case VideoSource.KuaiShou:
                                             GetVideo(item.AwemeId, filename, configEntity);
                                             break;
                                     }

                                 }
                                 catch
                                 {
                                     Logout(item + "解析失败");
                                 }
                             }
                             configEntity.SourcePath += configEntity.SourcePath == "" ? filename : "," + filename;
                         }
                     }
                     else if (configEntity.VideoSource == VideoSource.KuaiShou)
                     {
                         var filename = configEntity.LocalPath + "\\WebVideo\\" + Guid.NewGuid().ToString() + ".mp4";
                         GetVideo(txt_url.Text, filename, configEntity);
                         configEntity.SourcePath = filename;
                     }
                 }
                 if (check_cmd.Checked)
                 {
                     var log = new ConvertHelperCmd.Run(Logout);
                     var changestatus = new ConvertHelperCmd.Run(ChangeStatus);

                     try
                     {
                         var result = new ConvertHelperCmd().Convert(configEntity, log, ct, changestatus);
                     }
                     catch (Exception ex)
                     {
                         log("程序已停止:" + ex.Message);
                         changestatus("Stop");
                     }
                 }
                 else
                 {
                     var log = new ConvertHelper.Run(Logout);
                     var changestatus = new ConvertHelper.Run(ChangeStatus);

                     try
                     {
                         var result = new ConvertHelper().Convert(configEntity, log, ct, changestatus);
                     }
                     catch (Exception ex)
                     {
                         log("程序已停止:" + ex.Message);
                         changestatus("Stop");
                     }
                 }

             });

            task.Start();
        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            btn_Start.Visible = true;
            btn_Stop.Visible = false;
            cts.Cancel();
            clearFolder(GetConfigEntity());
        }

        private ConfigEntity GetConfigEntity(string url = "douyin")
        {
            var random = new Random();
            var configEntity = new ConfigEntity()
            {
                SourcePath = SourcePathText.Text.ToLower().Contains(".mp4") ? SourcePathText.Text : SourcePathText.Text + '\\',
                LocalPath = Environment.CurrentDirectory,
                Remove_StartTime = !string.IsNullOrWhiteSpace(Remove_Left.Text) ? Convert.ToDouble(Remove_Left.Text) : 0.1,
                Remove_EndTime = !string.IsNullOrWhiteSpace(Remove_Right.Text) ? Convert.ToDouble(Remove_Right.Text) : 0.1,
                Video_Mirroring = Video_Mirroring.Checked,
                CenterTime = !string.IsNullOrWhiteSpace(Video_Center.Text) ? Convert.ToDouble(Video_Center.Text) : 3.5,
                ExtendTime = !string.IsNullOrWhiteSpace(Video_Center_Extend.Text) ? Convert.ToDouble(Video_Center_Extend.Text) : 0.01,
                Video_BitrateChange = !string.IsNullOrWhiteSpace(Video_Bitrate.Text) ? check_cmd.Checked ? (long)Convert.ToInt32(Video_Bitrate.Text) : (long)Convert.ToInt32(Video_Bitrate.Text) * 1000 : check_cmd.Checked ? (long)random.Next(20, 25) * 10 : (long)random.Next(20, 25) * 10 * 1000,
                Video_FrameRateChange = !string.IsNullOrWhiteSpace(Video_FrameRate.Text) ? Convert.ToDouble(Video_FrameRate.Text) : random.NextDouble(),
                ThreadNumber_Single = !string.IsNullOrWhiteSpace(ThreadNumber_SingleBox.Text) ? Convert.ToInt32(ThreadNumber_SingleBox.Text) : 32,
                Gamma = !string.IsNullOrWhiteSpace(text_Gamma.Text) ? Convert.ToDouble(text_Gamma.Text) : 1,
                Saturation = !string.IsNullOrWhiteSpace(text_Saturation.Text) ? Convert.ToDouble(text_Saturation.Text) : 1,
                Brightness = !string.IsNullOrWhiteSpace(text_Brightness.Text) ? Convert.ToDouble(text_Brightness.Text) : 0,
                Contrast = !string.IsNullOrWhiteSpace(text_Contrast.Text) ? Convert.ToDouble(text_Contrast.Text) : 1,
                Repeat = check_Repeat.Checked,
                Filter = check_Filter.Checked,
                IsFilePath = IsFilePath,
                VideoSource = url.ToLower().IndexOf("tiktok") > 0 ? VideoSource.TikTok : url.ToLower().IndexOf("douyin") > 0 ? VideoSource.DouYin : VideoSource.KuaiShou
            };


            return configEntity;
        }

        private void SetContorlVal(ConfigEntity configEntity)
        {
            Remove_Left.Text = configEntity.Remove_StartTime.ToString();
            Remove_Right.Text = configEntity.Remove_EndTime.ToString();
            Video_Mirroring.Checked = configEntity.Video_Mirroring;
            Video_Center.Text = configEntity.CenterTime.ToString();
            Video_Center_Extend.Text = configEntity.ExtendTime.ToString();
            Video_Bitrate.Text = configEntity.Video_BitrateChange.ToString();
            Video_FrameRate.Text = configEntity.Video_FrameRateChange.ToString();
            ThreadNumber_SingleBox.Text = configEntity.ThreadNumber_Single.ToString();
            text_Gamma.Text = configEntity.Gamma.ToString();
            text_Saturation.Text = configEntity.Saturation.ToString();
            text_Brightness.Text = configEntity.Brightness.ToString();
            text_Contrast.Text = configEntity.Contrast.ToString();
            check_Repeat.Checked = configEntity.Repeat;
            check_Filter.Checked = configEntity.Filter;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetContorlVal(GetConfigEntity());
            List<VideoInfo> videoInfos = new List<VideoInfo>();
            table_Video.DataSource = videoInfos;
            Logout("");
        }

        private void check_Filter_CheckedChanged(object sender, EventArgs e)
        {
            if (check_Filter.Checked)
            {
                text_Gamma.Enabled = true;
                text_Brightness.Enabled = true;
                text_Contrast.Enabled = true;
                text_Saturation.Enabled = true;
            }
            else
            {
                text_Gamma.Enabled = false;
                text_Brightness.Enabled = false;
                text_Contrast.Enabled = false;
                text_Saturation.Enabled = false;
            }
        }

        private void btn_Chosen_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SourcePathText.Text = dialog.SelectedPath;
                IsFilePath = false;
            }
        }

        private void btn_ChosenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*.*)|*.*";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                SourcePathText.Text = string.Join(",", fileDialog.FileNames);
                IsFilePath = true;
            }
        }

        private void btn_OpenEnd_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("Explorer.exe", Environment.CurrentDirectory + "\\Finish");
        }

        private void check_cmd_CheckedChanged(object sender, EventArgs e)
        {

            if (check_cmd.Checked)
            {
                ThreadNumber_SingleBox.Visible = false;
                label6.Visible = false;
                label7.Visible = false;
            }
            else
            {
                ThreadNumber_SingleBox.Visible = true;
                label6.Visible = true;
                label7.Visible = true;
            }
        }

        private void GetVideo(string url, string filename, ConfigEntity configEntity)
        {
            Logout("正在无水印解析视频...");

            List<PostDateClass> postDateClassList = new List<PostDateClass>();
            switch (configEntity.VideoSource)
            {

                case VideoSource.TikTok:
                    try
                    {
                        var urlArray = url.Split('/');
                        IsInt(urlArray[urlArray.Length - 1]);
                        BuildVideo("https://snaptik.app/action.php?aweme_id=" + urlArray[urlArray.Length - 1] + "&act=download", filename);
                    }
                    catch
                    {
                        postDateClassList.Add(new PostDateClass("url", url));
                        var result = HttpClientHelper.postFileMessage("https://snaptik.app/action.php", postDateClassList);
                        result = result.Replace("lynaritaa/", "action.php?aweme_id=") + "&act=download";
                        BuildVideo(result, filename);
                    }
                    break;
                case VideoSource.DouYin:

                    //var html_302 = url;
                    //try
                    //{
                    //    url = HttpHelper.HttpGet(url);
                    //    string reg = @"<a[^>]*href=([""'])?(?<href>[^'""]+)\1[^>]*>";
                    //    url = Regex.Match(url, reg, RegexOptions.IgnoreCase).Groups["href"].Value;
                    //    html_302 = HttpHelper.HttpGet(url);
                    //}
                    //catch
                    //{
                    //}
                    //string vid = Regex.Matches(html_302, "/(.*?)/")[2].Value.Split('/')[1];
                    //var dytk = Regex.Match(Regex.Match(html_302, "dytk(.*?)}").Value, "\\w+(?=\")").Value;
                    //var getResult1 = HttpHelper.HttpGet("https://www.iesdouyin.com/web/api/v2/aweme/iteminfo/?item_ids=" + vid + "&dytk=" + dytk);
                    //var data1 = JsonConvert.DeserializeObject<Hashtable>(getResult1);
                    //var item_list = JsonConvert.DeserializeObject<List<Hashtable>>(data1["item_list"].ToString());
                    //var video = JsonConvert.DeserializeObject<Hashtable>(item_list[0]["video"].ToString());
                    //var play = JsonConvert.DeserializeObject<Hashtable>(video["play_addr"].ToString());
                    //var url_list = JsonConvert.DeserializeObject<List<string>>(play["url_list"].ToString());
                    //var videopath1 = url_list[0].ToString().Replace("playwm", "play");
                    //HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(videopath1);
                    //myHttpWebRequest.AllowAutoRedirect = false;
                    //HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                    //string rederictUrl = myHttpWebResponse.ResponseUri.AbsoluteUri;
                    postDateClassList.Add(new PostDateClass("url", url));
                    var getResult1 = HttpClientHelper.postMessage("http://www.ab18.cn/dyinner.php?url=" + url, postDateClassList, false, "Get");
                    var data1 = JsonConvert.DeserializeObject<Hashtable>(getResult1);
                    var videopath1 = data1["video"].ToString();
                    DownloadFile(videopath1, filename);
                    break;
                case VideoSource.KuaiShou:

                    //WebUtils webUtils = new WebUtils();
                    //var getResult = webUtils.DoGet("http://tapi.douhe.cloud/douhe/api/video/get_video?url=" + url);
                    //var data = JsonConvert.DeserializeObject<Hashtable>(getResult);
                    //var videopath = JsonConvert.DeserializeObject<Hashtable>(data["data"].ToString())["finalUri"].ToString();
                    try
                    {
                        var didv = Timestamp();
                        var cookies = string.Format("oing_setcoo=1; didv={0}; did=web_241ecf6e16c34b42a5cf1847b1914622; sid=91bd7e19aaa7a53b8fdb2ca2; clientid=3; client_key=65890b29; Hm_lvt_86a27b7db2c5c0ae37fee4a8a35033ee=1591762140; Hm_lpvt_86a27b7db2c5c0ae37fee4a8a35033ee=1591762180; userId=1967762954", didv);

                        HttpItem httpItem = new HttpItem();
                        httpItem.URL = url;
                        httpItem.Cookie = cookies;
                        httpItem.UserAgent = "user-agent=Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A372 Safari/604.1";
                        HttpHelper httpHelper = new HttpHelper();
                        var htmlResult = httpHelper.GetHtml(httpItem);
                        Spider spider = new Spider(htmlResult.Html, 1);
                        var jsonStr = spider.GetAttr("//*[@id=\"hide-pagedata\"]", "data-pagedata");
                        var json = JsonConvert.DeserializeObject<Hashtable>(jsonStr.Replace("&#34;", "\""));
                        json = JsonConvert.DeserializeObject<Hashtable>(json["video"].ToString());



                        DownloadFile(json["srcNoMark"].ToString(), filename);

                    }
                    catch
                    {
                        Thread.Sleep(1000);
                        WebUtils webUtils = new WebUtils();
                        var getResult = webUtils.DoGet("http://tapi.douhe.cloud/douhe/api/video/get_video?url=" + url);
                        var data = JsonConvert.DeserializeObject<Hashtable>(getResult);
                        var videopath = JsonConvert.DeserializeObject<Hashtable>(data["data"].ToString())["finalUri"].ToString();
                        DownloadFile(videopath, filename);
                    }
                    break;
            }


        }

        private void BuildVideo(string url, string filename)
        {
            Spider spider = new Spider(url);
            var videoPath = spider.GetAttr("//a[1]", "href");
            DownloadFile(videoPath, filename);
        }


        private IList<Aweme> GetVideoList(string url)
        {
            Logout("正在解析用户的视频列表...");
            ConfigEntity configEntity = GetConfigEntity(url);
            IList<Aweme> result = new List<Aweme>();

            result = GetData(url, configEntity);
            return result;
        }

        private IList<Aweme> GetData(string url, ConfigEntity configEntity)
        {
            switch (configEntity.VideoSource)
            {
                case VideoSource.DouYin:
                    WebUtils webUtils = new WebUtils();
                    var reslutStr = webUtils.DoGet("https://www.fhcollege.com/api/api/GetVideoPathList?url=" + url);

                    return JsonConvert.DeserializeObject<List<Aweme>>(reslutStr);
                case VideoSource.KuaiShou:
                    IList<Aweme> awemes = new List<Aweme>();
                    Spider spider = new Spider(url);
                    var nodeList = spider.GetNodeList("//*[@id=\"body-share-user\"]//li[@class=\"photo\"]/a");
                    HttpHelper httpHelper = new HttpHelper();
                    foreach (var item in nodeList)
                    {
                        var didv = Timestamp();
                        var sig = "cfd09546407b874aa361861c";
                        var cookies = string.Format("oing_setcoo=1; didv={0}; did=web_a619a93d92cd434b97ae764ce29a386b; sid=" + sig + "; ", didv);
                        HttpItem httpItem = new HttpItem();

                        //httpItem.Cookie = cookies;
                        httpItem.UserAgent = "user-agent=Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A372 Safari/604.1";

                        httpItem.URL = "https://v.kuaishou.com" + item.Attributes["href"].Value;
                        var htmlResult = httpHelper.GetHtml(httpItem);
                        //var urlitem = "https://v.kuaishou.com" + item.Attributes["href"].Value;
                        Spider spiderItem = new Spider(htmlResult.Html, 1);
                        var jsonStr = spiderItem.GetAttr("//*[@class=\"hide-pagedata\"]", "data-pagedata");
                        var json = JsonConvert.DeserializeObject<Hashtable>(jsonStr.Replace("&#34;", "\""));
                        var video = JsonConvert.DeserializeObject<Hashtable>(json["video"].ToString());
                        var share = JsonConvert.DeserializeObject<Hashtable>(json["share"].ToString());
                        awemes.Add(
                            new Aweme()
                            {
                                AwemeId = video["Id"].ToString(),
                                Desc = share["title"].ToString(),
                                Statistics = new Statistics()
                                {
                                    DiggCount = Convert.ToInt32(video["likeCount"].ToString()),
                                    AwemeId = video["Id"].ToString(),
                                    CommentCount = Convert.ToInt32(video["commentCount"].ToString()),
                                    PlayCount = Convert.ToInt32(video["viewCount"].ToString()),
                                }
                            });
                        Thread.Sleep(1000);
                    }
                    return awemes;
            }
            return null;
        }
        //private string GetData(ChromeDriver driver,string uid) {
        //    ct.ThrowIfCancellationRequested();
        //    Thread.Sleep(2000);
        //    if (driver.FindElementByTagName("body").Text.Length>3)
        //    {
        //        return driver.FindElementByTagName("body").Text;
        //    }

        //    driver.ExecuteScript("document.location.href=document.location.href");

        //        return GetData(driver, uid);
        //}

        /// <summary>
        /// 获取当前的时间戳
        /// </summary>
        /// <returns></returns>
        public static string Timestamp()
        {
            long ts = ConvertDateTimeToInt(DateTime.Now);
            return ts.ToString();
        }

        /// <summary>  
        /// 将c# DateTime时间格式转换为Unix时间戳格式  
        /// </summary>  
        /// <param name="time">时间</param>  
        /// <returns>long</returns>  
        public static long ConvertDateTimeToInt(System.DateTime time)
        {
            //System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            //long t = (time.Ticks - startTime.Ticks) / 10000;   //除10000调整为13位      
            long t = (time.Ticks - 621356256000000000) / 10000;
            return t;
        }



        ///<summary>
        /// 下载文件
        /// </summary>
        /// <param name="URL">下载文件地址</param>
        /// <param name="Filename">下载后另存为（全路径）</param>
        private bool DownloadFile(string URL, string filename)
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.Headers.Add("Host", new Uri(URL).Host);
                webClient.Headers.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A372 Safari/604.1");
                webClient.DownloadFile(URL, filename);
                //HttpWebRequest Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL);
                //HttpWebResponse myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                //Stream st = myrp.GetResponseStream();
                //Stream so = new System.IO.FileStream(filename, System.IO.FileMode.Create);
                //byte[] by = new byte[1024];
                //int osize = st.Read(by, 0, (int)by.Length);
                //while (osize > 0)
                //{
                //    so.Write(by, 0, osize);
                //    osize = st.Read(by, 0, (int)by.Length);
                //}
                //so.Close();
                //st.Close();
                //myrp.Close();
                //Myrq.Abort();
                return true;
            }
            catch (System.Exception e)
            {
                return false;
            }
        }

        private bool IsInt(string str)
        {
            foreach (var item in str)
            {
                Int32.Parse(item.ToString());
            }
            return true;
        }


        private void clearFolder(ConfigEntity configEntity)
        {
            var files = Directory.GetFiles(configEntity.LocalPath + @"\Audio\", ".", SearchOption.AllDirectories).ToList();
            foreach (var item in files)
            {
                try
                {
                    File.Delete(item);
                }
                catch { }
            }
            var files2 = Directory.GetFiles(configEntity.LocalPath + @"\Video\", ".", SearchOption.AllDirectories).ToList();
            foreach (var item in files2)
            {
                try
                {
                    File.Delete(item);
                }
                catch { }
            }
            var files3 = Directory.GetFiles(configEntity.LocalPath + @"\WebVideo\", ".", SearchOption.AllDirectories).ToList();
            foreach (var item in files3)
            {
                try
                {
                    File.Delete(item);
                }
                catch { }
            }
        }

        private void check_more_CheckedChanged(object sender, EventArgs e)
        {
            if (check_more.Checked)
            {
                lab_address.Text = "用户主页：";
                table_Video.Enabled = true;
            }
            else
            {
                lab_address.Text = "视频地址：";
                table_Video.Enabled = false;
            }
        }

        private void btn_Read_Click(object sender, EventArgs e)
        {
            task = new Task(() =>
             {
                 if (check_more.Checked)
                 {
                     //GetVideoListByWebBrowser(txt_url.Text);
                     var videoList = GetVideoList(txt_url.Text);
                     List<VideoInfo> videoInfos = videoList.Select(x => new VideoInfo(x)).ToList();
                     BinTableData(videoInfos);
                 }
                 else
                 {
                     var url = HttpHelper.HttpGet(txt_url.Text);
                     string vid = Regex.Matches(url, "/(.*?)/")[2].Value.Split('/')[1];
                     string reg = @"<a[^>]*href=([""'])?(?<href>[^'""]+)\1[^>]*>";
                     url = Regex.Match(url, reg, RegexOptions.IgnoreCase).Groups["href"].Value;
                     var html_302 = HttpHelper.HttpGet(url);
                     var dytk = Regex.Match(Regex.Match(html_302, "dytk(.*?)}").Value, "\\w+(?=\")").Value;
                     var getResult = HttpHelper.HttpGet("https://www.iesdouyin.com/web/api/v2/aweme/iteminfo/?item_ids=" + vid + "&dytk=" + dytk);
                     var data1 = JsonConvert.DeserializeObject<Hashtable>(getResult);
                     var item_list = JsonConvert.DeserializeObject<List<Hashtable>>(data1["item_list"].ToString());
                     var videoInfo = new VideoInfo();
                     videoInfo.AwemeId = item_list[0]["aweme_id"].ToString();
                     videoInfo.Desc = item_list[0]["desc"].ToString();
                     var staticdata = JsonConvert.DeserializeObject<Hashtable>(item_list[0]["statistics"].ToString());
                     videoInfo.CommentCount = staticdata["comment_count"].ToString();
                     videoInfo.DiggCount = staticdata["digg_count"].ToString();
                     BinTableData(new List<VideoInfo>() { videoInfo });
                 }
             });
            task.Start();


        }

        private void table_Video_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1 || e.ColumnIndex == -1) return;

            if (table_Video.Columns[e.ColumnIndex].Name != "select") return;

            DataGridViewCheckBoxCell cell = (DataGridViewCheckBoxCell)table_Video.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell.Value != null && (bool)cell.Value)
            {
                cell.Value = false;
            }
            else
            {
                cell.Value = true;
            }
        }

        private List<VideoInfo> GetAlarmConfirmModelsByDGVCheckbox()
        {
            List<VideoInfo> alarmModels = new List<VideoInfo>();
            foreach (DataGridViewRow row in table_Video.Rows)
            {
                if (row.Cells["select"].Value != null && (bool)row.Cells["select"].Value)
                {
                    VideoInfo model = row.DataBoundItem as VideoInfo;
                    if (model == null) continue;
                    alarmModels.Add(model);
                }
            }
            return alarmModels.Count == 0 ? null : alarmModels;
        }

        private void check_all_CheckedChanged(object sender, EventArgs e)
        {
            int count = table_Video.Rows.Count;
            for (int i = 0; i < count; i++)
            {
                DataGridViewCheckBoxCell checkCell = (DataGridViewCheckBoxCell)table_Video.Rows[i].Cells[0];
                Boolean flag = Convert.ToBoolean(checkCell.Value);
                if (flag == false && check_all.Checked)
                {
                    checkCell.Value = true;
                }
                else if (!check_all.Checked)
                {
                    checkCell.Value = false;
                }
            }
        }


        public void GetVideoListByWebBrowser(string url)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            myHttpWebRequest.AllowAutoRedirect = false;
            HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            string rederictUrl = myHttpWebResponse.ResponseUri.AbsoluteUri;
            var uid = rederictUrl.Replace("https://www.iesdouyin.com/share/user/", "").Split('?')[0];
            ThreadWebBrowser("http://douyin.fhcollege.com/index.php?userid=" + uid);
            
        }


        private void ThreadWebBrowser(string url)
        {
            Thread tread = new Thread(new ParameterizedThreadStart(BeginCatch));
            tread.SetApartmentState(ApartmentState.STA);
            tread.Start(url);
        }

        private void BeginCatch(object obj)
        {
            string url = obj.ToString();
            WebBrowser wb = new WebBrowser();
            wb.ScriptErrorsSuppressed = true;
            //在这里Navigate一个空白页面
            wb.Navigate("about:blank");
            string htmlcode = GetHtmlSource(url);
            wb.Document.Write(htmlcode);
            
        }
        //WebClient取网页源码
        private string GetHtmlSource(string Url)
        {
            string text1 = "";
            try
            {
                System.Net.WebClient wc = new WebClient();
                text1 = wc.DownloadString(Url);
            }
            catch (Exception exception1)
            { }
            return text1;
        }

    }
}