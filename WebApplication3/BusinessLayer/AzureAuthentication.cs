using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;

namespace WebApplication3.BusinessLayer
{
    public static class AzureAuthentication
    {
        public static readonly string domain = ConfigurationManager.AppSettings["Domain"];
        public static async Task<string> createToken()
        {
            var appId = ConfigurationManager.AppSettings["AppID"];
            var tenantID = ConfigurationManager.AppSettings["TenantID"];
            var redirectURl = ConfigurationManager.AppSettings["RedirectUrl"];
            var adminuserName = ConfigurationManager.AppSettings["AdminUserID"];
            var password = ConfigurationManager.AppSettings["AdminPassword"];
            var scopes = new string[] { "User.Read" };

            IPublicClientApplication client = PublicClientApplicationBuilder.Create(appId).WithTenantId(tenantID).WithRedirectUri(redirectURl).Build();
            AuthenticationResult authResult = null;
            try
            {
                SecureString securePassword = new SecureString();
                foreach (char c in password)
                {
                    securePassword.AppendChar(c);
                }

                authResult = await client.AcquireTokenByUsernamePassword(scopes, adminuserName, securePassword).ExecuteAsync();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return authResult.AccessToken;
        }

        public static bool DoesUserExistsAsync(string user, string AccessToken)
        {

            var myUri = new Uri($"https://graph.microsoft.com/v1.0/users/{user}");
            var myWebRequest = WebRequest.Create(myUri);
            var myHttpWebRequest = (HttpWebRequest)myWebRequest;
            myHttpWebRequest.PreAuthenticate = true;
            myHttpWebRequest.Headers.Add("Authorization", "Bearer " + AccessToken);
            try
            {
                var myWebResponse = myWebRequest.GetResponse();
                var responseStream = myWebResponse.GetResponseStream();
                if (responseStream == null) return false;

                var myStreamReader = new StreamReader(responseStream, Encoding.Default);
                var json = myStreamReader.ReadToEnd();

                responseStream.Close();
                myWebResponse.Close();

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static async Task CreateUserAsync(string user, string token)
        {
            Random random = new Random();
            int length = random.Next(8, 15);
            int nonaplhavchar = random.Next(1, length - 3);
            string password = Membership.GeneratePassword(length, nonaplhavchar);
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                var payload = new
                {
                    accountEnabled = true,
                    displayName = user,
                    mailNickname = user.Replace(" ", ""),
                    userPrincipalName = $"{user.Replace(" ", "")}@{domain}",
                    passwordProfile = new
                    {
                        forceChangePasswordNextSignIn = true,
                        password = password
                    }
                };
                var payloadText = JsonConvert.SerializeObject(payload);

                writer.Write(payloadText);
                writer.Flush();
                stream.Flush();
                stream.Position = 0;

                using (var content = new StreamContent(stream))
                {
                    content.Headers.Add("Content-Type", "application/json");
                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await client.PostAsync("https://graph.microsoft.com/v1.0/users/", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(response.ReasonPhrase);
                    }
                }
            }
        }

        public static async Task UpdateUserAsync(string user, string password, string token)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                var payload = new
                {
                    passwordProfile = new
                    {
                        forceChangePasswordNextSignIn = false,
                        password = password
                    }
                };
                var payloadText = JsonConvert.SerializeObject(payload);


                user = user.Replace(" ", "");
                writer.Write(payloadText);
                writer.Flush();
                stream.Flush();
                stream.Position = 0;

                using (var content = new StreamContent(stream))
                {
                    content.Headers.Add("Content-Type", "application/json");
                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var response = await client.PatchAsync($"https://graph.microsoft.com/v1.0/users/{user}@{domain}", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(response.ReasonPhrase);
                    }
                }
            }
        }

        public static void UpdateUser(string user, string password, string token)
        {
            user = user.Replace(" ", "");
            var myUri = new Uri($"https://graph.microsoft.com/v1.0/users/{user}@{domain}");
            var myWebRequest = WebRequest.Create(myUri);
            var myHttpWebRequest = (HttpWebRequest)myWebRequest;
            myHttpWebRequest.Method = "PATCH";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.PreAuthenticate = true;
            myHttpWebRequest.Headers.Add("Authorization", "Bearer " + token);
            var payload = new
            {
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = false,
                    password = password
                }
            };
            var data = JsonConvert.SerializeObject(payload);
            using (var streamWriter = new StreamWriter(myHttpWebRequest.GetRequestStream()))
            {
                string json = data;
                streamWriter.Write(json);
                streamWriter.Flush();
            }
            //WebResponse myWebResponse = null;
            try
            {
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

                var responseStream = myHttpWebResponse.GetResponseStream();

                var myStreamReader = new StreamReader(responseStream, Encoding.Default);
                var json = myStreamReader.ReadToEnd();

                responseStream.Close();
                myHttpWebResponse.Close();


            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    string statusCode = ((HttpWebResponse)e.Response).StatusCode.ToString();
                    string statusDesc = ((HttpWebResponse)e.Response).StatusDescription;
                    using (StreamReader r = new StreamReader(((HttpWebResponse)e.Response).GetResponseStream()))
                    {
                        var data1 = r.ReadToEnd();
                    }
                }
            }
        }

        public static void DeleteUser(string user, string token)
        {
            user = user.Replace(" ", "");
            var myUri = new Uri($"https://graph.microsoft.com/v1.0/users/{user}@{domain}");
            var myWebRequest = WebRequest.Create(myUri);
            var myHttpWebRequest = (HttpWebRequest)myWebRequest;
            myHttpWebRequest.Method = "DELETE";
            myHttpWebRequest.PreAuthenticate = true;
            myHttpWebRequest.Headers.Add("Authorization", "Bearer " + token);

            try
            {
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

                var responseStream = myHttpWebResponse.GetResponseStream();

                var myStreamReader = new StreamReader(responseStream, Encoding.Default);
                var json = myStreamReader.ReadToEnd();

                responseStream.Close();
                myHttpWebResponse.Close();


            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    string statusCode = ((HttpWebResponse)e.Response).StatusCode.ToString();
                    string statusDesc = ((HttpWebResponse)e.Response).StatusDescription;
                    using (StreamReader r = new StreamReader(((HttpWebResponse)e.Response).GetResponseStream()))
                    {
                        var data1 = r.ReadToEnd();
                    }
                }
            }
        }
    }



    public static class HttpClientExtensions
    {
        public async static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
        {
            var method = new HttpMethod("PATCH");

            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = content
            };

            return await client.SendAsync(request);
        }
    }
}