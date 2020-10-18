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

        private readonly Dictionary<int, Thread> _responseThreads = new Dictionary<int, Thread>();

        private readonly Thread _listenerThread, _threadCleaner, _shutdownWaiter;

        private bool _isRunning = true;

        /// <summary>
        /// Create a server and start listening from that port
        /// </summary>
        public Server(uint port)
        {
            _url = $"http://{Constants.ServiceURL}:{port}/";

            _listener.Prefixes.Add(_url);
            _listener.Start();

            _listenerThread = new Thread(() =>
            {
                Task listener = HandleConnection();
                listener.GetAwaiter().GetResult();
            });
            _listenerThread.Start();

            _threadCleaner = new Thread(() =>
            {
                while (_isRunning)
                {
                    try
                    {
                        foreach (KeyValuePair<int, Thread> thread in _responseThreads)
                        {

                            if (!thread.Value.IsAlive)
                            {
                                _responseThreads[thread.Key] = null;
                                _responseThreads.Remove(thread.Key);
                            }
                        }
                    }
                    catch { }
                    GC.Collect();

                    Thread.Sleep(5000);
                }
            });
            _threadCleaner.Start();

            _shutdownWaiter = new Thread(() =>
            {
                // Check if it's running every few seconds.
                // Higher is better.
                while (_isRunning) Thread.Sleep(1000);
                // Wait for 1 more GC to run and end.
                Thread.Sleep(4000);
                // Close program
                Environment.Exit(0);
            });
            _shutdownWaiter.Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void AddPage(string path, string response) => _responses.Add(path, response);
        public void AddPage(string path, Media media) => AddResponse(path, media);
        public void AddPage(string path, Func<string> func) => AddResponse(path, func);
        public void AddPage(string path, Func<Dictionary<string, string>, string> func) => AddResponse(path, func);

        private void AddResponse(string path, object o)
        {
            if (_responses.ContainsKey(path))
                _responses[path] = o;
            else
                _responses.Add(path, o);
        }

        private async Task HandleConnection()
        {
            while (_isRunning)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    Console.WriteLine("Device connected");

                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    string path = System.Uri.UnescapeDataString(request.Url.AbsolutePath[1..]);
                    if (String.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path)) path = "index";
                    if (path == "favicon.ico") continue;

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
                        {
                            Thread thread = new Thread(() => SendResponse(response, (string)_responses[path], true));
                            int threadID = thread.ManagedThreadId;
                            _responseThreads.Add(threadID, thread);
                            thread.Start();
                            thread = null;
                        }
                        else if (_responses[path] is Func<string>)
                        {
                            Thread thread = new Thread(() => SendResponse(response, ((Func<string>)_responses[path])(), true));
                            int threadID = thread.ManagedThreadId;
                            _responseThreads.Add(threadID, thread);
                            thread.Start();
                            thread = null;
                        }
                        else if (_responses[path] is Media)
                        {
                            Thread thread = new Thread(() => SendFile(response, ((Media)_responses[path]).DataPath, Media.typeToString[((Media)_responses[path]).Type]));
                            int threadID = thread.ManagedThreadId;
                            _responseThreads.Add(threadID, thread);
                            thread.Start();
                            thread = null;
                        }
                        else if (_responses[path] is Func<Dictionary<string, string>, string>)
                        {
                            Thread thread = new Thread(() =>
                            {
                                Dictionary<string, string> paramets = new Dictionary<string, string>();
                                for (int i = 0; i < parameters.Length; i++)
                                {
                                    if (parameters[i] == "") continue;
                                    paramets.Add(parameters[i].Split('=')[0], (parameters[i].Split('=').Length == 2) ? parameters[i].Split('=')[1] : "");
                                }
                                SendResponse(response, ((Func<Dictionary<string, string>, string>)_responses[path])(paramets), true);
                                parameters = null;
                            });
                            int threadID = thread.ManagedThreadId;
                            _responseThreads.Add(threadID, thread);
                            thread.Start();
                            thread = null;
                        }
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
        /// Used to send a response that is stored in memory over an HTTP Connection. Must be used
        /// asynchronous because responses might be large or connection might be slow
        /// </summary>
        /// <param name="response">HTTP Connection - Response</param>
        /// <param name="responseData">String - Response text to be sent</param>
        /// <param name="isFinal">Boolean - Will close connection if true after it sends data</param>
        private void SendResponse(HttpListenerResponse response, string responseData, bool isFinal = false)
        {
            SendResponse(new BinaryWriter(response.OutputStream), Encoding.ASCII.GetBytes(responseData), responseData.Length, isFinal);
        }

        /// <summary>
        /// Used to send a response that is stored in memory over an HTTP Connection. Must be used
        /// asynchronous because responses might be large or connection might be slow
        /// </summary>
        /// <param name="response">HTTP Connection - Response</param>
        /// <param name="bufferData">Byte array - Response Buffer</param>
        /// /// <param name="isFinal">Boolean - Will close connection if true after it sends data</param>
        private void SendResponse(HttpListenerResponse response, byte[] bufferData, int count, bool isFinal = false)
        {
            SendResponse(new BinaryWriter(response.OutputStream), bufferData, count, isFinal);
            if (isFinal != false)
                response.Close();
        }

        private void SendResponse(BinaryWriter writer, string responseData, bool isFinal = false)
        {
            SendResponse(writer, Encoding.ASCII.GetBytes(responseData), responseData.Length, isFinal);
        }

        private void SendResponse(BinaryWriter writer, byte[] bufferData, int count, bool isFinal = false)
        {
            try
            {
                writer.Write(bufferData, 0, count);
                writer.Flush();
                if (isFinal)
                    writer.Close();
            }
            catch (Exception e){ Console.WriteLine(e); }
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
            response.KeepAlive = true;
            byte[] buffer = new byte[16 * 1024];
            int read;
            try
            {
                using FileStream fs = File.OpenRead(dataFile);

                response.ContentLength64 = fs.Length;
                response.SendChunked = false;
                response.ContentType = contenType;

                BinaryWriter writer = new BinaryWriter(response.OutputStream);
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    SendResponse(writer, buffer, read, false);
                writer.Close();
                response.Close();
            }
            catch { }
        }
    }
}