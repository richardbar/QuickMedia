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

namespace QuickMedia
{
    public enum TypeOfMedia
    {
        MP3,
        MP4,
        JPG,
        TXT
    }

    class Media
    {
        public static Dictionary<TypeOfMedia, string> typeToString = new Dictionary<TypeOfMedia, string>()
        {
            { TypeOfMedia.MP3, "audio/mp3" },
            { TypeOfMedia.MP4, "video/mp4" },
            { TypeOfMedia.JPG, "image/jpeg" },
            { TypeOfMedia.TXT, "text/javascript" }
        };
        public TypeOfMedia Type;
        public string Name = "";
        public string DataPath = "";
    }
}
