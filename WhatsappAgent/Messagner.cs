using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Image = System.Drawing.Image;

namespace WhatsappAgent
{
    public class Messegner
    {
        private const string BASE_URL = "https://web.whatsapp.com";
        private readonly string codebase = Directory.GetParent(Assembly.GetExecutingAssembly().FullName).FullName;
        private readonly string tempPath = Path.GetTempPath();
        private readonly IWebDriver driver;
        private string handle;

        public bool IsDisposed { get; set; } = false;
        public delegate void OnDisposedEventHandler();
        public event OnDisposedEventHandler OnDisposed;

        public delegate void OnQRReadyEventHandler(Image qrbmp);
        public event OnQRReadyEventHandler OnQRReady;

        public Messegner(bool hideWindow = false)
        {
            try
            {
                var options = new ChromeOptions()
                {
                    LeaveBrowserRunning = false,
                    UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                };

                var chromeDir = Path.Combine(codebase, "chrome");
                var chromeExe = new FileInfo(Path.Combine(codebase, "chrome\\chrome.exe")); 
                var chromeDll = new FileInfo(Path.Combine(codebase, "chrome\\chrome.dll"));
                var chromeZip = new FileInfo(Path.Combine(codebase, "chrome\\chrome.zip"));

                if (!chromeDll.Exists)
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(chromeZip.FullName, chromeDir);
                }

                options.BinaryLocation = chromeExe.FullName;

                options.AddArgument($"--user-data-dir={tempPath.Replace("\\","\\\\")}\\\\Chrome\\\\UserData");
                if (hideWindow) {
                    options.AddArgument("--headless");
                    options.AddArgument("--disable-gpu");
                    options.AddArgument("--no-sandbox");
                    options.AddArgument($"user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{FileVersionInfo.GetVersionInfo(chromeExe.FullName).ProductVersion.Split('.')[0]}.0.0.0 Safari/537.36");
                }
                var chromeDriverService = ChromeDriverService.CreateDefaultService(chromeDir);
                chromeDriverService.HideCommandPromptWindow = true;
                driver = new ChromeDriver(chromeDriverService, options, TimeSpan.FromSeconds(100));
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        public void Login(uint login_timeout = 100)
        {
            try
            {
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

                WaitForQRAndLogin(login_timeout);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }


        private void WaitForQRAndLogin(uint login_timeout)
        {
            var foundQR = false;
            new WebDriverWait(driver, TimeSpan.FromSeconds(login_timeout)).Until(x => {
                if (!CheckWindowState(false))
                    return true;

                var elms = x.FindElements(By.CssSelector("#side"));
                if (elms.Count() > 0)
                    return true;

                if (!foundQR)
                {
                    elms = x.FindElements(By.CssSelector("canvas"));
                    if (elms.Count() > 0)
                    {
                        var qrcanvas = elms.First();
                        var qrbmp = GetQRCodeAsImage(qrcanvas);
                        OnQRReady?.Invoke(qrbmp);
                        foundQR = true;
                    }
                }

                return false;
            });

            CheckWindowState();
        }

        private Image GetQRCodeAsImage(IWebElement ele)
        {
            //First way
            //// Get entire page screenshot
            //var screenshot = driver.TakeScreenshot();
            //Image img = null;
            //using (var stream = new MemoryStream(screenshot.AsByteArray)){
            //    img = Image.FromStream(stream);
            //}
            //// Get the location of element on the page
            //Point point = ele.Location;

            //// Get width and height of the element
            //int eleWidth = ele.Size.Width;
            //int eleHeight = ele.Size.Height;

            //// Crop the entire page screenshot to get only element screenshot
            //Bitmap bmpImage = new Bitmap(img);
            //return bmpImage.Clone(new Rectangle(point.X, point.Y, eleWidth, eleHeight), bmpImage.PixelFormat);

            var base64Img = driver.ExecuteJavaScript<string>("return arguments[0].toDataURL('image/png').substring(22);", ele);
            
            Image img = null;
            using (var stream = new MemoryStream(Convert.FromBase64String(base64Img)))
            {
                img = Image.FromStream(stream);
            }
            return img;
        }

        public void Dispose()
        {
            try
            {
                driver?.Quit();
            }
            catch (Exception)
            {

                throw;
            }
            try
            {
                driver?.Dispose();
            }
            catch (Exception)
            {
            }
            finally
            {
                try
                {
                    IsDisposed = true;
                    OnDisposed?.Invoke();
                }
                catch (Exception)
                {
                    
                }
            }
        }

		public void SendMessageInCurrentChat(string message, uint ticks_timeout = 10, uint wait_after_send = 2)
		{
			try
			{
				var textbox = driver.FindElement(By.CssSelector("[aria-label=\"Send\"]"));
                foreach (var line in message.Split('\n').Where(x => x.Trim().Length > 0))
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
				TryDismissAlert();

				WaitForLastMessage(ticks_timeout);
				Wait(wait_after_send);
			}
			catch (NoSuchWindowException)
			{
				Dispose();
				throw;
			}
		}

		public void SendMessage(string number, string message, uint load_timeout = 30, uint ticks_timeout = 10, uint wait_after_send=2)
        {
            try
            {
                if(string.IsNullOrEmpty(number))
                    throw new ArgumentException(nameof(number) + " is required.");

                if (string.IsNullOrEmpty(message))
                    throw new ArgumentException(nameof(message) + " is required.");

                CheckWindowState();

                driver.Url = $"https://web.whatsapp.com/send?phone={number}&text={HttpUtility.UrlEncode(message)}&type=phone_number&app_absent=1";

                var textbox = WaitForCSSElemnt("[aria-label=\"Send\"]", load_timeout);
                //foreach (var line in message.Split('\n').Where(x => x.Trim().Length > 0))
                //{
                //    textbox.SendKeys(line);
                //    var actions = new Actions(driver);
                //    actions.KeyDown(Keys.Shift);
                //    actions.KeyDown(Keys.Enter);
                //    actions.KeyUp(Keys.Enter);
                //    actions.KeyUp(Keys.Shift);
                //    actions.Perform();
                //}
                textbox.SendKeys(Keys.Enter);
                TryDismissAlert();

                WaitForLastMessage(ticks_timeout);
                Wait(wait_after_send);
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

                if (string.IsNullOrEmpty(number))
                    throw new ArgumentException(nameof(number) + " is required.");

                if (!File.Exists(path))
                    throw new FileNotFoundException(path);

                var fi = new FileInfo(path);
                if (fi.Length == 0 || fi.Length > 16000000)
                    throw new ArgumentException("File size out of allowed bounds [1Byte, 16MB].");

                driver.Url = $"https://web.whatsapp.com/send?phone={number}&text&type=phone_number&app_absent=1";

                WaitForCSSElemnt("[title='Type a message']", load_timeout);

                var clip = WaitForCSSElemnt("[title='Attach']");
                clip.Click();

                var fileinput = WaitForCSSElemnt($"{(mediaType == MediaType.IMAGE_OR_VIDEO ? "input[accept*='image']" : "input[accept='*']")}");
                fileinput.SendKeys(path);

                var textbox = WaitForCSSElemnt("[title='Type a message']");
                Wait(3); 
                if (!string.IsNullOrEmpty(caption))
                    foreach (var line in caption.Split('\n').Where(x=>x.Trim().Length>0))
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
                TryDismissAlert();

                WaitForLastMessage(ticks_timeout);
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
                var elms = driver.FindElements(By.CssSelector("header [title='Menu']"));
                if (elms.Count > 0)
                {
                    elms.First().Click();
                    var logoutBtn = WaitForCSSElemnt("[aria-label='Log out']");
                    logoutBtn.Click();

                    var confirmBtn = WaitForCSSElemnt("[role='dialog'] button:nth-child(2)");
                    confirmBtn.Click();
                    Wait(8);
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
            new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until(x => !CheckWindowState(false) || x.FindElements(By.CssSelector(selector)).Count > 0);
            CheckWindowState();
            return driver.FindElement(By.CssSelector(selector));
        }

        private void WaitForLastMessage(uint seconds)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(seconds)).Until(x => {
                if (!CheckWindowState(false))
                    return true;

                var elms = x.FindElements(By.CssSelector(".message-out"));
                if (elms.Count > 0)
                {
                    var labels = elms.Last().FindElements(By.CssSelector("[data-icon='msg-dblcheck'], [data-icon='msg-check']"));
                    if (labels.Count > 0)
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
            new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until(x => !CheckWindowState(false) || x.FindElements(By.CssSelector(selector)).Count > 0);
            CheckWindowState();
            return driver.FindElements(By.CssSelector(selector));
        }

        private bool CheckWindowState(bool raiseError = true)
        {
            if (!driver.WindowHandles.Contains(handle) || !driver.Url.StartsWith(BASE_URL, StringComparison.InvariantCultureIgnoreCase)) {
                Dispose();
                if (raiseError)
                    throw new NoSuchWindowException("window closed.");
                else
                    return false;
            }
            return true;
        }

        private void TryDismissAlert()
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