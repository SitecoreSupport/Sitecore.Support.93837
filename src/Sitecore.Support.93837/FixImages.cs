using HtmlAgilityPack;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.Shell.Controls.RichTextEditor.Pipelines.SaveRichTextContent;
using Sitecore.Xml;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Sitecore.Support
{
    public class FixImages
    {
        private static int ExtractCSS(string styleValue, string s)
        {
            int num = styleValue.IndexOf(s, StringComparison.OrdinalIgnoreCase);
            int num2 = styleValue.IndexOf(";", num);
            int result;
            int.TryParse(styleValue.Substring(num + s.Length, num2 - num - s.Length).Replace("px", "").Trim(), out result);
            return result;
        }

        private static Item FindItem(ID itemID)
        {
            Item result;
            using (List<Database>.Enumerator enumerator = Factory.GetDatabases().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Item item = enumerator.Current.GetItem(itemID);
                    bool flag = item != null;
                    if (flag)
                    {
                        result = item;
                        return result;
                    }
                }
            }
            result = null;
            return result;
        }

        private static void FindNext(string str, out int begin, ref int end)
        {
            begin = str.IndexOf("<img", end);
            bool flag = begin >= 0;
            if (flag)
            {
                end = str.IndexOf("/>", begin);
            }
        }

        private static string FixHtml(string html)
        {
            int num = 0;
            int num2 = 0;
            StringBuilder stringBuilder = new StringBuilder();
            int num3;
            FixImages.FindNext(html, out num3, ref num2);
            bool flag = num3 >= 0;
            string result;
            if (flag)
            {
                while (num3 >= 0 && num3 < num2)
                {
                    stringBuilder.Append(html, num, num3 - num);
                    string value = FixImages.FixImg(html.Substring(num3, num2 - num3 + 2));
                    stringBuilder.Append(value);
                    num = num2 + 2;
                    FixImages.FindNext(html, out num3, ref num2);
                }
                stringBuilder.Append(html.Substring(num2 + 2));
                result = FixImages.RemoveStyleAttribute(stringBuilder.ToString());
            }
            else
            {
                result = html;
            }
            return result;
        }

        private static string FixImg(string img)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(img.Replace("&", "|"));
            XmlNode xmlNode = xmlDocument.SelectNodes("img")[0];
            bool flag = xmlNode != null;
            string result;
            if (flag)
            {
                XmlAttribute xmlAttribute = xmlNode.Attributes["style"];
                XmlAttribute xmlAttribute2 = xmlNode.Attributes["src"];
                string text = xmlAttribute2.Value;
                string linkText = string.Empty;
                int num = text.IndexOf("?");
                bool flag2 = num >= 0;
                if (flag2)
                {
                    linkText = text.Remove(num);
                }
                DynamicLink dynamicLink;
                bool flag3 = !DynamicLink.TryParse(linkText, out dynamicLink);
                if (flag3)
                {
                    result = img;
                    return result;
                }
                MediaItem mediaItem = Context.ContentDatabase.GetItem(dynamicLink.ItemId);
                bool flag4 = mediaItem == null;
                if (flag4)
                {
                    mediaItem = FixImages.FindItem(dynamicLink.ItemId);
                }
                int num2 = 0;
                int num3 = 0;
                bool flag5 = xmlAttribute != null;
                if (flag5)
                {
                    string value = xmlAttribute.Value;
                    num2 = FixImages.ExtractCSS(value, "width:");
                    num3 = FixImages.ExtractCSS(value, "height:");
                }
                bool flag6 = num2 <= 0 || num3 <= 0;
                if (flag6)
                {
                    result = img;
                    return result;
                }
                bool flag7 = xmlAttribute2 == null;
                if (flag7)
                {
                    result = img;
                    return result;
                }
               
                xmlAttribute2.Value = text;
            }
            result = xmlNode.OuterXml.Replace('|', '&');
            return result;
        }

        public void Process(SaveRichTextContentArgs args)
        {
            args.Content = FixImages.FixHtml(args.Content);
        }

        private static string RemoveStyleAttribute(string content)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);
            HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//img");
            bool flag = htmlNodeCollection != null;
            if (flag)
            {
                foreach (HtmlNode current in ((IEnumerable<HtmlNode>)htmlNodeCollection))
                {
                    HtmlAttribute htmlAttribute = current.Attributes["style"];
                    bool flag2 = htmlAttribute != null;
                    if (flag2)
                    {

                        string input = current.Attributes["style"].Value; ;
                        string pattern = "(width:+.+?;)|(height:+.+?;)";
                        string replacement = "";
                        Regex rgx = new Regex(pattern);
                        string result = rgx.Replace(input, replacement);

                        current.Attributes["style"].Value = result;
                    }
                }
            }
            return htmlDocument.DocumentNode.InnerHtml;
        }

        private static string RemoveAttributes(string url)
        {
            string[] separator = new string[]
            {
                "?",
                "|amp;"
            };
            string[] array = url.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            List<string> list = new List<string>();
            for (int i = 0; i < array.Length; i++)
            {
                bool flag = array[i].Contains("h=") || array[i].Contains("w=");
                if (flag)
                {
                    array[i] = null;
                }
            }
            string[] array2 = array;
            for (int j = 0; j < array2.Length; j++)
            {
                string text = array2[j];
                bool flag2 = text != null;
                if (flag2)
                {
                    list.Add(text);
                }
            }
            string text2 = list[0];
            for (int k = 1; k < list.Count; k++)
            {
                bool flag3 = k == 1;
                if (flag3)
                {
                    text2 = text2 + "?" + list[k];
                }
                else
                {
                    text2 = text2 + "|amp;" + list[k];
                }
            }
            return text2;
        }
    }
}
