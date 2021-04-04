using System;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;

namespace Boyd.DataBuses.Impl.Duplexes
{
    /// <summary>
    /// Type of connection to MQTT Broker
    /// </summary>
    public enum MQTTConnType
    {
        /// <summary>
        /// Use a TCP Transport for the MQTT connection
        /// </summary>
        TCP,
        /// <summary>
        /// Use a WS transport for the MQTT connection
        /// </summary>
        WS
    }

    /// <summary>
    /// Databus abstraction for a MQTT broker
    /// </summary>
    internal class MQTTDataBus<T1, T2> : BaseDataBus<T1, T2>
    {
        private readonly ILogger _logger;
        private readonly MQTTConnType _connType;
        private readonly IMqttClientOptions _clientOptions;
        private readonly string _host;
        private readonly int _port;
        private readonly bool _useTls;
        private SecureString _password;
        private SecureString _username;
        private readonly string _readTopic;
        private readonly string _writeTopic;
        private readonly Guid _guidOnly;
        private readonly IMqttClient _client;
        private readonly int _reconnectDelaySeconds;
        private readonly CancellationTokenSource _cancelSource;
        private readonly ISerializer<T1> _serializer;
        private readonly IDeserializer<T2> _deserializer;

        /// <summary>
        /// constructor for MQTT Data bus
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        /// <param name="deserializer"></param>
        public MQTTDataBus(ILoggerFactory loggerFactory,
            DataBusOptions options,
            ISerializer<T1> serializer,
            IDeserializer<T2> deserializer) : base(options, loggerFactory)
        {
            _reconnectDelaySeconds = 1;
            _serializer = serializer;
            _deserializer = deserializer;
            _cancelSource = new CancellationTokenSource();
            _guidOnly = Guid.NewGuid();
            _logger = loggerFactory.CreateLogger<MQTTDataBus<T1, T2>>();
            _host = options.SupplementalSettings["host"];
            _port = int.Parse(options.SupplementalSettings["port"]);
            bool.TryParse(options.SupplementalSettings["useTls"], out _useTls);
            _connType = Enum.Parse<MQTTConnType>(options.SupplementalSettings["connType"]);
            _username = new SecureString();
            _password = new SecureString();
            if (options.SupplementalSettings.ContainsKey("username"))
            {
                for (var i = 0; i < options.SupplementalSettings["username"].Length; i++)
                {
                    _username.AppendChar(options.SupplementalSettings["username"][i]);
                }
            }

            if (options.SupplementalSettings.ContainsKey("password"))
            {
                for (var i = 0; i < options.SupplementalSettings["password"].Length; i++)
                {
                    _username.AppendChar(options.SupplementalSettings["password"][i]);
                }
            }

            _readTopic = options.SupplementalSettings["readTopic"];
            _writeTopic = options.SupplementalSettings["writeTopic"];

            _clientOptions = BuildMQTTOptions();
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _client.UseDisconnectedHandler(DisconnectHandler);
            _client.UseConnectedHandler(ConnectHandler);
            _client.UseApplicationMessageReceivedHandler(MessageRecv);
        }

        private Task MessageRecv(MqttApplicationMessageReceivedEventArgs e)
        {
            var mess = _deserializer.Deserialize(e.ApplicationMessage.Payload);
            AddToQueue(mess);
            return Task.CompletedTask;
        }
        
        private async Task ConnectHandler(MqttClientConnectedEventArgs e)
        {
            await _client.SubscribeAsync(_readTopic, MqttQualityOfServiceLevel.ExactlyOnce);
        }

        private async Task DisconnectHandler(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogWarning("### DISCONNECTED FROM SERVER ### {0}", e.Exception);
            await Task.Delay(TimeSpan.FromSeconds(_reconnectDelaySeconds));

            try
            {
                await _client.ConnectAsync(_clientOptions, _cancelSource.Token);
            }
            catch
            {
                _logger.LogWarning("### RECONNECTING FAILED ###");
            }
        }

        private IMqttClientOptions BuildMQTTOptions()
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(_guidOnly.ToString());
            var uriString = $"{_host}:{_port.ToString()}";
            if (_connType == MQTTConnType.WS)
            {
                options = options.WithWebSocketServer(uriString);
            }
            else
            {
                options = options.WithTcpServer(uriString);
            }

            if (_useTls)
            {
                options = options.WithTls();
            }

            if (_username.Length > 0 && _password.Length > 0)
            {
                options = options.WithCredentials(_username.ToString(), _password.ToString());
            }
            return options
                .WithCleanSession()
                .Build();
        }

        protected override async Task SendData(T1 data, CancellationToken token)
        {
            await _client.PublishAsync(_writeTopic, Encoding.UTF8.GetString(_serializer.Serialize(data).ToArray()));
        }

        protected override Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            return TakeFromQueue(pObjTimeout, token);
        }

        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && !_readStopEvent.WaitOne(0, false))
                {
                    _readStopEvent.WaitOne(TimeSpan.FromMilliseconds(100));
                }
                _cancelSource.Cancel();
                await _client.DisconnectAsync();
            });
        }
    }
}