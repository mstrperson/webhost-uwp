using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WebhostUniversalWindows
{
    public static class WebhostAPICall
    {
        [DataContract]
        public class AuthenticationInfo
        {
            [DataMember(IsRequired = true)]
            public String EncodedCredential { get; set; }

            [DataMember(IsRequired = false, EmitDefaultValue = false)]
            public String Fingerprint { get; set; }
        }

        public static long JsonDate(DateTime date)
        {
            return date.ToBinary();
        }

        public static readonly String BaseUrl = "https://api.dublinschool.org/";
        //public static readonly String BaseUrl = "http://localhost:52693/";


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ConvertFromBase64String(string data)
        {
            if (String.IsNullOrEmpty(data))
                return "";

            MemoryStream ms = new MemoryStream();
            byte[] buffer = Convert.FromBase64String(data);
            ms.Write(buffer, 0, buffer.Length);
            ms.Position = 0;

            using (StreamReader reader = new StreamReader(ms))
            {
                String message = reader.ReadToEnd();
                return message;
            }
        }

        /// <summary>
        /// Pass a resource location such as "api/authenticate" and get back a valid Uri
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static Uri GetUri(String resource)
        {
            return new Uri(BaseUrl + resource);
        }

        /// <summary>
        /// Encode a string of data into base 64.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static String ConvertToBase64String(String data)
        {
            MemoryStream ms = new MemoryStream();
            byte[] buffer;
            using (StreamWriter writer = new StreamWriter(ms))
            {
                writer.WriteLine(data);
                writer.Flush();
                ms.Position = 0;
                buffer = new byte[ms.Length];
                ms.Read(buffer, 0, (int)ms.Length);
            }

            return Convert.ToBase64String(buffer);
        }



        /// <summary>
        /// Authenticate your application with a username and password.
        /// If authentication is successful, automatically stores credentials into the Application local settings.  Also returns an object containing the fingerprint if necessary.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="passwd"></param>
        /// <returns></returns>
        public async static Task<AuthenticationInfo> AuthenticateAsync(String username, String passwd)
        {
            String credentials = ConvertToBase64String(String.Format("username:{0};passwd:{1}", username, ConvertToBase64String(passwd)));
            HttpWebRequest request = HttpWebRequest.CreateHttp(GetUri("api/authenticate"));
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Method = "POST";

            String data = "{ \"EncodedCredential\":\"" + credentials + "\" }";

            // write the Json data from the Authentication object to the requestStream.
            using (var requestStream = new StreamWriter(await request.GetRequestStreamAsync()))
            {
                requestStream.Write(data);
            }

            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return OnAuthenticationSuccess(response);
                default:
                    throw new UnauthorizedAccessException(String.Format("Authentication Failed with status code {0}: {1}", response.StatusCode, response.StatusDescription));
            }
        }

        /// <summary>
        /// Stores the recieved fingerprint into the Windows application data local settings.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static AuthenticationInfo OnAuthenticationSuccess(HttpWebResponse response)
        {
            if (response.ContentLength > 0)
            {
                String responseBody;
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    responseBody = sr.ReadToEnd();

                }
                JsonSerializer serializer = JsonSerializer.Create();


                AuthenticationInfo info = serializer.Deserialize<AuthenticationInfo>(new JsonTextReader(new StringReader(responseBody)));

                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (settings.Values.ContainsKey("fingerprint"))
                {
                    settings.Values["fingerprint"] = info.Fingerprint;
                }
                else
                {
                    settings.Values.Add("fingerprint", info.Fingerprint);
                }

                return info;            
            }
            else
            {
                throw new SerializationException("Something went wrong deserializing response.");
            }
        }

        public static bool HasCredentials
        {
            get
            {
                return Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("fingerprint");
            }
        }

        /// <summary>
        /// Get the Authorization header value.
        /// </summary>
        private static String AuthorizationString
        {
            get
            {
                return String.Format("Webhost {0}", Windows.Storage.ApplicationData.Current.LocalSettings.Values["fingerprint"]);
            }
        }
        
        /// <summary>
        /// Get the Object returned by an authorized api request.
        /// </summary>
        /// <param name="resource">location of the resource relative to the base path</param>
        /// <param name="responseType">Type of data for the response</param>
        /// <param name="requestMethod">HTTP request method (Default "GET")</param>
        /// <param name="contentType">Data Type of request content, if applicable</param>
        /// <param name="content">Data to be serialized in the request content, if applicable</param>
        /// <returns></returns>
        public async static Task<object> GetObjectAsync(String resource, Type responseType, String requestMethod = "GET", Type contentType = null, object content = null)
        {
            
            HttpWebResponse response = await (contentType == null ? SendAuthorizedApiRequestAsync(resource, requestMethod) : SendAuthorizedApiRequestAsync(resource, requestMethod, contentType, content));

            if (response.StatusCode != HttpStatusCode.OK)
                throw new WebException("Response not OK", null, WebExceptionStatus.UnknownError, response);

            if (response.ContentLength <= 0)
                throw new WebException("Content Empty.", null, WebExceptionStatus.ReceiveFailure, response);

            String responseBody;
            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                responseBody = sr.ReadToEnd();
            }
            JsonSerializer serializer = JsonSerializer.Create();
            
            object info = serializer.Deserialize(new JsonTextReader(new StringReader(responseBody)), responseType);
            return info;
        }


        /// <summary>
        /// Send an authorized request to the Api.
        /// </summary>
        /// <param name="resource">location of the resource you are requesting relative to the base path</param>
        /// <param name="method">HTTP request method.</param>
        /// <returns>Api's response to your request.</returns>
        public async static Task<HttpWebResponse> SendAuthorizedApiRequestAsync(String resource, String method)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(GetUri(resource));
            request.Accept = "text/json";
            request.Method = method;
            request.Headers["Authorization"] = AuthorizationString;

            try
            {
                return (HttpWebResponse)await request.GetResponseAsync();
            }
            catch(WebException e)
            {
                return (HttpWebResponse)e.Response;
            }
        }

        /// <summary>
        /// Send an authorized api request with the given content.
        /// content must be either a String or a JsonSerializable object.
        /// </summary>
        /// <param name="resource">location of the resource you are accessing</param>
        /// <param name="method">HTTP Request Method</param>
        /// <param name="contentType">Data type of the content.</param>
        /// <param name="content">Content to be sent with the request.</param>
        /// <returns>Api's response to your request.</returns>
        public async static Task<HttpWebResponse> SendAuthorizedApiRequestAsync(String resource, String method, Type contentType, object content)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(GetUri(resource));
            request.Accept = "text/json";
            request.Method = method;
            request.Headers["Authorization"] = AuthorizationString;

            String data = "";
            if (content is String)
                data = (String)content;
            else
                data = JsonConvert.SerializeObject(content, contentType, new JsonSerializerSettings() { DateFormatHandling = DateFormatHandling.IsoDateFormat });

            // write the Json data from the Authentication object to the requestStream.
            using (var requestStream = new StreamWriter(await request.GetRequestStreamAsync()))
                requestStream.Write(data);

            try
            {
                return (HttpWebResponse)await request.GetResponseAsync();
            }
            catch (WebException e)
            {
                return (HttpWebResponse)e.Response;
            }
        }
    }    
}
