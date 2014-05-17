using Newtonsoft.Json;
using System;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;
using WapAPI.Models;

namespace WapAPI
{
    class Program
    {
        private const string subscriptionId = "386dd878-64b8-41a7-a02a-644f646e2df8";
        private const string BaseUrl = "https://api-cloud.syfuhs.net/subscriptions/" + subscriptionId;

        private const string StsEndpoint = "https://auth-cloud.syfuhs.net/wstrust/issue/usernamemixed";

        static void Main(string[] args)
        {
            Uri requestUri = new Uri(BaseUrl);

            string certThumbprint = "B2488696C145739434B1EB998466166A4A332827";

            X509Certificate2 certificate = LocateCertificate(certThumbprint);

            HttpWebRequestCertificateTest(requestUri, certificate);
            Console.WriteLine("===================================");

            HttpClientCertificateTest(requestUri, certificate).Wait();
            Console.WriteLine("===================================");

            var jwtToken = RequestJwtToken();

            HttpWebRequestJwtTest(requestUri, jwtToken);
            Console.WriteLine("===================================");

            HttpClientJwtTest(requestUri, jwtToken).Wait();
            Console.WriteLine("===================================");

            Console.ReadLine();
        }

        private static string RequestJwtToken()
        {
            var binding = new WS2007HttpBinding(SecurityMode.TransportWithMessageCredential);
            
            binding.Security.Message.EstablishSecurityContext = false;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;

            var factory = new WSTrustChannelFactory(binding, new EndpointAddress(new Uri(StsEndpoint)));
            factory.TrustVersion = TrustVersion.WSTrust13;

            factory.Credentials.UserName.UserName = "steve@syfuhs.net";
            factory.Credentials.UserName.Password = "supersecretshiny";

            var channel = factory.CreateChannel();

            RequestSecurityTokenResponse rstr = null;

            var resp = channel.Issue(new RequestSecurityToken()
            {
                RequestType = RequestTypes.Issue,
                TokenType = "urn:ietf:params:oauth:token-type:jwt",
                AppliesTo = new EndpointReference("http://azureservices/TenantSite")
            }, out rstr);

            GenericXmlSecurityToken xmlToken = resp as GenericXmlSecurityToken;

            if (xmlToken == null)
                return null;

            return Encoding.Unicode.GetString(Convert.FromBase64String(xmlToken.TokenXml.InnerXml));
        }

        private static async Task HttpClientJwtTest(Uri requestUri, string jwtToken)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = requestUri;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var resp = await client.GetAsync(requestUri.AbsolutePath);

            var sub = await resp.Content.ReadAsAsync<Subscription>();

            Console.WriteLine("SubId: " + sub.SubscriptionID);
        }

        private static async Task HttpClientCertificateTest(Uri requestUri, X509Certificate2 certificate)
        {
            WebRequestHandler handler = new WebRequestHandler();
            handler.ClientCertificates.Add(certificate);

            HttpClient client = new HttpClient(handler);

            client.BaseAddress = requestUri;

            var resp = await client.GetAsync(requestUri.AbsolutePath);

            var sub = await resp.Content.ReadAsAsync<Subscription>();

            Console.WriteLine("SubId: " + sub.SubscriptionID);
        }

        private static void HttpWebRequestJwtTest(Uri requestUri, string jwtToken)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUri);

            request.Headers.Add("x-ms-version", "2010-10-28");
            request.Method = "GET";
            request.ContentType = "application/json";

            request.Headers.Add("Authorization", "Bearer " + jwtToken);

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                ProcessResponse(response);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("------------------------------------");

                ProcessResponse(e.Response as HttpWebResponse);
            }
        }

        private static void HttpWebRequestCertificateTest(Uri requestUri, X509Certificate2 certificate)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUri);

            request.Headers.Add("x-ms-version", "2010-10-28");
            request.Method = "GET";
            request.ContentType = "application/json";

            request.ClientCertificates.Add(certificate);

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                ProcessResponse(response);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("------------------------------------");

                ProcessResponse(e.Response as HttpWebResponse);
            }
        }

        private static X509Certificate2 LocateCertificate(string certThumbprint)
        {
            X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);

            var certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, certThumbprint, false);
            certStore.Close();

            if (0 == certCollection.Count)
            {
                throw new Exception("Error: No certificate found containing thumbprint " + certThumbprint);
            }

            X509Certificate2 certificate = certCollection[0];
            return certificate;
        }

        private static void ProcessResponse(HttpWebResponse response)
        {
            Console.WriteLine("Response status code: " + response.StatusCode);

            for (int i = 0; i < response.Headers.Count; i++)
                Console.WriteLine("{0}: {1}", response.Headers.Keys[i], response.Headers[i]);

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var sub = JsonConvert.DeserializeObject<Subscription>(reader.ReadToEnd());

                Console.WriteLine("SubId: " + sub.SubscriptionID);
            }
            else
            {
                Console.WriteLine("Response output:");
                Console.WriteLine(reader.ReadToEnd());
            }

            response.Close();
            responseStream.Close();
            reader.Close();
        }
    }
}
