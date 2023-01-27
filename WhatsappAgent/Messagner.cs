using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;
using System.Threading;

namespace WhatsappAgent
{
    public class Messegner
    {
        private const string BASE_URL = "https://web.whatsapp.com/";
        private IWebDriver Driver;
        private string handle;

        public bool IsDisposed { get; set; } = false;

        public Messegner(BrowserType browserType = BrowserType.CHROME)
        {
            try
            {
                if(browserType== BrowserType.CHROME)
                {
                    var options = new ChromeOptions()
                    {
                        LeaveBrowserRunning = false,
                        UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                    };
                    options.AddArgument("--log-level=3");
                    Driver = new ChromeDriver(ChromeDriverService.CreateDefaultService(), options);
                }
                else
                {
                    throw new NotSupportedException("Browser not supported.");
                }

                Driver.Url = BASE_URL;

                foreach (var handle in Driver.WindowHandles)
                {
                    if (handle != null && !handle.Equals(Driver.CurrentWindowHandle))
                    {
                        Driver.SwitchTo().Window(handle);
                        Driver.Close();
                    }
                }

                handle = Driver.CurrentWindowHandle;

                new WebDriverWait(Driver, TimeSpan.FromSeconds(100)).Until(x => !Driver.WindowHandles.Contains(handle) || x.FindElements(By.CssSelector("[data-testid='input-placeholder']")).Count > 0);

                checkWindowState();
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
                Driver.Dispose();
            }
            catch (Exception)
            {
            }
            finally
            {
                IsDisposed = true;
            }
        }

        public void SendMessage(string number, string message)
        {
            try
            {
                Driver.Url = $"https://web.whatsapp.com/send?phone={number}&text&type=phone_number&app_absent=1";

                new WebDriverWait(Driver, TimeSpan.FromSeconds(10)).Until(x => !Driver.WindowHandles.Contains(handle) || x.FindElements(By.CssSelector("[data-testid='conversation-compose-box-input']")).Count > 0);

                checkWindowState();

                var textbox = Driver.FindElement(By.CssSelector("[data-testid='conversation-compose-box-input']"));
                foreach (var line in message.Split('\n'))
                {
                    textbox.SendKeys(line);
                    var actions = new Actions(Driver);
                    actions.KeyDown(Keys.Shift);
                    actions.KeyDown(Keys.Enter);
                    actions.KeyUp(Keys.Enter);
                    actions.KeyUp(Keys.Shift);
                    actions.Perform();
                }
                textbox.SendKeys(Keys.Enter);
                tryDismissAlert();
                var timenow = DateTime.Now;
                new WebDriverWait(Driver, TimeSpan.FromSeconds(10)).Until(x => DateTime.Now - timenow >= TimeSpan.FromMilliseconds(1000));
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
                var elms = Driver.FindElements(By.CssSelector("[data-testid='mi-logout menu-item']"));
                if (elms.Count > 0)
                {
                    elms.First().Click();
                    new WebDriverWait(Driver, TimeSpan.FromSeconds(3)).Until(x => !Driver.WindowHandles.Contains(handle) || x.FindElements(By.CssSelector("[data-testid='popup-controls-ok']")).Count > 0);

                    checkWindowState();

                    var confirmBtn = Driver.FindElement(By.CssSelector("[data-testid='popup-controls-ok']"));
                    confirmBtn.Click();
                    Thread.Sleep(4000);
                    Dispose();
                    return;
                }

                elms = Driver.FindElements(By.CssSelector("[data-testid='menu-bar-menu']"));
                if (elms.Count > 0)
                {
                    elms.First().Click();
                    new WebDriverWait(Driver, TimeSpan.FromSeconds(3)).Until(x => !Driver.WindowHandles.Contains(handle) || x.FindElements(By.CssSelector("[data-testid='mi-logout menu-item']")).Count > 0);

                    checkWindowState();

                    var logoutBtn = Driver.FindElement(By.CssSelector("[data-testid='mi-logout menu-item']"));
                    logoutBtn.Click();
                    new WebDriverWait(Driver, TimeSpan.FromSeconds(3)).Until(x => !Driver.WindowHandles.Contains(handle) || x.FindElements(By.CssSelector("[data-testid='popup-controls-ok']")).Count > 0);

                    checkWindowState();

                    var confirmBtn = Driver.FindElement(By.CssSelector("[data-testid='popup-controls-ok']"));
                    confirmBtn.Click();
                    Thread.Sleep(4000);
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

        private void checkWindowState()
        {
            if (!Driver.WindowHandles.Contains(handle)) {
                Dispose();
                throw new NoSuchWindowException("window closed.");
            }
        }

        private void tryDismissAlert()
        {
            try
            {
                Driver.SwitchTo().Alert().Accept();
            }
            catch (Exception)
            {
            }
        }
    }
}