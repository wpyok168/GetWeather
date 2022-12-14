using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NSoup;
using AngleSharp;
using AngleSharp.Dom;
using System.Text.RegularExpressions;
using Newtonsoft;
using Newtonsoft.Json.Linq;

namespace 天气预报
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WebClient web = new WebClient();
            web.Encoding = Encoding.UTF8;
            //string str = web.DownloadString("http://www.weather.com.cn/weather/101230203.shtml");
            //var t = web.DownloadStringTaskAsync("http://www.weather.com.cn/weather/101230203.shtml");    
            //t.Wait();
            string cityname = "思明";
            var t = web.DownloadDataTaskAsync($"http://toy1.weather.com.cn/search?cityname={cityname}&callback=success_jsonpCallback&_=1664936165826");
            string city = Encoding.UTF8.GetString(t.Result);
            string pattern = @"\[\{(.)*?(?=\))";
            string cityjson = Regex.Match(city, pattern).ToString();
            JArray citylist = JArray.Parse(cityjson);
            //foreach (var item in citylist)
            //{
            //    string citys = item.SelectToken("$.ref").ToString();
            //    string[] strs = citys.Split('~');
            //}
            string citys = citylist[0].SelectToken("$.ref").ToString();
            string[] strs = citys.Split('~');

            string url = $"http://www.weather.com.cn/weather/{strs[0]}.shtml";
            var t1 = web.DownloadDataTaskAsync(url);
            //var t1 = web.DownloadDataTaskAsync("http://www.weather.com.cn/weather/101230203.shtml");
            string html = Encoding.UTF8.GetString(t1.Result);

            web.Dispose();
            if (html.ToLower().Contains("empty"))
            {
                return;
            }
            //AngleSharp
            IConfiguration config = Configuration.Default;
            IBrowsingContext context = BrowsingContext.New(config);
            var t2 = context.OpenAsync(req => req.Content(html));
            t1.Wait();
            AngleSharp.Dom.IDocument document = t2.Result;
            AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> elements = document.GetElementsByClassName("t clearfix");
            //$(".t.clearfix>li:eq(3)>p.win>i").prop("innerHTML")
            AngleSharp.Dom.IElement e1 = document.QuerySelector(".t.clearfix>li>p.win>i");
            List<TQ> list2 = new List<TQ>();
            foreach (AngleSharp.Dom.IElement item in elements.Children("li"))
            {
                TQ tQ = new TQ();
                foreach (AngleSharp.Dom.IElement item1 in item.Children)
                {
                    if (item1.LocalName.ToLower().Equals("h1"))
                    {
                        tQ.Date = item1.TextContent;
                    }
                    if (item1.ClassName != null)
                    {
                        if (item1.ClassName.ToLower().Equals("wea"))
                        {
                            tQ.Temperature = item1.TextContent.Replace("\n", ""); ;
                        }
                        if (item1.ClassName.ToLower().Equals("tem"))
                        {
                            tQ.Weather = item1.TextContent.Replace("\n", "");
                        }
                        if (item1.ClassName.ToLower().Equals("win"))
                        {
                            tQ.Wind = item1.TextContent.Replace("\n", "");
                            //IEnumerable<IElement> nodes1 = item1.Children.Where(m => m.TagName.Equals("EM"));
                            //foreach (IElement node in nodes1.First().Children)
                            //{

                            //}

                            if (item1.Children.Count() >0 )
                            {
                                foreach (var item2 in item1.Children[0].Children)
                                {
                                    if (string.IsNullOrEmpty(tQ.NNW))
                                    {
                                        tQ.NNW = item2.GetAttribute("title");
                                    }
                                    else
                                    {
                                        tQ.NNW = tQ.NNW + "转" + item2.GetAttribute("title");
                                    }  
                                }
                            }
                        }
                    }
                }
                list2.Add(tQ);
            }
            document.Close();

            //NSoup
            NSoup.Nodes.Document docns = NSoupClient.Parse(html);
            NSoup.Select.Elements els = docns.GetElementsByClass("t");

            //HtmlAgilityPack
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//*[@id=\"7d\"]/ul"); // 1.11.46版本有问题：丢失风力 ＜3级  回退1.11.45 正常，再升版正常
            //CSS选择器需要再nuget: HtmlAgilityPack.CssSelectors
            HtmlNode hnode = doc.DocumentNode.QuerySelector(".t.clearfix");  // 有问题：丢失风力 ＜3级
            HtmlNode hnode1 = doc.DocumentNode.QuerySelector(".c7d");
            //HtmlNode ihnode = doc.DocumentNode.QuerySelector(".t.clearfix>li>p.win>i");   //有问题不能正常取值
            //需要多几个NextSibling才能取到i标签的innnerText 不像anglesharp那么方便
            //HtmlNode ihnode = doc.DocumentNode.QuerySelector(".t.clearfix>li>p.win>i").NextSibling.NextSibling.NextSibling;
            HtmlNode ihnode = doc.DocumentNode.QuerySelector(".t.clearfix>li>p.win>i").NextSiblingElement().NextSibling.NextSibling;
            List<TQ> list = new List<TQ>();

            foreach (HtmlNode node in nodes.Elements())
            {
                if (node.Name == "li")
                {
                    TQ tQ = new TQ();
                    foreach (HtmlNode item in node.ChildNodes)
                    {
                        if (item.Name == "h1")
                        {
                            tQ.Date = item.InnerText;
                        }
                        if (item.Name == "p" && item.OuterHtml.Contains("wea"))
                        {
                            tQ.Temperature = item.InnerText;
                        }
                        if (item.QuerySelector(".tem") != null)
                        {
                            tQ.Weather = item.InnerText.Replace("\n","");
                        }
                        if (item.QuerySelector(".win") != null)
                        {
                            //List<HtmlNode> list1 = (item.ChildNodes.Where(m => m.Name == "i")).ToList<HtmlNode>();
                            tQ.Wind = (item.ChildNodes.Where(m => m.Name == "i")).ToList<HtmlNode>()[0].InnerText;
                            HtmlNodeCollection list1 = (item.ChildNodes.Where(m => m.Name == "em")).ToList<HtmlNode>()[0].ChildNodes;
                            foreach (HtmlNode item1 in list1)
                            {
                                if (item1.Name.Equals("#text"))
                                {
                                    continue;
                                }
                                else if (string.IsNullOrEmpty(tQ.NNW))
                                {
                                    tQ.NNW = item1.GetAttributeValue("title", "");
                                }
                                else
                                {
                                    tQ.NNW = tQ.NNW + "转" + item1.GetAttributeValue("title", "");
                                }
                            }
                        }
                    }
                    list.Add(tQ);
                }
            }
        }
        public class TQ
        {
            public string Date { get; set; } //日期
            public string Weather { get; set; } //天气
            public string Temperature { get; set; } //温度
            public string Wind { get; set; } //风力
            public string NNW { get; set; } //方向

        }
    }
}
