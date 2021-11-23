using CommandLine;

namespace BroncoStatusChecker
{

    public class Options
    {
        [Option('v', "vin", Required = true, HelpText = "Ford Vehicle VIN")]
        public string Vin { get; set; }

        [Option('o', "order", Required = true, HelpText = "Ford Order Number")]
        public string OrderNumber { get; set; }

        [Option('s', "sid", Required = false, HelpText = "Twilio SMS SID (to receive SMS Messages)")]
        public string TwilioSID { get; set; }

        [Option('t', "token", Required = false, HelpText = "Twilio SMS Auth Token (to receive SMS Messages)")]
        public string TwilioAuthToken { get; set; }
        
        [Option('p', "phone", Required = false, HelpText = "Twilio SMS Phone Number (Set up at Twilio.com)")]
        public string TwilioPhoneNumber{ get; set; }

        [Option('m', "myphone", Required = false, HelpText = "Your Phone Number to receiove SMS")]
        public string PhoneNumber { get; set; }

        [Option('d', "Delay", Required = false, HelpText = "Minimum Delay Default 45mins")]
        public int Delay { get; set; } = 45;

        [Option('r', "random", Required = false, HelpText = "Maximum Random Wait (In addition to Minimum Delay Default: 15mins)")]
        public int Rnd { get; set; } = 15;


    }
}
