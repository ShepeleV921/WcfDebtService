using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Ionic.Zip;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using LoadPipeline.MailNotify;
using NLog;
using NLog.LayoutRenderers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Tools;
using Tools.Classes;
using Tools.DAL;
using Tools.Models;
using Tools.Rosreestr;
using Tools.Xml;
using static System.Net.Mime.MediaTypeNames;
using static System.Threading.Thread;
using Path = System.IO.Path;

namespace LoadPipeline
{
    public class LoadWorker : IWorker<LoadOrder>, IDisposable
    {
        private RosreestrPipeline _worker;
        private LoadOrder _order;
        private RequestDownloadInfo _downloadInfo;
        private IRosreestrNumberSearchPipeline _searchForm;

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly string _savePath = SETTINGS.XML_FOLDER;
        private static int _activeThreadsCount;

        public static bool OutOfCapacity => _activeThreadsCount == SETTINGS.UPLOAD_XML_THREAD_COUNT;


        public LoadWorker(RosreestrPipeline worker, LoadOrder order)
        {
            _worker = worker;
            _order = order;
            
            _activeThreadsCount++;
            Repository.SetBusyLoader(order.WorkerKey, order.NumRequest);
            _logger.Info($"Начинаю загрузку. Текущее число занятых потоков = {_activeThreadsCount}");
        }


        public void Dispose()
        {
            Repository.UpdateLastUploadAttempt(_order.ID);
            Repository.SetFreeLoader(_order.WorkerKey);
            _logger.Info($"Освободил поток для {_order}. Текущее количество = {_activeThreadsCount}");
            _activeThreadsCount--;
        }



        public void Run()
        {
            StartAsync().DoNotAwait();
        }


        private async Task StartAsync()
        {
            await Task.Run(() => Start());
        }


        private void RollBackToPrepared()
        {
            _logger.Error($"{_order}. Росреестр потерял выписку???. Откатываю до подготовленного");
            Repository.RollBackToPrepared(_order.ID);
        }

        private bool Start()
        {
            if (CurrentThread.Name == null)
            {
                CurrentThread.Name = "LoadWorker";
            }
            var options = new ChromeOptions();
            //options.AddArgument("no-sandbox");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-popup-blocking");
            //options.AddArgument("--incognito");
            options.AddUserProfilePreference("safebrowsing.enabled", true);
            using (IWebDriver driver = new ChromeDriver(@"C:\VS project\WcfDebtService\packages\Selenium.WebDriver.ChromeDriver.116.0.5845.9600\driver\win32", options, TimeSpan.FromMinutes(5)))
            {
                try
                {
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(920));
                    driver.Navigate().GoToUrl(@"https://lk.rosreestr.ru/request-access-egrn/property-search");
                    wait.Until(d => d.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div")).Count > 0);
                    var error = driver.FindElement(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div")).FindElements(By.TagName(@"button"));
                    if (error.Count > 0) // если есть баннеры, которые нужно закрыть  
                    {
                        for (int i = 0; i < error.Count; i++)
                        {
                            error[i].Click();
                            Sleep(1000);
                        }
                    }
                    wait.Until(d => d.FindElements(By.XPath("//*[@id='main-page-wrapper']/div/div[2]/div[2]/div/div/div[2]/p")).Count > 0);
                    bool Test_Element_Register = driver.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div/div[2]/div[2]/div/div/div[2]/p")).Count > 0;
                    if (Test_Element_Register == true)
                    {
                        driver.Navigate().GoToUrl(@"https://esia.gosuslugi.ru/login/");
                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='login']")).Count > 0);
                        Thread.Sleep(500);
                        driver.FindElement(By.XPath(@"//*[@id='login']")).SendKeys("+7(988)570-48-29");
                        Thread.Sleep(500);
                        driver.FindElement(By.XPath(@"//*[@id='password']")).SendKeys("<76MU_heVk");
                        Thread.Sleep(5000);
                        wait.Until(d => d.FindElements(By.XPath(@"/html/body/esia-root/div/esia-login/div/div[1]/form/div[4]/button")).Count > 0);
                        driver.FindElement(By.XPath(@"/html/body/esia-root/div/esia-login/div/div[1]/form/div[4]/button")).Click();
                        wait.Until(d => d.FindElements(By.XPath(@"/html/body/esia-root/div/esia-login/div/div/div/div[4]/button[2]")).Count > 0);
                        driver.FindElement(By.XPath(@"/html/body/esia-root/div/esia-login/div/div/div/div[4]/button[2]")).Click();
                        Thread.Sleep(3500);
                        driver.Navigate().GoToUrl(@"https://lk.rosreestr.ru/request-access-egrn/property-search");
                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div")).Count > 0);
                        Thread.Sleep(2000);
                        error = driver.FindElement(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div")).FindElements(By.TagName(@"button"));
                        if (error.Count > 0) // если есть баннеры, которые нужно закрыть  
                        {
                            for (int i = 0; i < error.Count; i++)
                            {
                                error[i].Click();
                                Sleep(500);
                            }
                        }
                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div/div[1]")));

                        bool authorization = driver.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[1]/header/div[1]/div/div[2]/div/div/span")).Count > 0;
                        if (authorization)
                        {
                            bool error_ = driver.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div")).Count > 0;
                            if (error_)
                            {
                                error = driver.FindElement(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div")).FindElements(By.TagName(@"button"));
                                if (error.Count > 0) // если есть баннеры, которые нужно закрыть  
                                {
                                    for (int i = 0; i < error.Count; i++)
                                    {
                                        error[i].Click();
                                        Sleep(500);
                                    }
                                }
                            }
                            wait.Until(d => d.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[1]/header/div[1]/div/div[2]/div/div/span")).Count > 0);
                            driver.FindElement(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[1]/header/div[1]/div/div[2]/div/div/span")).Click();
                            bool notification = driver.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div/div[1]")).Count > 0;
                            if (notification)
                            {
                                bool Error_ = driver.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div/div")).Count > 0;
                                if (Error_) // Дудосим сайт 
                                {
                                    while (Error_)
                                    {
                                        driver.FindElement(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div/div/div/button")).Click();
                                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[1]/header/div[1]/div/div[2]/div/div/span")).Count > 0);
                                        driver.FindElement(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[1]/header/div[1]/div/div[2]/div/div/span")).Click();
                                        Error_ = driver.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div")).Count > 0;
                                        Thread.Sleep(10_000);
                                    }
                                }
                            }
                            wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div/div[2]/div[3]/div[2]/a[1]")).Count > 0);
                            driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div/div[2]/div[3]/div[2]/a[1]")).Click();
                            wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div/div[2]/div/div[2]/div/a[1]")).Count > 0);
                            driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div/div[2]/div/div[2]/div/a[1]")).Click();
                        }
                    }
                    driver.Navigate().GoToUrl(@"https://lk.rosreestr.ru/eservices");
                    wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div/div[2]/div[3]/div[2]/a[1]")).Count > 0);
                    driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div/div[2]/div[3]/div[2]/a[1]")).Click();
                    wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div/div[2]/div/div[2]/div/a[3]")).Count > 0);
                    driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div/div[2]/div/div[2]/div/a[3]")).Click();
                    wait.Until(d => d.FindElements(By.XPath(@"//*[@id='filter-cadastral']")).Count > 0);
                    driver.FindElement(By.XPath(@"//*[@id='filter-name']")).SendKeys(_order.NumRequest);
                    Thread.Sleep(4500);
                    wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[1]/div/div/div[2]/div/form/div[2]/button[2]")).Count > 0);
                    driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[1]/div/div/div[2]/div/form/div[2]/button[2]")).Click();
                    Thread.Sleep(3000);
                    var Error_Nothing_Found = driver.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div[2]/div"));
                    var Error_At_Work = driver.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div/div[5]"));
                    if (Error_Nothing_Found.Count > 0) //в учётке нет запроса с таким номером
                    {
                        for (int i = 0; i < Error_Nothing_Found.Count; i++)
                        {
                            string Nothing_Found = Error_Nothing_Found[i].Text;
                            if (Nothing_Found == "Ничего не найдено")
                            {
                                RollBackToPrepared();
                                break;
                            }
                        }
                    }
                    //wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div")).Count > 0);
                    bool list_Result_true = driver.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div")).Count > 0;
                    var List_Result = driver.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div"));

                    if (list_Result_true)
                    {
                        if (List_Result.Count > 0)
                        {
                            while (List_Result.Count > 0)
                            {
                                if (List_Result.Count == 1)
                                {
                                    wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div/div[5]")));
                                    var result = driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div/div[5]")).Text;

                                    if (result == "Выполнено")
                                    {
                                        string destDirectory = Path.Combine(_savePath, "attachment");
                                        FileInfo fileInfo = new FileInfo(destDirectory + ".zip");
                                        DeleteUnnecessaryFiles(Path.Combine(destDirectory, _order.NumRequest));
                                        if (File.Exists(destDirectory + ".zip"))
                                            File.Delete(fileInfo.FullName);
                                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div/div[6]/a")));
                                        driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div/div[6]/a")).Click();
                                        Sleep(20000);
                                        BeginXmlProcessing();
                                        break;
                                    }
                                    else if (result == "На проверке")
                                    {
                                        _logger.Warn($"{_order}. Запрос ещё не обработан Росреестром. Повторю загрузку этой выписки позже");
                                        //Repository.RollbackToThePreviousVersion(_order.ID);
                                        break;
                                    }
                                    else if (result == "В работе")
                                    {
                                        Repository.RollbackToThePreviousVersion(_order.ID);
                                        _logger.Warn($"{_order}. Запрос есть, но не обрабатывается. Заказываем новую выписку");
                                        break;
                                    }
                                }
                                else
                                {
                                    wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[1]/div/div/div[2]/div/form/div[2]/button[2]")));
                                    driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[1]/div/div/div[2]/div/form/div[2]/button[2]")).Click();
                                    Thread.Sleep(2500);
                                }
                            }
                        }
                    }
                    driver.Close();
                    driver.Quit();
                    driver.Dispose(); 
                    Dispose();
                }
                catch (Exception ex)
                {
                    driver.Close();
                    driver.Quit();
                    driver.Dispose();
                    _logger.Error(ex);
                    Dispose();
                    return true;
                }
                finally
                {
                    foreach (var proc in Process.GetProcessesByName("IEDriverServer"))
                    {
                        proc.Kill();
                    }
                }
            }
            return true;
        }
        //private void Start1()
        //{
        //    CurrentThread.Name = "LoadWorker";

        //    try
        //    {
        //        while (!_worker.Done)
        //        {
        //            _worker.Deactivate();
        //            _worker.Init();

        //            if ((_searchForm = _worker.InitPipeline?.OpenNumberSearchFrom()) == null)
        //                continue;

        //            _downloadInfo = _searchForm.DownloadRequest(_order.NumRequest, _savePath);

        //            if (_downloadInfo.NoLink) // запрос есть, но еще не обработан
        //            {
        //                _logger.Warn($"{_order}. Запрос ещё не обработан Росреестром. Повторю загрузку этой выписки позже");
        //                break;
        //            }

        //            if (_downloadInfo.NoRequest) // в учётке нет запроса с таким номером
        //            {
        //                RollBackToPrepared();
        //                break;
        //            }
        //            else
        //            {
        //                _logger.Info($"{_order}. Архив успешно скачан.");
        //                _searchForm.Found = true;
        //            }

        //            if (_searchForm.Found)
        //            {
        //                BeginXmlProcessing();
        //                break;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error(ex);
        //    }
        //    finally
        //    {
        //        Dispose();
        //    }
        //}


        private void BeginXmlProcessing()
        {
            string destDirectory = Path.Combine(_savePath, "attachment");
            //FileInfo fileInfo = new FileInfo(_downloadInfo.FilePath);
            FileInfo fileInfo = new FileInfo(destDirectory + ".zip");
            UnzipFile(fileInfo, destDirectory);
        }


        private void UnzipFile(FileInfo fileInfo, string destDirectory)
        {
            _logger.Info($"Начинаю разархивацию для потока {CurrentThread.ManagedThreadId}");

            try
            {
                if (!Directory.Exists(destDirectory))
                    Directory.CreateDirectory(destDirectory);

                using (ZipFile zip = ZipFile.Read(fileInfo.FullName)) // основной zip-файл
                using (MemoryStream nestedZipMemory = new MemoryStream())
                using (MemoryStream xmlMemory = new MemoryStream())
                using (MemoryStream pdfMemory = new MemoryStream())
                using (StreamReader pdfMemoryReader = new StreamReader(pdfMemory, Encoding.GetEncoding(1251)))
                using (StreamReader xmlMemoryReader = new StreamReader(xmlMemory, Encoding.UTF8))
                {
                    ZipEntry entry = zip.Entries.FirstOrDefault(x => x.FileName.EndsWith(".zip"));

                    if (entry != null)
                    {
                        entry.Extract(nestedZipMemory);

                        nestedZipMemory.Position = 0;
                        using (ZipFile nestedZip = ZipFile.Read(nestedZipMemory)) // вложенный zip-файл
                        {
                            ZipEntry xmlEntry = nestedZip.Entries.FirstOrDefault(x => x.FileName.EndsWith(".xml"));
                            xmlEntry.Extract(xmlMemory);
                            xmlMemory.Position = 0;
                        }

                        string xmlString = xmlMemoryReader.ReadToEnd();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlString);

                        XmlParserFactory xmlReestrParser = new XmlParserFactory();
                        IXmlReestrParser parser = xmlReestrParser.GetReestrParser(doc);

                        string tmp = Path.Combine(destDirectory, parser.RequeryNumber + ".xml");
                        doc.Save(tmp);

                        string htmlText = parser.GetHtmlText();

                        File.WriteAllText(Path.Combine(destDirectory, parser.RequeryNumber + ".html"), htmlText);

                        _logger.Info($"Разархивированно успешно для потока {CurrentThread.ManagedThreadId}");

                        UploadXmlData(Path.Combine(destDirectory, _order.NumRequest + ".xml"), _order.ID, null);
                        DeleteUnnecessaryFiles(Path.Combine(destDirectory, _order.NumRequest));
                    }
                    else
                    {
                        ZipEntry xmlEntry = zip.Entries.FirstOrDefault(x => x.FileName.EndsWith(".xml"));
                        ZipEntry pdfEntry = zip.Entries.FirstOrDefault(x => x.FileName.EndsWith(".pdf"));



                        pdfEntry.Extract(pdfMemory);
                        pdfMemory.Position = 0;

                        //MemoryStream test = new MemoryStream();

                        //pdfMemoryReader.BaseStream.Seek(0, SeekOrigin.Begin);
                        //pdfMemoryReader.BaseStream.Position = 0;
                        //pdfMemoryReader.BaseStream.CopyTo(test);
                        //byte[] data = test.ToArray();

                        //var pdfTemp = File.ReadAllBytes(destDirectory);


                        xmlEntry.Extract(xmlMemory);
                        xmlMemory.Position = 0;

                        string xmlString = xmlMemoryReader.ReadToEnd();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlString);

                        XmlParserFactory xmlReestrParser = new XmlParserFactory();
                        IXmlReestrParser parser = xmlReestrParser.GetReestrParser(doc);


                        string tmp = Path.Combine(destDirectory, _order.NumRequest + ".xml");
                        doc.Save(tmp);

                        _logger.Info($"Разархивированно успешно для потока {CurrentThread.ManagedThreadId}");

                        UploadXmlData(Path.Combine(destDirectory, _order.NumRequest + ".xml"), _order.ID, pdfMemoryReader);
                        DeleteUnnecessaryFiles(Path.Combine(destDirectory, _order.NumRequest));
                    }
                }

                if (File.Exists(destDirectory + ".zip"))
                    File.Delete(fileInfo.FullName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }


        private void UploadXmlData(string fileName, int id, StreamReader pdfMemoryReader)
        {
            _logger.Info($"Загружаю файлы в базу данных для потока {CurrentThread.ManagedThreadId}");

            try
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
                string dirPath = Path.GetDirectoryName(fileName);

                // Загружаем xml-файл
                XmlDocument doc = new XmlDocument { PreserveWhitespace = false };
                doc.Load(fileName);

                XmlParserFactory xmlReestrParser = new XmlParserFactory();
                IXmlReestrParser parser = xmlReestrParser.GetReestrParser(doc);


                //string num = parser.RequeryNumber;
                if (pdfMemoryReader == null)
                {
                    string htmlText = null;
                    string htmlPath = Path.Combine(dirPath, fileNameNoExt + ".html");
                    if (File.Exists(htmlPath))
                        htmlText = File.ReadAllText(htmlPath);

                    AddXmlInfo(doc, htmlText, null, id);

                }
                else
                {

                    //pdfStream = Convert.ToBase64String(byte);
                    string pdfText = null;

                    //pdfText = pdfMemoryReader.ReadToEnd();
                    //byte[] pdf1 = Repository.Compress(pdfText);
                    //var resalt = Repository.DeCompressDoc(pdf1);
                    //File.WriteAllBytes("D:\\test.pdf", resalt);

                    AddXmlInfo(doc, null, pdfMemoryReader, id);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }


        private void AddXmlInfo(XmlDocument xml, string html, StreamReader pdfMemoryReader, int id)
        {
            try
            {
                XmlParserFactory xmlReestrParser = new XmlParserFactory();
                IXmlReestrParser reestr = xmlReestrParser.GetReestrParser(xml);

                string xmlData = xml.InnerXml;
                string htmlData = html;
                //string pdfData = pdf;
                string xslHref = reestr.XslHref;

                Repository.AddXmlData(id, xmlData, htmlData, pdfMemoryReader, xslHref);


                if (reestr.Persons.Count > 0)
                {
                    CreateEGRP(reestr.Persons, id);
                }
                else if (reestr.Resident.Count > 0)
                {
                    CreateEGRP_1(reestr.Resident, id);
                }
                else if (reestr.Municipality.Count > 0)
                {
                    CreateEGRP_2(reestr.Municipality, id);
                }

                if (reestr.Governances.Count > 0)
                    Repository.SetGovResult(id);

                if (reestr.Organizations.Count > 0)
                    Repository.SetOrganizationResult(id);

                if (reestr.Persons.Count == 0 && reestr.Governances.Count == 0 && reestr.Organizations.Count == 0 && reestr.Resident.Count == 0 && reestr.Municipality.Count == 0)
                {
                    Repository.SetNoXmlData(id);
                    Repository.CreateEGRP_NoXmlData(id);
                }
            }
            catch
            {
                throw;
            }
        }


        private void CreateEGRP(IEnumerable<XmlPerson> persons, int id)
        {
            _logger.Info($"Создаю ЕГРП для потока {CurrentThread.ManagedThreadId}");

            List<EGRP> egrpList = new List<EGRP>();

            foreach (XmlPerson person in persons)
            {
                egrpList.Add(new EGRP
                {
                    ID_Pipeline = id,
                    FIO = person.FullName.Trim(),
                    DateReg = person.RegDate,
                    Fraction = person.Fraction,
                    FullFraction = person.FullFraction
                });
            }

            Repository.CreateEGRP(egrpList);
        }

        private void CreateEGRP_1(IEnumerable<XmlResident> Resident, int id)
        {
            _logger.Info($"Создаю ЕГРП для потока {CurrentThread.ManagedThreadId}");

            List<EGRP> egrpList = new List<EGRP>();

            foreach (XmlResident person in Resident)
            {
                egrpList.Add(new EGRP
                {
                    ID_Pipeline = id,
                    FIO = person.FullName.Trim(),
                    DateReg = person.RegDate,
                    Fraction = person.Fraction,
                    FullFraction = person.FullFraction
                });
            }

            Repository.CreateEGRP(egrpList);
        }

        private void CreateEGRP_2(IEnumerable<XmlMunicipality> Municipality, int id)
        {
            _logger.Info($"Создаю ЕГРП для потока {CurrentThread.ManagedThreadId}");

            List<EGRP> egrpList = new List<EGRP>();

            foreach (XmlMunicipality person in Municipality)
            {
                egrpList.Add(new EGRP
                {
                    ID_Pipeline = id,
                    FIO = person.FullName.Trim(),
                    DateReg = person.RegDate,
                    Fraction = person.Fraction,
                    FullFraction = person.FullFraction
                });
            }

            Repository.CreateEGRP(egrpList);
        }

        private void DeleteUnnecessaryFiles(string file)
        {
            _logger.Info($"Удаляю ненужные файлы для потока {CurrentThread.ManagedThreadId}");

            string xml = file + ".xml";
            string html = file + ".html";

            if (File.Exists(xml))
                File.Delete(xml);

            if (File.Exists(html))
                File.Delete(html);
        }


        private void NotifySuccessMail(LoadOrder order)
        {
            var pair = Repository.GetEmailAndCadastral(order.Source, order.ID);

            if (pair == null || pair.Item1 == null)
                return;

            string subj = "Получена выписка";
            string msg = $"Выписка для кадастрового номера {pair.Item2} запроса {order.NumRequest} получена.";

            MailInfo info = new MailInfo(pair.Item1, subj, msg);

            bool sended = Notifier.Notify(info, out string error);

            if (sended)
                _logger.Info("Владелец уведомлён по E-mail адресу");
            else
                _logger.Error(error);

        }
    }
}