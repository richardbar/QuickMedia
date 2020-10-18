using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMedia
{
    public class Media
    {
        public string fileName;
        public string directory;
        public string type;
    }

    public class Server
    {
        private readonly string _url;
        private readonly HttpListener _listener = new HttpListener();
        private readonly SortedDictionary<string, object> _responses = new SortedDictionary<string, object>();
        private readonly SortedDictionary<int, Thread> _responseThreads = new SortedDictionary<int, Thread>();
        private readonly Thread _listenerThread, _threadCleaner, _shutdownWaiter;
        public bool _isRunning = true;

        public Server(uint port)
        {
            _url = $"http://localhost:{port}/";

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
                            try
                            {
                                if (!thread.Value.IsAlive)
                                {
                                    _responseThreads[thread.Key] = null;
                                    _responseThreads.Remove(thread.Key);
                                }
                            }
                            catch
                            {
#if DEBUG
                                Console.Error.WriteLine("There was an exception while cleaning reponse Threads");
#endif
                            }
                        }
                    }
                    catch
                    {
#if DEBUG
                        Console.Error.WriteLine("There was an exception while cleaning reponse Threads");
#endif
                    }

                    GC.Collect();

                    Thread.Sleep(5000);
                }
            });
            _threadCleaner.Start();

            _shutdownWaiter = new Thread(() =>
            {
                while (_isRunning) Thread.Sleep(1000);

                Thread.Sleep(4000);

                _listenerThread.Abort();
                _threadCleaner.Abort();

                _listener.Close();

                Environment.Exit(0);
            });
            _shutdownWaiter.Start();
        }

        public Server() : this(8080) { }

        public bool AddPage(string path, object response)
        {
            if (_responses.ContainsKey(path))
                _responses[path] = response;
            else
                _responses.Add(path, response);

            return _responses[path] == response;
        }

        private async Task HandleConnection()
        {
            while (_isRunning)
            {
                try
                {
                    HttpListenerContext ctx = await _listener.GetContextAsync();
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse res = ctx.Response;

                    string path = Uri.UnescapeDataString(req.Url.AbsolutePath[1..].Split("?")[0]);

#if DEBUG
                    Console.WriteLine($"Device connected from {req.UserAgent} and is requesting {path}");
#endif

                    res.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                    res.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                    res.AppendHeader("Access-Control-Allow-Origin", "*");

                    try
                    {
                        Object response = _responses[path];

                        if (response is string)
                        {
                            Thread thread = new Thread(() => SendResponse(res, (string)response));
                            int threadID = thread.ManagedThreadId;
                            _responseThreads.Add(threadID, thread);
                            thread.Start();
                        }
                        else if (response is Func<string>)
                        {
                            Thread thread = new Thread(() => SendResponse(res, ((Func<string>)response)()));
                            int threadID = thread.ManagedThreadId;
                            _responseThreads.Add(threadID, thread);
                            thread.Start();
                        }
                        else if (response is Func<SortedDictionary<string, string>, string>)
                        {
                            SortedDictionary<string, string> arguments = new SortedDictionary<string, string>();
                            string[] postData = new StreamReader(req.InputStream, req.ContentEncoding).ReadToEnd().Split("&");
                            foreach (string data in postData)
                            {
                                string[] splits = data.Split(new char[] { '=' }, 2);
                                if (splits.Length == 1)
                                    arguments[splits[0]] = "";
                                else
                                    arguments[splits[0]] = splits[1];
                            }

                            Thread thread = new Thread(() => SendResponse(res, ((Func<SortedDictionary<string, string>, string>)response)(arguments)));
                            int threadID = thread.ManagedThreadId;
                            _responseThreads.Add(threadID, thread);
                            thread.Start();
                        }
                        else if (response is Media)
                        {
                            Thread thread = new Thread(() => SendFile(res, ((Media)response).directory, ((Media)response).type));
                            int threadID = thread.ManagedThreadId;
                            _responseThreads.Add(threadID, thread);
                            thread.Start();
                            thread = null;
                        }
                    }
                    catch (Exception)
                    {
                        res.StatusCode = 404;
                        res.Close(); 
                    }
                }
                catch { }
            }
        }

        private void SendResponse(HttpListenerResponse res, string response)
        {
            SendResponse(new BinaryWriter(res.OutputStream), Encoding.ASCII.GetBytes(response), response.Length);
            res.Close();
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
            catch { }
        }

        private void SendFile(HttpListenerResponse res, string datafile, string contentType)
        {
            res.KeepAlive = true;
            byte[] buffer = new byte[16 * 1024];
            int read;
            try
            {
                using FileStream fs = File.OpenRead(datafile);

                res.ContentLength64 = fs.Length;
                res.SendChunked = false;
                res.ContentType = contentType;

                using (BinaryWriter writer = new BinaryWriter(res.OutputStream))
                {
                    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        SendResponse(writer, buffer, read, false);
                    }

                    writer.Close();
                    res.Close();
                }
            }
            catch { }
        }
    }
}
