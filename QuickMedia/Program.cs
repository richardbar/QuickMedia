/**
* QuickMedia, an open source media server
* Copyright (C) 2020  Richard Bariampa
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License as published
* by the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU Affero General Public License for more details.
* You should have received a copy of the GNU Affero General Public License
* along with this program.  If not, see <https://www.gnu.org/licenses/>.
* 
* richardbar:          richard1996ba@gmail.com
**/

using System.Collections.Generic;
using System.IO;
using System.Xml;
using Newtonsoft.Json;

namespace QuickMedia
{
    class Program
    {
        public Media[] medias;

        public Program()
        {
            Server server = new Server(8080);
            if (!File.Exists("medias.json")) File.WriteAllText("medias.json", "[]");
            medias = JsonConvert.DeserializeObject<Media[]>(File.ReadAllText("medias.json"));
            for (int i = 0; i < medias.Length; i++)
                server.AddPage(medias[i].Name, medias[i]);
            server.AddPage("getAll", (Dictionary<string, string> paramets) => {
                if (!paramets.ContainsKey("format")) return "";
                switch(paramets["format"].ToLower())
                {
                    case "json":
                        return JsonConvert.SerializeObject(medias);
                    case "xml":
                        string returny = "<?xml version=\"1.0\" encoding=\"ASCII\"?><medias>";
                        for (int i = 0; i < medias.Length; i++)
                            returny += $"<media>" +
                                       $"<Type>{medias[i].Type}</Type>" +
                                       $"<Name>{medias[i].Name}</Name>" +
                                       $"<DataPath>{medias[i].DataPath}</DataPath>" +
                                       $"</media>";
                        returny += "</medias>";
                        return returny;
                    default:
                        return "";
                }
            });
        }

        public static void Main()
        {
            _ = new Program();
        }
    }
}
