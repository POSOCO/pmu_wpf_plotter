using PMU_Plotter.HistoryProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace PMU_Plotter
{
    class PMUAsyncFetcher
    {
        // HistoricalTrendProviderClient _serviceClient;
        Uri _serviceUri;
        string _userName;
        string _password;
        PhasorPointEndpointBehavior _endpointBehavior;
        HistoricalTrendProviderClient _serviceClient;
        ConfigurationManager _confiuration;
        public void Initialize(ConfigurationManager configuration)
        {
            _confiuration = configuration;
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            _endpointBehavior = new PhasorPointEndpointBehavior();
        }

        protected HistoricalTrendProviderClient CreateServiceClient()
        {
            _serviceUri = new UriBuilder("https", _confiuration.Host, _confiuration.Port, _confiuration.Path).Uri;
            _userName = _confiuration.UserName;
            _password = _confiuration.Password;

            EndpointAddress endpoint = new EndpointAddress(_serviceUri);

            var security = SecurityBindingElement.CreateUserNameOverTransportBindingElement();
            security.EnableUnsecuredResponse = true;
            security.IncludeTimestamp = false;
            security.MessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;

            CustomBinding binding = new CustomBinding();

            var mtomBinding = new MtomMessageEncodingBindingElement();
            mtomBinding.MaxBufferSize = 2147483647;
            binding.Elements.Add(mtomBinding);

            HttpTransportBindingElement httpTransport;

            //if ("http".Equals(_serviceUri.Scheme))
            //{
            //    //"http" supports the SoapUI MockService
            //    httpTransport = new HttpTransportBindingElement();
            //}
            //else
            //{
            binding.Elements.Add(security);
            httpTransport = new HttpsTransportBindingElement() { RequireClientCertificate = false };
            //}

            httpTransport.ManualAddressing = false;
            httpTransport.AllowCookies = false;
            httpTransport.AuthenticationScheme = AuthenticationSchemes.Anonymous;
            httpTransport.BypassProxyOnLocal = false;
            httpTransport.DecompressionEnabled = true;
            httpTransport.HostNameComparisonMode = HostNameComparisonMode.StrongWildcard;
            httpTransport.KeepAliveEnabled = false;
            httpTransport.ProxyAuthenticationScheme = AuthenticationSchemes.Anonymous;
            httpTransport.Realm = "";
            httpTransport.TransferMode = TransferMode.Buffered;
            httpTransport.UnsafeConnectionNtlmAuthentication = false;

            // Setting UseDefaultWebProxy to true slows down the first call to the web service
            httpTransport.UseDefaultWebProxy = false; // this differs from the WCF output

            // Default for the MaxReceivedMessageSize property is 65536 - too small in testing.
            httpTransport.MaxReceivedMessageSize = 2147483647;
            binding.Elements.Add(httpTransport);

            // SendTimeout – used to initialize the OperationTimeout, which governs the 
            // whole process of sending a message, including receiving a reply message for
            // a request/reply service operation. This timeout also applies when sending 
            // reply messages from a callback contract method.
            // PhasorPoint currently times out after 60 seconds.
            // Wait 30 extra seconds before declaring a timeout.
            // RequestTimeout should typically be 90 seconds.
            binding.SendTimeout = TimeSpan.FromSeconds(120);

            // Use a programmatic binding
            var serviceClient = new HistoricalTrendProviderClient(binding, endpoint);

            serviceClient.ClientCredentials.UserName.UserName = _userName;
            serviceClient.ClientCredentials.UserName.Password = _password;

            // validate cert by calling a function

            serviceClient.Endpoint.Behaviors.Add(_endpointBehavior);

            return serviceClient;
        }

        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        public Dictionary<object, List<PMUDataStructure>> GetData(DateTime startTime, DateTime endTime, List<int> measurementIDs, bool getFullData, bool getMinMax, int dataRate)
        {
            Dictionary<object, List<PMUDataStructure>> parsedData = null;
            try
            {
                DateTime utcStartTime = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Local, TimeZoneInfo.Utc);
                DateTime utcEndTime = TimeZoneInfo.ConvertTime(endTime, TimeZoneInfo.Local, TimeZoneInfo.Utc);

                utcStartTime = DateTime.SpecifyKind((utcStartTime), DateTimeKind.Utc);
                utcEndTime = DateTime.SpecifyKind((utcEndTime), DateTimeKind.Utc);

                TimeRangeElement tre = new TimeRangeElement();
                tre.startTime = utcStartTime;
                tre.endTime = utcEndTime;
                if (getMinMax)
                {
                    _serviceClient = CreateServiceClient();
                    //Thread.Sleep(1000);
                    _serviceClient.Open();
                    //Thread.Sleep(1000);

                    byte[] data = _serviceClient.GetPmuMinMaxData(tre, dataRate, measurementIDs.ToArray());

                    //Thread.Sleep(1000);

                    _serviceClient.Close();

                    //Thread.Sleep(1000);

                    PhasorPointBinaryDataParser parser = new PhasorPointBinaryDataParser();
                    parsedData = parser.Parse(data);
                    //Thread.Sleep(1000);

                    return parsedData;
                }
                else if (getFullData)
                {
                    _serviceClient = CreateServiceClient();
                    //Thread.Sleep(1000);
                    _serviceClient.Open();
                    //Thread.Sleep(1000);

                    byte[] data = _serviceClient.GetFullResolutionData(tre, measurementIDs.ToArray());
                    //Thread.Sleep(1000);
                    _serviceClient.Close();
                    //Thread.Sleep(1000);
                    // TemplateView.addItemsToConsole(templateName + " data fetch completed");
                    PhasorPointBinaryDataParser parser = new PhasorPointBinaryDataParser();
                    parsedData = parser.Parse(data);
                    //Thread.Sleep(1000);
                    // TemplateView.addItemsToConsole(templateName + " data parsing completed");

                    return parsedData;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: {0}", e);
                // TemplateView.addItemsToConsole("Error in " + templateName + " csv dumping -> " + e.Message);
                return null;
            }
            return null;
        }

        // created by sudhir on 07.11.2017
        private List<uint> GetMeasIDs(Dictionary<object, List<PMUDataStructure>> parsedData)
        {
            List<uint> measIDs = new List<uint>();
            foreach (var measData in parsedData)
            {
                string meas = measData.Key.ToString();
                uint measID = uint.Parse(meas);
                measIDs.Add(measID);
            }
            return measIDs;
        }

        // created by sudhir 30.12.2017
        public PMUMeasDataLists GetDataOfMeasId(Dictionary<object, List<PMUDataStructure>> parsedData, uint measId, bool checkMeasExistence = false)
        {
            if (checkMeasExistence)
            {
                // check if the measId is the measurements list of parsed Data
                List<uint> measIds = GetMeasIDs(parsedData);
                int measIndex = measIds.IndexOf(measId);
                if (measIndex == -1)
                {
                    return new PMUMeasDataLists();
                }
            }
            List<PMUDataStructure> pmuData = parsedData[measId];
            List<float> pmuVals = new List<float>();
            List<string> pmuQualities = new List<string>();
            List<DateTime> pmuTimeStamps = new List<DateTime>();
            for (int i = 0; i < pmuData.Count; i++)
            {
                PMUDataStructure data = parsedData[measId][i];
                pmuVals.Add(data.Value[0]);
                pmuQualities.Add(data.Quality.ToString());
                pmuTimeStamps.Add(DateTime.SpecifyKind((TimeZoneInfo.ConvertTime(data.TimeStamp, TimeZoneInfo.Utc, TimeZoneInfo.Local)), DateTimeKind.Local));
            }
            PMUMeasDataLists lists = new PMUMeasDataLists(pmuVals, pmuQualities, pmuTimeStamps);
            return lists;
        }
    }
}
