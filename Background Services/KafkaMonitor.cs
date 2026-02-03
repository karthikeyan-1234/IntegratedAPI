using Confluent.Kafka;

namespace IntegratedAPI.Background_Services
{
    public class KafkaMonitor : BackgroundService
    {
        private readonly IProducer<string, string> _producer;
        private readonly IConsumer<string, string> _consumer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private ILogger<KafkaMonitor> _logger;

        public KafkaMonitor(IProducer<string, string> producer,
            IConsumer<string, string> consumer,
            IServiceScopeFactory serviceScopeFactory, ILogger<KafkaMonitor> logger)
        {
            _producer = producer;
            _consumer = consumer;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;

            _logger.LogInformation("Kafka monitoring started");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(new[] { "product-added" }); //"product-removed", "product-changed" will be subscribed once topic is added
            await ListenForEvents(stoppingToken);
        }


        public async Task ListenForEvents(CancellationToken cancellationToken) {

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var cr = _consumer.Consume(cancellationToken);

                    if (cr != null)
                    {

                        switch (cr.Topic)
                        {

                            case "product-added":
                                _logger.LogInformation("Product added : " + cr.Message.Value);
                                break;

                            case "product-removed":
                                _logger.LogInformation("Product removed : " + cr.Message.Value);
                                break;

                            case "product-changed":
                                _logger.LogInformation("Product changed : " + cr.Message.Value);
                                break;

                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        
        }
    }
}
