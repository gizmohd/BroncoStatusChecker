
using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using CommandLine;

namespace BroncoStatusChecker
{
    partial class Program
    {
        private static bool SMSEnabled = false;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                if (!string.IsNullOrWhiteSpace(o.TwilioAuthToken) && !string.IsNullOrWhiteSpace(o.TwilioSID))
                {
                    TwilioClient.Init(o.TwilioSID, o.TwilioAuthToken);
                    SMSEnabled = true;
                }
                do
                {
                    Console.WriteLine($"{DateTime.Now } : Checking status.....");

                    CheckForBroncoChanges(o.Vin, o.OrderNumber, o.TwilioPhoneNumber, o.PhoneNumber);
                    Console.WriteLine($"{DateTime.Now } : Waiting {o.Delay}mins...");
                    System.Threading.Thread.Sleep(1000 * 60 * o.Delay);
                    var rnd = R.Next(0, o.Rnd);

                    if (rnd > 0)
                    {
                        Console.WriteLine($"Imposing Random wait of {rnd} Minutes");
                        System.Threading.Thread.Sleep(1000 * 60 * rnd);
                    }


                } while (true);
            });



        }

        static readonly Random R = new(DateTime.Now.Millisecond);

        private static void CheckForBroncoChanges(string vin, string orderNumber, string twilioPhoneNumber, string recipientPhoneNumber)
        {

            var palsAppResults = "https://palsapp.com/Search/AnonymousVinStatus/"
                .AppendPathSegment(vin)
                .WithHeader("origin", "https://palsapp.com")
                .WithHeader("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36 Edg/95.0.1020.53")
                .PostAsync().ReceiveJson<JObject>().Result;

            var x = (JObject)((JArray)palsAppResults.SelectToken("s"))[0];

            var pDestination = x["Destination"];
            var pDealerName = x["DealerName"];
            var pDescription = x["Description"];
            bool updateDetected = false;

            (string tmpHash, JObject jsonDoc) = CheckFordForUpdates(vin, orderNumber);

            tmpHash = CreateMD5($"{tmpHash}{pDescription}{pDestination}{pDealerName}");

            string existingHash = null;
            bool firstCheck = true;
            updateDetected = false;
            if (System.IO.File.Exists("hash.dat"))
            {
                existingHash = System.IO.File.ReadAllText("hash.dat");
                firstCheck = false;
            }
            if (!firstCheck)
                updateDetected = !tmpHash.Equals(existingHash);

            if (updateDetected)
            {
                System.IO.File.WriteAllText("hash.dat", tmpHash);
                Console.ForegroundColor = ConsoleColor.Yellow;
                StringBuilder sb = new();
                sb.AppendLine("Bronco Status Change Detected:");
                sb.AppendLine($"primaryStatus={jsonDoc["primaryStatus"]}");
                sb.AppendLine($"releaseDate={jsonDoc["releaseDate"]}");
                sb.AppendLine($"shipmentDate={jsonDoc["shipmentDate"]}");
                sb.AppendLine($"transitDate={jsonDoc["transitDate"]}");
                sb.AppendLine($"etaStartDate={jsonDoc["etaStartDate"]}");
                sb.AppendLine($"etaEndDate={jsonDoc["etaEndDate"]}");

                sb.AppendLine("PalsApp:");
                sb.AppendLine($"Destination={pDestination}");
                sb.AppendLine($"DealerName={pDealerName}");
                sb.AppendLine($"Description={pDescription}");



                Console.WriteLine(sb.ToString());
                if (SMSEnabled)
                    Console.WriteLine("... Sending SMS!");

                Console.ResetColor();

                if (SMSEnabled)
                    MessageResource.Create(
                             body: sb.ToString(),
                             from: new Twilio.Types.PhoneNumber(twilioPhoneNumber),
                             to: new Twilio.Types.PhoneNumber(recipientPhoneNumber)
                     );
            }
            else
            {
                Console.WriteLine("No Change Detected....");
            }
        }

        private static (string, JObject) CheckFordForUpdates(string vin, string orderNumber)
        {

            string tmpHash;
            JObject jsonDoc = "https://shop.ford.com/aemservices/shop/vot/api/customerorder/?orderNumber=&partAttributes=BP2_.*&vin="
                            .SetQueryParam("vin", vin)
                            .SetQueryParam("orderNumber", orderNumber)
                            .WithHeader("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9")
                            .WithHeader("accept-encoding", "gzip, deflate, br")
                            .WithHeader("accept-language", "en-US,en;q=0.9")
                            .WithHeader("cache-control", "max-age=0")

                            .WithHeader("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.71 Safari/537.36 Edg/94.0.992.38")
                            .GetJsonAsync<JObject[]>()
                            .Result[0];
            jsonDoc.SelectToken("custOrderPartsInfo ").Parent.Remove();
            jsonDoc.SelectToken("custOrderImgToken ").Parent.Remove();

            var json = JsonConvert.SerializeObject(jsonDoc, new JsonSerializerSettings()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                Formatting = Formatting.None,
            });


            tmpHash = CreateMD5(json);


            return (tmpHash, jsonDoc);
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
