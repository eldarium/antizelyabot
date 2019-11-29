using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace ZelyaDushitelBot
{
    public class WeatherWorker
    {
        static string APIKEY = "90ad4ca194cd399cde533c8b2696c8fb";
        static string CurrentUrl =
    "http://api.openweathermap.org/data/2.5/weather?" +
    "@QUERY@=@LOC@&mode=xml&units=metric&APPID=" + APIKEY;
        static string ForecastUrl =
            "http://api.openweathermap.org/data/2.5/forecast?" +
            "@QUERY@=@LOC@&mode=xml&units=metric&APPID=" + APIKEY;

        public string GetWeather(string city)
        {
            string url = CurrentUrl.Replace("@LOC@", city);
            url = url.Replace("@QUERY@", "q");
            var sb = new StringBuilder();
            using (WebClient wc = new WebClient())
            {
                try
                {
                    string xml = wc.DownloadString(url);
                    XmlDocument xml_doc = new XmlDocument();
                    xml_doc.LoadXml(xml);
                    XmlNode loc_node = xml_doc.SelectSingleNode("/current/city");
                    sb.AppendLine($"{loc_node.Attributes["name"].InnerText}, {loc_node.SelectSingleNode("country").InnerText}");
                    var temp_node = xml_doc.SelectSingleNode("//temperature").Attributes["value"].InnerText;
                    sb.AppendLine($"Температура {temp_node} градусов");
                    var winSpeed = xml_doc.SelectSingleNode("//wind");
                    sb.AppendLine($"Ветер {winSpeed.SelectSingleNode("speed").Attributes["value"].InnerText} м/с, направление {winSpeed.SelectSingleNode("direction").Attributes["name"].InnerText}");
                    var wtNode = xml_doc.SelectSingleNode("//precipitation");
                    switch (wtNode.Attributes["mode"].InnerText)
                    {
                        case "no":
                            sb.AppendLine("Без осадков");
                            break;
                        case "rain":
                            sb.AppendLine("Дождь");
                            break;
                        case "snow":
                            sb.AppendLine("Снег");
                            break;
                        default:
                            sb.AppendLine(wtNode.Attributes["mode"].InnerText);
                            break;
                    }
                    sb.AppendLine($"\"{xml_doc.SelectSingleNode("//weather/@value").InnerText.First().ToString().ToUpper() + xml_doc.SelectSingleNode("//weather/@value").InnerText.Substring(1)}\"");
                }
                catch (Exception e)
                {
                    sb.Append("exception\n");
                    sb.Append(e);
                }
            }
            return sb.ToString();
        }

        public string GetForecast(string city)
        {
            string url = ForecastUrl.Replace("@LOC@", city);
            url = url.Replace("@QUERY@", "q");
            var sb = new StringBuilder();
            using (WebClient wc = new WebClient())
            {
                try
                {
                    string xml = wc.DownloadString(url);
                    XmlDocument xml_doc = new XmlDocument();
                    xml_doc.LoadXml(xml);

                    // Get the city, country, latitude, and longitude.
                    XmlNode loc_node = xml_doc.SelectSingleNode("weatherdata/location");
                    sb.AppendLine($"{loc_node.SelectSingleNode("name").InnerText}, {loc_node.SelectSingleNode("country").InnerText}");

                    foreach (XmlNode time_node in xml_doc.SelectNodes("//time"))
                    {
                        // Get the time in UTC.
                        DateTime time =
                            DateTime.Parse(time_node.Attributes["from"].Value,
                                null, DateTimeStyles.AssumeUniversal);

                        // Get the temperature.
                        XmlNode temp_node = time_node.SelectSingleNode("temperature");
                        string temp = temp_node.Attributes["value"].Value;

                        sb.AppendLine($"{time.ToShortTimeString()} : {temp}");
                    }
                }
                catch (Exception e)
                {
                    sb.Append("exception\n");
                    sb.Append(e);
                }
            }
            return sb.ToString();

        }
    }
}