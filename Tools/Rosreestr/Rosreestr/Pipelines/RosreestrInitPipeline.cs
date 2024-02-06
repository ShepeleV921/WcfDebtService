using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace Tools.Rosreestr
{
    partial class RosreestrPipeline
    {
        /// <summary>
        /// Инициализация начала работы с сайтом росреестра
        /// </summary>
        private class RosreestrInitPipeline : IRosreestrInitPipeline
        {
            readonly RosreestrPipeline _pipeline;


            public RosreestrInitPipeline(RosreestrPipeline pipeline)
            {
                _pipeline = pipeline;

                #region Подключение к сайту росреестра

                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(ROSR_EGRN);
                    request.ProtocolVersion = HttpVersion.Version11;
                    request.Method = WebRequestMethods.Http.Get;
                    request.CookieContainer = _pipeline._cookieContainer;
                    request.Timeout = 3 * 60 * 1000;
                    request.ServerCertificateValidationCallback += (_, __, ___, ____) => true;

                    // Получение кукисов для работы с сайтом
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader respStream = new StreamReader(response.GetResponseStream()))
                    {
                        string s = respStream.ReadToEnd();
                    }
                }

                // кукис для поддержания сессии между запросами
                _pipeline._cookieSession = _pipeline._cookieContainer.GetCookies(new Uri(POST_REQUEST_URL))["JSESSIONID_8"];
                if (_pipeline._cookieSession == null)
                    throw new RosReestrException("Не удалось создать сессию для сайта rosreestr");


                #endregion


                #region Инициализация работы с сайтом

                {
                    string body = "init" + GROUP_SEPARATOR;
                    HttpWebRequest request = this._pipeline.CreatePostRequest(POST_INIT_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _pipeline.GetJson(s);
                        this._pipeline._actions.Add(new ActionInfo { Name = "init", Request = body, Response = s });

                        try
                        {
                            this._pipeline._vaadinSecurityKey = json["Vaadin-Security-Key"].Value<string>();
                        }
                        catch
                        {
                            throw new RosReestrException("Не удалось получить код безопасности для сайта rosreestr");
                        }
                    }
                }

                #endregion


                #region Focus

                {
                    string body = this._pipeline._vaadinSecurityKey + GROUP_SEPARATOR + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "focus" + FIELD_SEPARATOR + "s";
                    HttpWebRequest request = this._pipeline.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _pipeline.GetJson(s);
                        this._pipeline._actions.Add(new ActionInfo { Name = "init", Request = body, Response = s });
                    }
                }

                #endregion


                #region Вход на сайт

                {
                    string body = this._pipeline._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "36" + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "c" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  this._pipeline._key + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "curText" + FIELD_SEPARATOR + "s";
                    HttpWebRequest request = this._pipeline.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = this._pipeline.GetJson(s);
                        this._pipeline._actions.Add(new ActionInfo { Name = "Login", Request = body, Response = s });
                    }
                }


                #endregion


                #region Blur

                {
                    string body = this._pipeline._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "8" + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "c" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "blur" + FIELD_SEPARATOR + "s";
                    HttpWebRequest request = this._pipeline.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = this._pipeline.GetJson(s);
                        this._pipeline._actions.Add(new ActionInfo { Name = "blur", Request = body, Response = s });
                    }
                }

                #endregion


                #region Доступ к форме выбора "Мои счета" - "Мои заявки" - "Поиск объектов"

                {
                    string body = this._pipeline._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "true" + FIELD_SEPARATOR +
                                  "PID30" + FIELD_SEPARATOR +
                                  "state" + FIELD_SEPARATOR +
                                  "b" + RECORD_SEPARATOR +
                                  "1,565,217,false,false,false,false,1,26,13" + FIELD_SEPARATOR +
                                  "PID30" + FIELD_SEPARATOR +
                                  "mousedetails" + FIELD_SEPARATOR + "s";
                    HttpWebRequest request = this._pipeline.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = this._pipeline.GetJson(s);
                        this._pipeline._actions.Add(new ActionInfo
                        {
                            Name = "Доступ к форме выбора Мои счета - Мои заявки - Поиск объектов",
                            Request = body,
                            Response = s
                        });
                    }
                }

                #endregion
            }

            /// <summary>
            /// Открывает форму поиска объектов недвижимости
            /// </summary>
            /// <returns></returns>
            public IRosreestrRealEstateSearchPipeline OpenRealEstateSearchForm()
            {
                if (_pipeline.HasError)
                {
                    _pipeline.OnErrorThrown("Ошибка сервиса росреестра. OpenRealEstateSearchForm");
                    return null;
                }

                try
                {
                    return new RosreestrRealEstateSearchPipeline(_pipeline);
                }
                catch (Exception exc)
                {
                    _pipeline.OnErrorThrown(exc, "Ошибка сервиса росреестра. OpenRealEstateSearchForm");
                }

                return null;
            }

            /// <summary>
            /// Открывает форму поиска заказанных выписок
            /// </summary>
            /// <returns></returns>
            public IRosreestrNumberSearchPipeline OpenNumberSearchFrom()
            {
                if (_pipeline.HasError)
                {
                    _pipeline.OnErrorThrown("Ошибка сервиса росреестра. OpenNumberSearchFrom");
                    return null;
                }

                try
                {
                    return new RosreestrNumberSearchPipeline(_pipeline);
                }
                catch (Exception exc)
                {
                    _pipeline.OnErrorThrown(exc, "Ошибка сервиса росреестра. OpenRealEstateSearchForm");
                }

                return null;
            }
        }
    }
}
