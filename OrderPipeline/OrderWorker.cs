using System;
using static System.Threading.Thread;
using System.Threading.Tasks;
using LoadPipeline.MailNotify;
using NLog;
using Tools;
using Tools.Classes;
using Tools.DAL;
using Tools.Models;
using Tools.Rosreestr;
using System.Threading;
using Tools.Rosreestr.Rosreestr.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Linq;
using OpenQA.Selenium.DevTools.V112.Network;
using System.Diagnostics;

namespace OrderPipeline
{
    public class OrderWorker : IWorker<PreparedOrder>, IDisposable
    {
        private readonly RosreestrPipeline _worker;
        private readonly PreparedOrder _order;
        private IRosreestrRealEstateSearchPipeline _searchForm;
        private IRosreestrRealEstateSearchResultsPipeline _resultForm;
        private IRosreestrOrderFormPipeline _orderForm;
        private int _rosrFailedResponseCount = 0;

        private static int _activeThreadsCount;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static bool OutOfCapacity => _activeThreadsCount == SETTINGS.ORDER_PIPELINE_THREAD_COUNT;


        public OrderWorker(RosreestrPipeline worker, PreparedOrder order)
        {
            _worker = worker;
            _order = order;

            _activeThreadsCount++;
            _logger.Info($"Начинаю заказ. Текущее число занятых потоков = {_activeThreadsCount}");
            Repository.SetBusyOrder(_worker.LoginKey, _order.CadastralNumber);
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
            var options = new ChromeOptions();
            //options.AddArgument("no-sandbox");
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
                    Thread.Sleep(2000);
                    driver.FindElement(By.XPath(@"//*[@id='react-select-3-input']")).SendKeys(_order.CadastralNumber);
                    Thread.Sleep(4000);
                    bool NothingFound = driver.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/div/div/div[2]/div")).Count > 0;
                    if (NothingFound)
                    {
                        CancelOrdering();
                    }
                    else
                    {
                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/div/div[1]/div/div[2]/div/div[2]")).Count > 0);
                        driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/div/div[1]/div/div[2]/div/div[2]")).Click();
                        Thread.Sleep(5500);
                        bool Error = driver.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div")).Count > 0;
                        if (Error == true) // Дудосим сайт 
                        {
                            while (Error == true)
                            {
                                driver.FindElement(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div/div/div/button")).Click();
                                wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/div/div[1]/div/div[2]/div/div[2]")).Count > 0);
                                driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div/div[2]/div/div[1]/div/div[2]/div/div[2]")).Click();
                                Error = driver.FindElements(By.XPath(@"//*[@id='personal-cabinet-root']/div/div[2]/div")).Count > 0;
                                Thread.Sleep(10_000);
                            }
                        }
                        wait.Until(d => d.FindElements(By.XPath(@"//*[@class='rros-ui-lib-modal-container']/div/div[2]/div/div[1]/div[1]/div[2]/div/div/button")).Count > 0);
                        Thread.Sleep(1000);
                        driver.FindElement(By.XPath(@"//*[@class='rros-ui-lib-modal-container']/div/div[2]/div/div[1]/div[1]/div[2]/div/div/button")).Click();
                        Thread.Sleep(1500);
                        wait.Until(d => d.FindElements(By.XPath(@"/html/body/div[13]/div/div/div[1]/div[2]/div/div")).Count > 0);
                        driver.FindElement(By.XPath(@"/html/body/div[13]/div/div/div[1]/div[2]/div/div")).Click();
                        Thread.Sleep(6500);
                        driver.Navigate().GoToUrl(@"https://lk.rosreestr.ru/request-access-egrn/my-claims");
                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='filter-cadastral']")).Count > 0);
                        driver.FindElement(By.XPath(@"//*[@id='filter-cadastral']")).SendKeys(_order.CadastralNumber.Trim());
                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[1]/div/div/div[2]/div/form/div[2]/button[2]")).Count > 0);
                        driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[1]/div/div/div[2]/div/form/div[2]/button[2]")).Click();
                        Thread.Sleep(7500);
                        wait.Until(d => d.FindElements(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div/div[1]")).Count > 0);
                        Thread.Sleep(1500);
                        var test_list_itogo = driver.FindElement(By.XPath(@"//*[@id='main-page-wrapper']/div[2]/div[2]/div[2]/div/div[1]/div/div[2]/div/div[1]"));
                        string RequestNumber = test_list_itogo.Text;

                        _logger.Info($"Номер {RequestNumber} Успешно получен. Отдыхаю 5 минут");
                        Repository.SetAsPrepared(_order, DateTime.Now, RequestNumber, _worker.LoginKey);
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

        private void Start1()
        {
            CurrentThread.Name = "OrderWorker";

            try
            {
                while (!_worker.Done)
                {
                    _worker.Deactivate();
                    _worker.Init();

                    if ((_searchForm = _worker.InitPipeline?.OpenRealEstateSearchForm()) == null)
                        continue;

                    _logger.Info("Начинаю поиск");

                    _resultForm = _searchForm.SearchAddress("Ростовская область", _order.CadastralNumber);

                    if (_resultForm == null)
                    {
                        if (_rosrFailedResponseCount != 5)
                        {
                            _logger.Warn($"NULL от Росреестра. Попытка {_rosrFailedResponseCount} из 5");
                            _rosrFailedResponseCount++;
                            continue;
                        }

                        CancelOrdering();
                        break;
                    }

                    if (_resultForm.Addresses.Count == 0)
                    {
                        SetNoAddressesFound();
                        break;
                    }

                    if (_resultForm.Addresses.Count > 1)
                    {
                        SetMoreThanOneAddressFound();
                        break;
                    }

                    _logger.Info("Адреса успешно получены");

                    _orderForm = _resultForm.OpenOrderForm(0, true);

                    if (_orderForm == null)
                        continue;

                    if (_orderForm.IsAnnul)
                    {
                        SetAnnulOrder();
                        break;
                    }

                    if ((_orderForm = _orderForm.AddCaptcha()) == null || _worker.HasError)
                    {
                        _logger.Error("Сбой ввода капчи");
                        continue;
                    }

                    ProcessOrderForm();
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                Dispose();
            }
        }


        private void ProcessOrderForm()
        {
            _orderForm.EnterCaptcha(_orderForm.ResolvedCaptcha);

            if (_worker.HasError)
                return;

            _orderForm.Send();

            if (_orderForm.HasTimeout)
            {
                _logger.Info("Таймаут. Отдыхаю 5 минут");
                Sleep(300_000);
            }

            if (_orderForm.HasSuccess)
            {
                _logger.Info($"Номер {_orderForm.RequestNumber} Успешно получен. Отдыхаю 5 минут");
                Repository.SetAsPrepared(_order, DateTime.Now, _orderForm.RequestNumber, _worker.LoginKey);
                Sleep(300_000);
            }
        }


        public void Dispose()
        {
            Repository.SetFreeOrder(_worker.LoginKey);
            _activeThreadsCount--;
            _logger.Info($"Освободил поток. Текущее количество = {_activeThreadsCount}");
        }


        private void SetAnnulOrder()
        {
            Repository.SetAnul(_order);
            NotifyFailureMail(_order.Source, $"Кадастровый номер {_order.CadastralNumber} анулирован");
            _logger.Warn("АНУЛИРОВАН");
        }


        private void CancelOrdering()
        {
            _logger.Error("Ни одна из 5 попыток не получила адресс");
            Repository.SetIncorrect(_order, _worker.LoginKey);
        }


        private void SetNoAddressesFound()
            => Repository.SetNoAddressesFound(_order, _worker.LoginKey);


        private void SetMoreThanOneAddressFound()
            => Repository.SetMoreThanOneAddressesFound(_order, _worker.LoginKey);


        private void NotifyFailureMail(string source, string msg)
        {
            string email = Repository.GetEmail(source);

            if (email == null)
                return;

            string subj = "Не удалось заказать выписку";

            MailInfo info = new MailInfo(email, subj, msg);

            bool sended = Notifier.Notify(info, out string error);

            if (sended)
                _logger.Info("Владелец уведомлён по E-mail адресу");
            else
                _logger.Error(error);
        }
    }
}