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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMedia
{
    /// <summary>
    /// Used for the Creation of an HTTP Server listening from a standart port
    /// </summary>
    class Server
    {
        private readonly string _url;

        private readonly HttpListener _listener = new HttpListener();

        private readonly Dictionary<string, object> _responses = new Dictionary<string, object>();

        /// <summary>
        /// Create a server and start listening from that port
        /// </summary>
        public Server(uint port)
        {
            _url = $"http://{Constants.ServiceURL}:{port}/";

            _listener.Prefixes.Add(_url);
            _listener.Start();

            Thread listenerThread = new Thread(() =>
            {
                Task listener = HandleConnection();
                listener.GetAwaiter().GetResult();
            });
            listenerThread.Start();
        }

        public void AddPage(string path, string response) => _responses.Add(path, response);
        public void AddPage(string path, Media media) => AddResponse($"files/{path}", media);
        public void AddPage(string path, Func<string> func) => AddResponse($"funcs/{path}", func);
        public void AddPage(string path, Func<Dictionary<string, string>, string> func) => AddResponse($"funcs/{path}", func);

        private void AddResponse(string path, object o)
        {
            if (_responses.ContainsKey(path))
                _responses[path] = o;
            else
                _responses.Add(path, o);
        }

        private async Task HandleConnection()
        {
            while (true)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    Console.WriteLine("Device connected");

                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    string path = System.Uri.UnescapeDataString(request.Url.AbsolutePath[1..]);
                    if (String.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path)) path = "index";
                    if (path == "favicon.ico")
                    {
                        new Thread(() =>
                        {
                            byte[] responseText = File.ReadAllBytes("favicon.ico");
                            response.OutputStream.Write(responseText, 0, responseText.Length);
                            response.Close();
                        }).Start();
                    }
                    string[] parameters = Array.Empty<string>();
                    try
                    {
                        parameters = request.RawUrl[1..].Substring(path.Length)[1..].Split('&');
                    }
                    catch { }

                    Console.WriteLine(path);
                    if (_responses.ContainsKey(path))
                    {
                        if (_responses[path] is string)
                            new Thread(() =>
                            {
                                byte[] responseText = Encoding.ASCII.GetBytes((string)_responses[path]);
                                response.OutputStream.Write(responseText, 0, responseText.Length);
                                response.Close();
                            }).Start();
                        else if (_responses[path] is Media)
                            new Thread(() => SendFile(response, ((Media)_responses[path]).DataPath, Media.typeToString[((Media)_responses[path]).Type])).Start();
                        else if (_responses[path] is Func<string>)
                            new Thread(() =>
                            {
                                byte[] responseText = Encoding.ASCII.GetBytes(((Func<string>)_responses[path])());
                                response.OutputStream.Write(responseText, 0, responseText.Length);
                                response.Close();
                            }).Start();
                        else if (_responses[path] is Func<Dictionary<string, string>, string>)
                            new Thread(() =>
                            {
                                Dictionary<string, string> paramets = new Dictionary<string, string>();
                                for (int i = 0; i < parameters.Length; i++)
                                {
                                    if (parameters[i] == "") continue;
                                    paramets.Add(parameters[i].Split('=')[0], (parameters[i].Split('=').Length == 2) ? parameters[i].Split('=')[1] : "");
                                }
                                byte[] responseText = Encoding.ASCII.GetBytes(((Func<Dictionary<string, string>, string>)_responses[path])(paramets));
                                parameters = null;
                                response.OutputStream.Write(responseText, 0, responseText.Length);
                                response.Close();
                            }).Start();
                    }
                    else
                    {
                        response.StatusCode = 404;
                        response.Close();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Used for sending a file over an HTTP Connection. Must be used asynchronous because
        /// files might be large or connection might be slow.
        /// </summary>
        /// <param name="response">HTTP Connection - Response</param>
        /// <param name="dataFile">String - Location of file to be sent</param>
        /// <param name="contenType">Type of content of file</param>
        private void SendFile(HttpListenerResponse response, string dataFile, string contenType)
        {
            long memoryUsed = Process.GetCurrentProcess().PrivateMemorySize64;
            byte[] buffer = new byte[64 * 1024];
            int read;
            try
            {
                using FileStream fs = File.OpenRead(dataFile);

                response.ContentLength64 = fs.Length;
                response.SendChunked = false;
                response.ContentType = contenType;

                using BinaryWriter writer = new BinaryWriter(response.OutputStream);
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, read);
                    writer.Flush();
                }
                writer.Close();
            }
            catch { }
            response.Close();
            memoryUsed = Process.GetCurrentProcess().PrivateMemorySize64 - memoryUsed;
            if (memoryUsed > 64 * 1024) GC.Collect();
        }
    }
}