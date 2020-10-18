using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickMedia
{
    public class Program
    {
        public static Media[] meds;
        public static string GetMedias(SortedDictionary<string, string> arguments)
        {
            if (!arguments.ContainsKey("type"))
                return JsonConvert.SerializeObject(meds);
            switch (arguments["type"])
            {
                case "photo":
                    return JsonConvert.SerializeObject(from med in meds where med.type == "image/jpeg" || med.type == "image/png" select med);
                case "video":
                    return JsonConvert.SerializeObject(from med in meds where med.type == "video/mpeg" || med.type == "video/mp4" select med);
                case "audio":
                    return JsonConvert.SerializeObject(from med in meds where med.type == "video/mpeg" select med);
            }
            return "[]";
        }

        public static void Main(string[] args)
        {
            Server srv = new Server(8080);
            srv.AddPage("index", new Media() { directory = "AppData/static/index.html", type = "text/html" });
            srv.AddPage("", new Media() { directory = "AppData/static/index.html", type = "text/html" });
            srv.AddPage("js/QuickMedia.js", new Media() { directory = "AppData/static/js/QuickMedia.js", type = "application/javascript" });
            srv.AddPage("css/QuickMedia.css", new Media() { directory = "AppData/static/css/QuickMedia.css", type = "text/css" });

            if (!File.Exists("AppData/media.json"))
                File.WriteAllText("AppData/media.json", "[]");
            meds = JsonConvert.DeserializeObject<Media[]>(File.ReadAllText("AppData/media.json"));
            foreach (Media med in meds)
            {
                med.fileName = $"media/{med.fileName}";
                med.directory = $"AppData/media/{med.directory}";

                srv.AddPage(med.fileName, med);
            }
            Func<SortedDictionary<string, string>, string> GetMeds = GetMedias;
            srv.AddPage("getMedias", GetMeds);
        }
    }
}
