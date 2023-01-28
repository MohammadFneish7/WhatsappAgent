using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace WhatsappAgent
{
    public class Messegner
    {
        private const string BASE_URL = "https://web.whatsapp.com/";
        private string codebase = Directory.GetParent(Assembly.GetExecutingAssembly().FullName).FullName;
        private IWebDriver driver;
        private string handle;

        public bool IsDisposed { get; set; } = false;
        public delegate void OnDisposedEventHandler();
        public event OnDisposedEventHandler OnDisposed;

        public Messegner(BrowserType browserType = BrowserType.CHROME, uint login_timeout = 100)
        {
            try
            {

                if (browserType== BrowserType.CHROME)
                {
                    var options = new ChromeOptions()
                    {
                        LeaveBrowserRunning = false,
                        UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                    };
                    options.AddArgument($"--user-data-dir={codebase.Replace("\\","\\\\")}\\\\UserData");
                    var chromeDriverService = ChromeDriverService.CreateDefaultService();
                    chromeDriverService.HideCommandPromptWindow = true;
                    driver = new ChromeDriver(chromeDriverService, options);
                }
                else if(browserType == BrowserType.FIREFOX)
                {
                    var firefoxDriverService = FirefoxDriverService.CreateDefaultService();
                    firefoxDriverService.HideCommandPromptWindow = true;

                    driver = new FirefoxDriver(firefoxDriverService,new FirefoxOptions()
                    {
                        LogLevel = FirefoxDriverLogLevel.Fatal,
                        UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                    });
                }

                driver.Url = BASE_URL;

                foreach (var handle in driver.WindowHandles)
                {
                    if (handle != null && !handle.Equals(driver.CurrentWindowHandle))
                    {
                        driver.SwitchTo().Window(handle);
                        driver.Close();
                    }
                }

                handle = driver.CurrentWindowHandle;

                WaitForCSSElemnt("[data-testid='input-placeholder']", login_timeout);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                driver?.Dispose();
            }
            catch (Exception)
            {
            }
            finally
            {
                IsDisposed = true;
                OnDisposed?.Invoke();
            }
        }

        public void SendMessage(string number, string message, uint load_timeout = 30, uint ticks_timeout = 10)
        {
            try
            {
                CheckWindowState();

                driver.Url = $"https://web.whatsapp.com/send?phone={number}&text&type=phone_number&app_absent=1";

                var textbox = WaitForCSSElemnt("[data-testid='conversation-compose-box-input']", load_timeout);
                foreach (var line in message.Split('\n'))
                {
                    textbox.SendKeys(line);
                    var actions = new Actions(driver);
                    actions.KeyDown(Keys.Shift);
                    actions.KeyDown(Keys.Enter);
                    actions.KeyUp(Keys.Enter);
                    actions.KeyUp(Keys.Shift);
                    actions.Perform();
                }
                textbox.SendKeys(Keys.Enter);
                tryDismissAlert();

                WaitForLastMessage(ticks_timeout);
            }
            catch (NoSuchWindowException)
            {
                Dispose();
                throw;
            }
        }

        public void SendMedia(MediaType mediaType, string number, string path, string caption=null, uint load_timeout = 30, uint ticks_timeout = 20)
        {
            try
            {
                CheckWindowState();

                if (!File.Exists(path))
                    throw new FileNotFoundException(path);

                driver.Url = $"https://web.whatsapp.com/send?phone={number}&text&type=phone_number&app_absent=1";

                WaitForCSSElemnt("[data-testid='conversation-compose-box-input']", load_timeout);

                var clip = WaitForCSSElemnt("[data-testid='clip']");
                clip.Click();

                var attachImage = WaitForCSSElemnt($"[data-testid='{(mediaType == MediaType.IMAGE_OR_VIDEO ? "attach-image" : "attach-document")}']");
                var fileinput = attachImage.FindElement(By.XPath("../input"));
                fileinput.SendKeys(path);

                var textbox = WaitForCSSElemnt("[data-testid='media-caption-input-container']");
                if(!string.IsNullOrEmpty(caption))
                    foreach (var line in caption.Split('\n'))
                    {
                        textbox.SendKeys(line);
                        var actions = new Actions(driver);
                        actions.KeyDown(Keys.Shift);
                        actions.KeyDown(Keys.Enter);
                        actions.KeyUp(Keys.Enter);
                        actions.KeyUp(Keys.Shift);
                        actions.Perform();
                    }

                textbox.SendKeys(Keys.Enter);
                tryDismissAlert();

                WaitForLastMessage(10);
            }
            catch (NoSuchWindowException)
            {
                Dispose();
                throw;
            }
        }

        public void Logout()
        {
            try
            {
                var elms = driver.FindElements(By.CssSelector("[data-testid='mi-logout menu-item']"));
                if (elms.Count > 0)
                {
                    elms.First().Click();
                    var confirmBtn = WaitForCSSElemnt("[data-testid='popup-controls-ok']");
                    confirmBtn.Click();
                    Wait(4);
                    Dispose();
                    return;
                }

                elms = driver.FindElements(By.CssSelector("[data-testid='menu-bar-menu']"));
                if (elms.Count > 0)
                {
                    elms.First().Click();
                    var logoutBtn = WaitForCSSElemnt("[data-testid='mi-logout menu-item']");
                    logoutBtn.Click();

                    var confirmBtn = WaitForCSSElemnt("[data-testid='popup-controls-ok']");
                    confirmBtn.Click();
                    Wait(4);
                    Dispose();
                }
                else
                {
                    throw new Exception("unable to logout.");
                }
            }
            catch (NoSuchWindowException)
            {
                Dispose();
                throw;
            }
        }

        private void Wait(uint seconds)
        {
            var timenow = DateTime.Now;
            new WebDriverWait(driver, TimeSpan.FromSeconds(seconds)).Until(x => DateTime.Now - timenow >= TimeSpan.FromMilliseconds(seconds * 1000));
        }

        private IWebElement WaitForCSSElemnt(string selector, uint timeout = 3)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until(x => !driver.WindowHandles.Contains(handle) || x.FindElements(By.CssSelector(selector)).Count > 0);
            CheckWindowState();
            return driver.FindElement(By.CssSelector(selector));
        }

        private void WaitForLastMessage(uint seconds)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(seconds)).Until(x => {
                if (!driver.WindowHandles.Contains(handle))
                    return true;

                var elms = x.FindElements(By.CssSelector("[data-testid='msg-dblcheck']"));
                if (elms.Count > 0)
                {
                    var label = elms.Last().GetAttribute("aria-label").ToLower().Trim();
                    if (label.Equals("send") || label.Equals("delivered") || label.Equals("read"))
                    {
                        return true;
                    }
                }

                return false;
            });

            CheckWindowState();
        }

        private ReadOnlyCollection<IWebElement> WaitForCSSElemnts(string selector, int timeout = 3)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until(x => !driver.WindowHandles.Contains(handle) || x.FindElements(By.CssSelector(selector)).Count > 0);
            CheckWindowState();
            return driver.FindElements(By.CssSelector(selector));
        }

        private void CheckWindowState()
        {
            if (!driver.WindowHandles.Contains(handle)) {
                Dispose();
                throw new NoSuchWindowException("window closed.");
            }
        }

        private void tryDismissAlert()
        {
            try
            {
                driver.SwitchTo().Alert().Accept();
            }
            catch (Exception)
            {
            }
        }
    }
}