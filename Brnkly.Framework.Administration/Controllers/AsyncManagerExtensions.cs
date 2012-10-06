using System;
using System.Net;
using System.Web.Mvc.Async;

namespace Brnkly.Framework.Administration.Controllers
{
    public static class AsyncManagerExtensions
    {
        public static void SendWebRequest(this AsyncManager asyncManager, string url, Action<string> onResponse)
        {
            CodeContract.ArgumentNotNull("onResponse", onResponse);

            using (var webClient = new WebClient())
            {
                webClient.UseDefaultCredentials = true;
                webClient.DownloadStringCompleted += (sender, args) =>
                {
                    onResponse(args.Result);
                    asyncManager.OutstandingOperations.Decrement();
                };

                asyncManager.OutstandingOperations.Increment();
                webClient.DownloadStringAsync(new Uri(url));
            }
        }
    }
}