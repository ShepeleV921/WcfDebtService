using System;
using System.Collections.Generic;
using static System.Threading.Thread;
using System.Threading.Tasks;
using LoadPipeline.MailNotify;
using NLog;
using Tools;
using Tools.Classes;
using Tools.DAL;
using Tools.Models;
using Tools.Rosreestr;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Tools.Rosreestr.Rosreestr.Interfaces;

namespace PreparePipeline
{
    public class PrepareWorker : IWorker<UnpreparedOrder>, IDisposable
    {
        private readonly RosreestrPipeline _worker;         // 
        private readonly UnpreparedOrder _order;
        private readonly Queue<string> _correctStreets;
        private IRosreestrRealEstateSearchPipeline _searchForm;
        private IRosreestrRealEstateSearchResultsPipeline _resultForm;
        private int _rosrFailedResponseCount = 0;
        private List<AddressSearchInfoGos> _addressSearchInfoGos = new List<AddressSearchInfoGos>();

        private static int _activeThreadsCount;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static bool OutOfCapacity => _activeThreadsCount == SETTINGS.PREPARE_PIPELINE_THREAD_COUNT;


        public PrepareWorker(RosreestrPipeline worker, UnpreparedOrder order)
        {
            _worker = worker;
            _order = order;

            if (order.Street != null) _correctStreets = new Queue<string>(Repository.TryGetCorrectStreet(order.Street));
            else _correctStreets = new Queue<string>();

            
            _activeThreadsCount++;
            _logger.Info($"Начинаю заказ. Текущее число занятых потоков = {_activeThreadsCount}");
        }


        public void Run()
        {
            StartAsync().DoNotAwait();
        }

        private async Task StartAsync()
        {
            await Task.Run(() => Start());
        }

        private bool Start()
        {
            if (CurrentThread.Name == null)
            {
                CurrentThread.Name = "PrepareWorker";
            }
            string adr = AddressVerification(_order);

            var options = new ChromeOptions();
            //options.AddArgument("no-sandbox");
            //options.AddArgument("--start-maximized");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--incognito");
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
                    wait.Until(d => d.FindElements(By.XPath(@"//*[@id='react-select-3-input']")).Count > 0);
                    Thread.Sleep(2500);
                    driver.FindElement(By.XPath(@"//*[@id='react-select-3-input']")).SendKeys(adr);
                    Thread.Sleep(5500);
                    ////*[@id="personal-cabinet-root"]/div/div[2]/div/div/div/div
                    bool Missing_of_Addresses = driver.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/div/div/div[2]/div")).Count > 0;
                    if (Missing_of_Addresses == true) // Если нету результата в ростреестре "По заданным критериям поиска объекты не найдены"
                    {
                        SetNotFoundData();
                    }
                    else
                    {
                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/span")).Count > 0);
                        var list = driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/div/div[1]/div/div[2]")).FindElements(By.ClassName(@"rros-ui-lib-table__row"));
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (i < 10)
                            {
                                AddressSearchInfoGos test = new AddressSearchInfoGos();

                                test.FullAddress = (list[i].FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/div/div[1]/div/div[2]/div[" + (i + 1) + "]/div[2]")).Text);
                                test.CadastralNumber = (list[i].FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/div/div[1]/div/div[2]/div[" + (i + 1) + "]/div[1]/div/a")).Text).Trim();
                                _addressSearchInfoGos.Add(test);
                            }
                        }
                        SetSuccessNew();
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
                    Dispose();
                    _logger.Error(ex);
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
        //    CurrentThread.Name = "PrepareWorker";

        //    try
        //    {
        //        while (!_worker.Done)
        //        {
        //            _worker.Deactivate();
        //            _worker.Init();

        //            if ((_searchForm = _worker.InitPipeline?.OpenRealEstateSearchForm()) == null)
        //                continue;

        //            _logger.Info("Начинаю поиск");

        //            TryFindAddress();

        //            if (_resultForm == null)
        //            {
        //                if (_rosrFailedResponseCount != 5)
        //                {
        //                    _rosrFailedResponseCount++;
        //                    _logger.Warn($"NULL от Росреестра. Попытка {_rosrFailedResponseCount} из 5");
        //                    continue;
        //                }

        //                CancelOrdering();
        //                break;
        //            }

        //            if (_resultForm.NotFound && _correctStreets.Count != 0)
        //            {
        //                _order.Street = _correctStreets.Dequeue();
        //                continue;
        //            }

        //            if (_resultForm.NotFound)
        //            {
        //                SetNotFoundData();
        //                break;
        //            }

        //            for (int i = 0; i < _resultForm.Addresses.Count; ++i)
        //            {
        //                var form = _resultForm.OpenOrderForm(i, false);
        //                if (form == null)
        //                    return;

        //                _resultForm.Addresses[i].ChkAnnul = form.IsAnnul;

        //                if (form.Close() == null)
        //                    return;
        //            }

        //            SetSuccess();
        //            break;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error(ex);
        //        CancelOrdering();
        //    }
        //    finally
        //    {
        //        Dispose();
        //    }
        //}


        private void TryFindAddress()
        {
            if (_order.Town?.ToLower() == "верхнетемерницкий")
            {
                _order.City = "";
                _resultForm = _searchForm.SearchAddress("Ростовская область",
                                            _order.District,
                                            null,
                                            _order.Street,
                                            _order.Home?.ToString(),
                                            _order.Corp?.ToString(),
                                            _order.Flat?.ToString());
            }
            else
            {
                _resultForm = _searchForm.SearchAddress("Ростовская область",
                                            _order.District,
                                            string.IsNullOrEmpty(_order.City) ? _order.Town : _order.City,
                                            _order.Street,
                                            _order.Home?.ToString(),
                                            _order.Corp?.ToString(),
                                            _order.Flat?.ToString());
            }
        }

        public static string AddressVerification(UnpreparedOrder _order)
        {
            string adr = _order.Address;
            string str1 = "корп.";

            if (adr.Contains(str1))
            {
                Console.WriteLine("Строка:{0}" + "" + "содержит слово:{1}", adr, str1);
                adr = adr.Replace(", корп.", "/");
                return adr;
            }
            else
            {
                Console.WriteLine("Нет совпадений");
                return adr;
            }
        }

        public void Dispose()
        {
            _logger.Info($"Освободил поток. Текущее количество = {_activeThreadsCount}");
            _activeThreadsCount--;
        }


        private void SetSuccess()
        {
            _logger.Info($"Найденно {_resultForm.Addresses.Count}");
            Repository.AddPreparedData(_order, _resultForm.Addresses);
        }

        private void SetSuccessNew()
        {
            _logger.Info($"Найденно {_addressSearchInfoGos.Count}");
            Repository.AddPreparedDataNew(_order, _addressSearchInfoGos);
        }

        private void SetNotFoundData()
        {
            _logger.Info("данные не найдены. Обновляю запись в базе данных");
            Repository.SetNotFoundData(_order);
        }

        private void CancelOrdering()
        {
            _logger.Error("Ни одна из 5 попыток не получила адресс");
            Repository.SetAddressNotFound(_order);
        }

        private void NotifyFailureMail(string source, string msg)
        {
            string email = Repository.GetEmail(source);

            if (email == null)
                return;

            string subj = "Не удалось получить кадастровый номер";

            MailInfo info = new MailInfo(email, subj, msg);

            bool sended = Notifier.Notify(info, out string error);

            if (sended)
                _logger.Info("Владелец уведомлён по E-mail адресу");
            else
                _logger.Error(error);
        }
    }
}