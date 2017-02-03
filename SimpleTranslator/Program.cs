using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleTranslator
{
    class Program
    {
        // 翻訳前言語コード
        private static string fromLangCode = "ja";
        // 翻訳語言語コード
        private static string toLangCode = "en";
        // 言語ドメイン
        private static string domain = "generalnn";
        static void Main(string[] args)
        {
            string textToTransform = "海外でWi-Fiをご利用される場合は、その国の法律に基づいた設定変更が必要になります。";

            CallTranslator callTranslator = new CallTranslator();
            string detectedLangCode = callTranslator.DetectMethod(textToTransform);
            string translatedText = callTranslator.TranslateMethod(textToTransform, detectedLangCode, toLangCode, domain);
            Console.WriteLine("翻訳元言語：" + detectedLangCode);
            Console.WriteLine("翻訳元文：" + textToTransform);
            Console.WriteLine("翻訳文：" + translatedText);
            Console.ReadLine();
        }
    }
    

    /*
      Microsoft Translatorの呼出
    */
    public class CallTranslator
    {
        // Subscription Key
        private const string SubscriptionKey = "<Cognitive TranslatorのSubscription Keyを入力>";
        // TranslateサービスのURL
        private static readonly Uri ServiceUrl = new Uri("http://api.microsofttranslator.com/v2/Http.svc/");

        public string TranslateMethod(string textToTransform, string fromLangCode, string toLangCode, string domain)
        {
            // トークンの取得
            var authTokenSource = new AzureAuthToken(SubscriptionKey);
            var token = string.Empty;
            token = authTokenSource.GetAccessToken();

            // トランスレーターサービスの呼出
            string uri = ServiceUrl + "Translate?text=" + System.Web.HttpUtility.UrlEncode(textToTransform) 
                + "&appid=" + token + "&from=" + fromLangCode + "&to=" + toLangCode + "&category=" + domain;
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            //httpWebRequest.Headers.Add("Authorization", "Bearer " + token);
            WebResponse response = null;
            string translatedText = null;

            try
            {
                response = httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String"));
                    translatedText = (string)dcs.ReadObject(stream);
                }
            } catch (Exception ex){
                Console.WriteLine("message: " + ex.Message);
                Console.WriteLine("stack trace: ");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }

            return translatedText;
        }

        public string DetectMethod(string textToTransform)
        {
            // トークンの取得
            var authTokenSource = new AzureAuthToken(SubscriptionKey);
            var token = string.Empty;
            token = authTokenSource.GetAccessToken();

            // Detectサービスの呼出
            string uri = ServiceUrl + "Detect?text=" + System.Web.HttpUtility.UrlEncode(textToTransform) + "&appid=" + token;
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            //httpWebRequest.Headers.Add("Authorization", "Bearer " + token);
            WebResponse response = null;
            string langCode = null;

            try
            {
                response = httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String"));
                    langCode = (string)dcs.ReadObject(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("message: " + ex.Message);
                Console.WriteLine("stack trace: ");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }

            return langCode;
        }
    }

    /*
      Cognitive Service Translator APIの認証トークンを取得
    */
    public class AzureAuthToken
    {
        // トークンサービスのURL
        private static readonly Uri ServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");
        // Subscription Keyを渡すときの要求ヘッダー
        private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";
        // トークンの有効時間：5分
        private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 5, 0);
        // 有効なトークンを格納
        private string storedTokenValue = string.Empty;
        // 有効なトークンの取得時間
        private DateTime storedTokenTime = DateTime.MinValue;

        /*
          Subscription Keyの取得
        */
        public string SubscriptionKey { get; private set; } = string.Empty;

        /*
         トークンサービスへのリクエスト時のHTTPステータスコードの取得
        */
        public HttpStatusCode RequestStatusCode { get; private set; }

        /*
          トークンを取得するためのクライアント作成
        */
        public AzureAuthToken(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key", "Subscription Keyが必要です。");
            }

            this.SubscriptionKey = key;
            this.RequestStatusCode = HttpStatusCode.InternalServerError;
        }

        /*
          Subscriptionに紐づいたトークンの取得 (非同期)
        */
        public async Task<string> GetAccessTokenAsync()
        {
            if (SubscriptionKey == string.Empty) return string.Empty;

            // トークンが有効な場合は有効なトークンを返す
            if ((DateTime.Now - storedTokenTime) < TokenCacheDuration)
            {
                return storedTokenValue;
            }

            // トークンを取得
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = ServiceUrl;
                request.Content = new StringContent(string.Empty);
                request.Headers.TryAddWithoutValidation(OcpApimSubscriptionKeyHeader, this.SubscriptionKey);
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = await client.SendAsync(request);
                this.RequestStatusCode = response.StatusCode;
                response.EnsureSuccessStatusCode();
                var token = await response.Content.ReadAsStringAsync();
                storedTokenTime = DateTime.Now;
                storedTokenValue = "Bearer " + token;
                return storedTokenValue;
            }
        }

        /*
          Subscriptionに紐づいたトークンの取得 (同期)
        */
        public string GetAccessToken()
        {
            // トークンが有効な場合は有効なトークンを返す
            if ((DateTime.Now - storedTokenTime) < TokenCacheDuration)
            {
                return storedTokenValue;
            }

            // トークンを取得
            string accessToken = null;

            var task = Task.Run(async () =>
            {
                accessToken = await GetAccessTokenAsync();
            });

            while (!task.IsCompleted)
            {
                System.Threading.Thread.Yield();
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
            else if (task.IsCanceled)
            {
                throw new Exception("トークンの取得がタイムアウトしました。");
            }

            return accessToken;
        }
    }
}
