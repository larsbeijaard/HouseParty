using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HouseParty
{
    public class Program
    {
        private string m_BridgeIP = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "ip.secret");
        private string m_AuthUserCode = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "authUser.secret");

        // All the lights that are registed on the Philips Hue
        private List<JObject> m_BridgeLights = new List<JObject>();

        public enum LoggerMessageType
        {
            INFO = 0,
            ERROR = 1,
            NOTE = 2
        }

        public static void Main(string[] _args)
        {
            Program _p = new Program();

            _p.GetAllLights();
            Console.ReadKey();
        }

        /// <summary>
        /// Find all lights that are registered on the Philips Hue
        /// </summary>
        private void GetAllLights()
        {
            WebClient _client = new WebClient();

            try
            {
                // Request for gathering info on the Bridge lights
                string _lightsJson = $"http://{m_BridgeIP}/api/{m_AuthUserCode}/lights";
                JObject _data = JObject.Parse(_client.DownloadString(_lightsJson));

                // Make sure there is atleast 1 light
                if (_data.Count <= 0)
                {
                    Logger("0 Bridge lights found! Press a key to exit...", LoggerMessageType.NOTE);
                    return;
                }

                Logger($"{_data.Count} Bridge lights found!");

                // All each light to the lights list (to scrap data from them later on)
                for (int _index = 0; _index < _data.Count; _index++)
                    m_BridgeLights.Add(_data);

                TurnLightsOn();
                FlickerLights();
            }
            catch (Exception _e)
            {
                Logger($"Oops, something went wrong!\nError: {_e.Message}", LoggerMessageType.ERROR);
                return;
            }
        }

        private void TurnLightsOn()
        {
            Logger("Trying to turn on the lights...");

            JObject _data = new JObject()
            {
                ["on"] = true
            };

            string _url = string.Empty;

            // Loop through each light and turn them on
            for (int _index = 1; _index <= m_BridgeLights.Count; _index++)
            {
                _url = $"http://{m_BridgeIP}/api/{m_AuthUserCode}/lights/{_index}/state";

                SendAPIRequest(_url, _data.ToString());
                Logger($"Light {_index} has turned on!");
            }
        }

        private void FlickerLights()
        {
            Logger("Starting task: Flickering lights...");

            // Loop through each light and set their brightness
            foreach (JObject _light in m_BridgeLights)
            {
                Task _task = new Task(() =>
                {
                    while (true)
                    {
                        FlickerLightsAsync(254);
                        Thread.Sleep(1500);
                        FlickerLightsAsync(0);
                        Thread.Sleep(1500);
                    }
                });

                _task.Start();
            }
        }

        private void FlickerLightsAsync(int _brightness)
        {
            JObject _data = new JObject()
            {
                ["bri"] = _brightness
            };

            string _url = string.Empty;

            for (int _index = 1; _index <= m_BridgeLights.Count; _index++)
            {
                _url = $"http://{m_BridgeIP}/api/{m_AuthUserCode}/lights/{_index}/state";
                SendAPIRequest(_url, _data.ToString());
            }
        }

        private void SendAPIRequest(string _url, string _data)
        {
            using WebClient _client = new WebClient();
            try
            {
                _client.UploadString(_url, "PUT", _data.ToString());
                Logger($"Send API request to: {_url} with the following data: {_data}", LoggerMessageType.NOTE);
            }
            catch (Exception _e)
            {
                Logger($"Oops, something went wrong!\nError: {_e.Message}", LoggerMessageType.ERROR);
                return;
            }
        }

        /// <summary>
        /// Logs messages to the console
        /// </summary>
        /// <param name="_message"> Message to log </param>
        /// <param name="_color"> Color of the message </param>
        private void Logger(string _message, LoggerMessageType _loggerMessageType = LoggerMessageType.INFO)
        {
            switch (_loggerMessageType)
            {
                case LoggerMessageType.INFO:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("INFO: ");
                    break;

                case LoggerMessageType.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("ERROR: ");
                    break;

                case LoggerMessageType.NOTE:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("NOTE: ");
                    break;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(_message);
        }
    }
}
