using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class TwoCaptchaClient
{
    public string APIKey { get; private set; }

    public TwoCaptchaClient(string apiKey)
    {
        APIKey = apiKey;
    }

    string captchaID;
    /// <summary>
    /// Sends a solve request and waits for a response
    /// </summary>
    /// <param name="googleKey">The "sitekey" value from site your captcha is located on</param>
    /// <param name="pageUrl">The page the captcha is located on</param>
    /// <param name="proxy">The proxy used, format: "username:password@ip:port</param>
    /// <param name="proxyType">The type of proxy used</param>
    /// <param name="result">If solving was successful this contains the answer</param>
    /// <returns>Returns true if solving was successful, otherwise false</returns>
    public bool SolveRecaptchaV2(string googleKey, string pageUrl, out string result) //string proxy, ProxyType proxyType,
    {
        string requestUrl = "http://2captcha.com/in.php?key=" + APIKey + "&method=userrecaptcha&googlekey=" + googleKey + "&pageurl=" + pageUrl;// + "&proxy=" + proxy + "&proxytype=";

        //switch (proxyType)
        //{
        //    case ProxyType.HTTP:
        //        requestUrl += "HTTP";
        //        break;
        //    case ProxyType.HTTPS:
        //        requestUrl += "HTTPS";
        //        break;
        //    case ProxyType.SOCKS4:
        //        requestUrl += "SOCKS4";
        //        break;
        //    case ProxyType.SOCKS5:
        //        requestUrl += "SOCKS5";
        //        break;
        //}

        try
        {
            var req = WebRequest.Create(requestUrl);

            using (WebResponse resp = req.GetResponse())
            using (StreamReader read = new StreamReader(resp.GetResponseStream()))
            {
                string response = read.ReadToEnd();

                if (response.Length < 3)
                {
                    result = response;
                    return false;
                }
                else
                {
                    if (response.Substring(0, 3) == "OK|")
                    {
                        Console.WriteLine("Капча отправлена на решение успешно");
                        captchaID = response.Remove(0, 3);
                        Console.WriteLine("ID капчи: " + captchaID);
                        for (int i = 0; i < 24; i++)
                        {
                            WebRequest getAnswer = WebRequest.Create("http://2captcha.com/res.php?key=" + APIKey + "&action=get&id=" + captchaID);

                            using (WebResponse answerResp = getAnswer.GetResponse())
                            using (StreamReader answerStream = new StreamReader(answerResp.GetResponseStream()))
                            {
                                string answerResponse = answerStream.ReadToEnd();

                                if (answerResponse.Length < 3)
                                {
                                    Console.WriteLine("Неверный ответ, при решении капчи");
                                    result = answerResponse;
                                    return false;
                                }
                                else
                                {
                                    if (answerResponse.Substring(0, 3) == "OK|")
                                    {
                                        Console.WriteLine("Капча успешно решена");
                                        result = answerResponse.Remove(0, 3);
                                        return true;
                                    }
                                    else if (answerResponse != "CAPCHA_NOT_READY")
                                    {
                                        result = answerResponse;
                                        return false;
                                    }
                                }
                            }
                            Console.WriteLine("Капча пока не решена, ожидаем 5 секунд");
                            Thread.Sleep(5000);
                        }

                        result = "Timeout";
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("Ошибка при решении капчи");
                        result = response;
                        return false;
                    }
                }
            }
        }
        catch { }

        result = "Unknown error";
        return false;
    }

    public void ReportBad()
    {
        file_get_contents("http://2captcha.com/res.php?key=" + APIKey + "&action=reportbad&id=" + captchaID);
        Console.WriteLine("Отправлен репорт на плохую почту");
    }

    public void ReportGood()
    {
        file_get_contents("http://2captcha.com/res.php?key=" + APIKey + "&action=reportgood&id=" + captchaID);
        Console.WriteLine("Капча прошла, отправлен гуд репорт");
    }

    static protected string file_get_contents(string fileName)
    {
        var startTime = System.Diagnostics.Stopwatch.StartNew();
        string sContents;
        while (true)
        {
            try
            {
                System.Net.WebClient wc = new System.Net.WebClient();
                byte[] response = wc.DownloadData(fileName);
                sContents = System.Text.Encoding.ASCII.GetString(response);
                break;
            }
            catch
            {
                Console.WriteLine("Произошла ошибка при подключении к сайту капчи. Повторная попытка через 1 секунду.");
            }
            Thread.Sleep(1000);
        }
        startTime.Stop();
        var resultTime = startTime.Elapsed;
        Console.WriteLine("Обращение к сайту капчи заняло: " + resultTime.Milliseconds * 0.001 + " секунды.");
        return sContents;
    }
}

public enum ProxyType
{
    HTTP,
    HTTPS,
    SOCKS4,
    SOCKS5
}
