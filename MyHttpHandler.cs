using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZelyaDushitelBot
{
    public class MyHttpHandler : DelegatingHandler
    {
        public DateTimeOffset? Time{get;set;}
        public MyHttpHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if(!response.IsSuccessStatusCode){
                if(response.StatusCode == System.Net.HttpStatusCode.TooManyRequests){
                    Time = response.Headers.RetryAfter.Date;
                }
            }
            return response;
        }
    }
}