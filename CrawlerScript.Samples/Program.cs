using System;
using System.Collections.Generic;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace CrawlerScript.Samples
{
    internal class Program
    {
        private static string account = "ZhangQueque";
        private static string password = "zhanghaodong138";
        private static string userAgent = "";
        public static class CnBlogs
        {
            //登陆地址
            public static string LoginUrl = "https://account.cnblogs.com/signin";
            //评论地址
            public static string CommentsUrl = "https://i.cnblogs.com/comments";
            public static string CommentsGetApi = "https://i.cnblogs.com/api/feedback/1?mine=false";
        }
        static void Main(string[] args)
        {
            var edgeOptions = new EdgeOptions();
            //大部分网站不会检测驱动，如果有检测根据提示添加所需要的参数即可。
            //无头模式，适合自动化任务，即没有浏览器窗口的模式
            //edgeOptions.AddArguments("--headless");
            //配置、浏览器的启动参数，以帮助隐藏 Selenium WebDriver 的自动化特征
            edgeOptions.AddArgument("--disable-blink-features=AutomationControlled");
            // 设置自定义的用户代理（可选,实测没啥用）
            //edgeOptions.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.0.0 Safari/537.36");
            using (IWebDriver webDriver = new EdgeDriver(edgeOptions))
            {
                //隐藏navigator自动化特征（可选,实测没啥用）
                //IJavaScriptExecutor jsDev = (IJavaScriptExecutor)webDriver;
                //jsDev.ExecuteScript("Object.defineProperty(navigator, 'plugins', {get: () => [1, 2, 3]});");
                //jsDev.ExecuteScript("Object.defineProperty(navigator, 'languages', {get: () => ['en-US', 'en']});");
                //jsDev.ExecuteScript("delete navigator.webdriver;");

                // 导航到目标网页
                webDriver.Navigate().GoToUrl(CnBlogs.LoginUrl);
                // 找到表单元素并填写
                //账号
                IWebElement accountElement = webDriver.FindElement(By.CssSelector("#mat-input-0"));
                accountElement.SendKeys(account);
                var element_account = accountElement.GetAttribute("value");
                //密码
                IWebElement passwordElement = webDriver.FindElement(By.CssSelector("#mat-input-1"));
                passwordElement.SendKeys(password);
                var element_password = passwordElement.GetAttribute("value");
                if (account != element_account || password != element_password)
                {
                    throw new Exception("页面账号密码文本框值核对错误！");
                }
                //点击登录按钮
                IWebElement loginBtnElement = webDriver.FindElement(By.CssSelector(@"body > app-root > app-sign-in-layout > div > div > app-sign-in > app-content-container > div > div > div > form > div > button"));
                //第一次点击，弹出验证
                loginBtnElement.Click();
                //点击验证按钮
                IWebElement verifyBtnElement = webDriver.FindElement(By.CssSelector(@"#rectMask"));
                //会发现阿里云自动化检测验证错误， 需要隐蔽自动化特征 ，注意27行代码
                verifyBtnElement.Click();

                // 设置显式等待登录成功
                WebDriverWait wait = new WebDriverWait(webDriver, TimeSpan.FromMilliseconds(2000)); // 2000 毫秒
                wait.Until(driver =>
                {
                    // 等待某个特定条件，例如页面标题变化或特定元素出现
                    // 这里以等待页面标题包含 "Dashboard" 为例
                    return driver.Title.Contains("博客园 - 开发者的网上家园");
                });

                // 导航到评论网页
                webDriver.Navigate().GoToUrl(CnBlogs.CommentsUrl);
                //获取自己的所有评论，3种方式，最后一种我称之为无敌

                #region 1、获取页面元素进行爬取     
                IWebElement tableElement = webDriver.FindElement(By.XPath(@"/html/body/cnb-root/cnb-app-layout/div[2]/as-split/as-split-area[2]/div/div/cnb-spinner/div/cnb-comment-main/cnb-spinner/div/div[2]/table"));
                // 提取表格主体
                IWebElement tbody = tableElement.FindElement(By.TagName("tbody"));
                IList<IWebElement> rows = tbody.FindElements(By.TagName("tr"));

                // 初始化列表来存储表格数据
                List<string> tableData = new List<string>();

                foreach (var row in rows)
                {
                    IList<IWebElement> cells = row.FindElements(By.TagName("td"));

                    // 如果表格包含表头，可能需要处理 <th> 单元格
                    if (cells.Count == 0)
                    {
                        cells = row.FindElements(By.TagName("th"));
                    }

                    List<string> cellTexts = new List<string>();

                    foreach (var cell in cells)
                    {
                        string cellText = cell.Text.Trim();
                        cellTexts.Add(cellText);
                    }

                    // 使用管道符作为分隔符，避免与数据中可能出现的逗号冲突
                    string rowData = string.Join(" | ", cellTexts);
                    tableData.Add(rowData);
                }

                // 输出提取的数据
                foreach (var row in tableData)
                {
                    Console.WriteLine(row);
                }
                #endregion

                #region 2、直接请求接口获取
                // 导航到接口地址
                webDriver.Navigate().GoToUrl(CnBlogs.CommentsGetApi);

                var data = webDriver.PageSource;

                Console.WriteLine(data);

                #endregion

                #region 3、伪造html元素，执行fetch脚本，获取元素内容
                // 导航到评论网页
                webDriver.Navigate().GoToUrl(CnBlogs.CommentsUrl);
                IJavaScriptExecutor sousuoexecutor = (IJavaScriptExecutor)webDriver;
                sousuoexecutor.ExecuteScript($@"
var container = document.createElement('div');
container.id = 'hiddenDataDiv';
container.style.display = 'none';
document.body.appendChild(container);
fetch(""https://i.cnblogs.com/api/feedback/1?mine=false"", {{
  ""headers"": {{
    ""accept"": ""application/json, text/plain, */*"",
    ""accept-language"": ""zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6"",
    ""priority"": ""u=1, i"",
    ""sec-ch-ua"": ""\""Microsoft Edge\"";v=\""131\"", \""Chromium\"";v=\""131\"", \""Not_A Brand\"";v=\""24\"""",
    ""sec-ch-ua-mobile"": ""?0"",
    ""sec-ch-ua-platform"": ""\""Windows\"""",
    ""sec-fetch-dest"": ""empty"",
    ""sec-fetch-mode"": ""cors"",
    ""sec-fetch-site"": ""same-origin""
  }},
  ""referrer"": ""https://i.cnblogs.com/comments"",
  ""referrerPolicy"": ""strict-origin-when-cross-origin"",
  ""body"": null,
  ""method"": ""GET"",
  ""mode"": ""cors"",
  ""credentials"": ""include""
}}).then(response => response.json())
.then(data => {{
    document.getElementById('hiddenDataDiv').textContent = JSON.stringify(data);
}})
.catch(error => console.error('Error:', error));");
                string jsonData = sousuoexecutor.ExecuteScript("return document.getElementById('hiddenDataDiv').textContent;").ToString();
                Console.WriteLine(jsonData);
                #endregion


            }

            Console.ReadKey();
        }
    }
}
