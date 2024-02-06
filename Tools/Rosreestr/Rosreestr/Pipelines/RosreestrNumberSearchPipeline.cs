using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace Tools.Rosreestr
{
    partial class RosreestrPipeline
    {
        /// <summary>
        /// Форма поиска заявок по номеру
        /// </summary>
        private class RosreestrNumberSearchPipeline : IRosreestrNumberSearchPipeline
        {
            private readonly RosreestrPipeline _pipeline;

            public bool Found { get; set; }


            public RosreestrNumberSearchPipeline(RosreestrPipeline pipeline)
            {
                this._pipeline = pipeline;

                string body = _pipeline._vaadinSecurityKey + GROUP_SEPARATOR +
                              "125" + FIELD_SEPARATOR +
                              "PID0" + FIELD_SEPARATOR +
                              "height" + FIELD_SEPARATOR +
                              "i" + RECORD_SEPARATOR +
                              "755" + FIELD_SEPARATOR +
                              "PID0" + FIELD_SEPARATOR +
                              "width" + FIELD_SEPARATOR +
                              "i" + RECORD_SEPARATOR +
                              "1423" + FIELD_SEPARATOR +
                              "PID0" + FIELD_SEPARATOR +
                              "browserWidth" + FIELD_SEPARATOR +
                              "i" + RECORD_SEPARATOR +
                              "378" + FIELD_SEPARATOR +
                              "PID0" + FIELD_SEPARATOR +
                              "browserHeight" + FIELD_SEPARATOR +
                              "i" + RECORD_SEPARATOR +
                              "true" + FIELD_SEPARATOR +
                              "PID36" + FIELD_SEPARATOR +
                              "disabledOnClick" + FIELD_SEPARATOR +
                              "b" + RECORD_SEPARATOR +
                              "true" + FIELD_SEPARATOR +
                              "PID36" + FIELD_SEPARATOR +
                              "state" + FIELD_SEPARATOR +
                              "b" + RECORD_SEPARATOR +
                              "1,659,141,false,false,false,false,1,64,15" + FIELD_SEPARATOR +
                              "PID36" + FIELD_SEPARATOR +
                              "mousedetails" + FIELD_SEPARATOR + "s";
                HttpWebRequest request = this._pipeline.CreatePostRequest(POST_REQUEST_URL, body);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string s = reader.ReadToEnd();
                    JObject json = _pipeline.GetJson(s);
                    this._pipeline._actions.Add(new ActionInfo { Name = "Доступ к форме поиска заявок по номеру", Request = body, Response = s });
                }
            }


			public RequestDownloadInfo DownloadRequest(string numRequest, string dirPath)
            {
                if (this._pipeline.HasError)
                {
                    this._pipeline.OnErrorThrown("Ошибка сервиса росреестра. DownloadRequest");
                    return null;
                }

                try
                {
                    string body = this._pipeline._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "true" + FIELD_SEPARATOR +
                                  "PID56" + FIELD_SEPARATOR +
                                  "clearSelections" + FIELD_SEPARATOR +
                                  "b" + RECORD_SEPARATOR +
                                  "125" + FILE_SEPARATOR + FIELD_SEPARATOR +
                                  "PID56" + FIELD_SEPARATOR +
                                  "selected" + FIELD_SEPARATOR +
                                  "c" + RECORD_SEPARATOR + FIELD_SEPARATOR +
                                  "PID49" + FIELD_SEPARATOR +
                                  "dateString" + FIELD_SEPARATOR +
                                  "s" + RECORD_SEPARATOR +
                                  "-1" + FIELD_SEPARATOR +
                                  "PID49" + FIELD_SEPARATOR +
                                  "year" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  "-1" + FIELD_SEPARATOR +
                                  "PID49" + FIELD_SEPARATOR +
                                  "month" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  "-1" + FIELD_SEPARATOR +
                                  "PID49" + FIELD_SEPARATOR +
                                  "day" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  numRequest + FIELD_SEPARATOR +
                                  "PID45" + FIELD_SEPARATOR +
                                  "text" + FIELD_SEPARATOR +
                                  "s" + RECORD_SEPARATOR +
                                  "12" + FIELD_SEPARATOR +
                                  "PID45" + FIELD_SEPARATOR +
                                  "c" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  "true" + FIELD_SEPARATOR +
                                  "PID55" + FIELD_SEPARATOR +
                                  "state" + FIELD_SEPARATOR +
                                  "b" + RECORD_SEPARATOR +
                                  "1,936,215,false,false,false,false,1,24,4" + FIELD_SEPARATOR +
                                  "PID55" + FIELD_SEPARATOR +
                                  "mousedetails" + FIELD_SEPARATOR + "s";

                    HttpWebRequest request = this._pipeline.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = this._pipeline.GetJson(s);
                        this._pipeline._actions.Add(new ActionInfo { Name = "DownloadRequest", Request = body, Response = s });

                        if (s.Contains(numRequest))
                        {
                            if (s.Contains("download") && s.Contains("\"src\"") && s.Contains("zip"))
                            {
                                int startIndex = s.IndexOf("\"src\"") + 6;
                                startIndex = s.IndexOf('\"', startIndex) + 1;

                                int endIndex = s.IndexOf(".zip");
                                endIndex = s.IndexOf('\"', endIndex) - 1;

                                string fileUrl = s.Substring(startIndex, endIndex - startIndex);
                                fileUrl = BASE_URL + "/" + fileUrl.Replace("\\/", "/").TrimStart('/');

                                string path = Path.Combine(dirPath, numRequest + ".zip");
                                using (WebClientEx downloader = new WebClientEx())
                                {
                                    downloader.CookieContainer.Add(this._pipeline._cookieSession);
                                    downloader.DownloadFile(fileUrl, path);
                                }

                                return RequestDownloadInfo.CreateSuccessInfo(path);
                            }
                            else
                            {
                                string status = string.Empty;
                                try
                                {
                                    int startIdx = s.IndexOf(numRequest);
                                    startIdx = s.IndexOf('[', startIdx) + 1;
                                    startIdx = s.IndexOf('[', startIdx) + 1;
                                    startIdx = s.IndexOf('[', startIdx) + 1;
                                    startIdx = s.IndexOf('[', startIdx) + 1;

                                    int endIdx = s.IndexOf(']', startIdx);
                                    endIdx = s.LastIndexOf('\"', endIdx) - 1;
                                    startIdx = s.LastIndexOf('\"', endIdx) + 1;

                                    status = s.Substring(startIdx, endIdx - startIdx);
                                }
                                catch
                                {
                                    throw new InvalidOperationException("Request status not found");
                                }

                                return RequestDownloadInfo.CreateNoLinkInfo();
                            }
                        }
                        else
                        {
                            return RequestDownloadInfo.CreateNoRequestInfo();
                        }
                    }
                }
                catch (Exception exc)
                {
                    this._pipeline.OnErrorThrown(exc, "Ошибка сервиса росреестра. DownloadRequest");
                }

                return null;
            }
        }
    }
}
