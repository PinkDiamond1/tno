using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TNO.API.Areas.Services.Models.Content;
using TNO.Entities;
using TNO.Kafka;
using TNO.Kafka.Models;
using TNO.Services.Managers;
using TNO.Services.NLP.Config;
using TNO.Services.NLP.OpenNLP;

namespace TNO.Services.NLP;

/// <summary>
/// NLPManager class, provides a common way to manager the actions this service performs.
/// Each time the manager is run it will execute NLP processes to extract data.
/// </summary>
public class NLPManager : ServiceManager<NLPOptions>
{
    #region Variables
    private readonly EntityExtractor _extractor = new();
    private Task? _consumer;
    private readonly TaskStatus[] _notRunning = new TaskStatus[] { TaskStatus.Canceled, TaskStatus.Faulted, TaskStatus.RanToCompletion };
    #endregion

    #region Properties
    /// <summary>
    /// get - Kafka Consumer object.
    /// </summary>
    protected IKafkaListener<string, NLPRequest> Consumer { get; private set; }

    /// <summary>
    /// get - Kafka message producer.
    /// </summary>
    protected IKafkaMessenger Producer { get; private set; }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new instance of a NLPManager object, initializes with specified parameters.
    /// </summary>
    /// <param name="consumer"></param>
    /// <param name="producer"></param>
    /// <param name="api"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public NLPManager(
        IKafkaListener<string, NLPRequest> consumer,
        IKafkaMessenger producer,
        IApiService api,
        IOptions<NLPOptions> options,
        ILogger<NLPManager> logger)
        : base(api, options, logger)
    {
        this.Consumer = consumer;
        this.Producer = producer;
        this.Consumer.OnError += ConsumerErrorHandler;
    }
    #endregion

    #region Methods
    /// <summary>
    /// Continues to update the Kafka consumer if the configuration changes.
    /// </summary>
    /// <returns></returns>
    public async override Task RunAsync()
    {
        var delay = _options.DefaultDelayMS;

        // Always keep looping until an unexpected failure occurs.
        while (true)
        {
            if (this.State.Status != ServiceStatus.Running)
            {
                this.Logger.LogDebug("The service is not running: '{Status}'", this.State.Status);
            }
            else
            {
                try
                {
                    var topics = _options.Topics.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (topics.Length != 0)
                    {
                        if (!this.Consumer.IsReady) this.Consumer.Open();
                        this.Consumer.Subscribe(topics);

                        // Create a new thread if the prior one isn't running anymore.
                        if (_consumer == null || _notRunning.Contains(_consumer.Status))
                        {
                            _consumer = Task.Factory.StartNew(() => ConsumerHandler());
                        }
                    }
                    else if (topics.Length == 0)
                    {
                        this.Consumer.Stop();
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Service had an unexpected failure.");
                    this.State.RecordFailure();
                }
            }

            // The delay ensures we don't have a run away thread.
            this.Logger.LogDebug("Service sleeping for {delay} ms", delay);
            await Task.Delay(delay);
        }
    }

    /// <summary>
    /// Keep consuming messages from Kafka until the service stops running.
    /// </summary>
    /// <returns></returns>
    private async Task ConsumerHandler()
    {
        while (this.State.Status == ServiceStatus.Running && this.Consumer.IsReady)
        {
            await this.Consumer.ConsumeAsync(HandleMessageAsync);
        }

        // The service is stopping or has stopped, consume should stop too.
        this.Consumer.Stop();
    }

    /// <summary>
    /// The Kafka consumer has failed for some reason, need to record the failure.
    /// Fatal or unexpected errors will result in a request to stop consuming.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private bool ConsumerErrorHandler(object sender, ErrorEventArgs e)
    {
        this.State.RecordFailure();
        if (e.GetException() is ConsumeException ex)
        {
            return ex.Error.IsFatal;
        }

        // Inform the consumer it should stop.
        return this.State.Status != ServiceStatus.Running;
    }

    /// <summary>
    /// Retrieve a file from storage and send to Microsoft Cognitive Services. Obtain
    /// the transcription and update the content record accordingly.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task HandleMessageAsync(ConsumeResult<string, NLPRequest> result)
    {
        // The service has stopped, so to should consuming messages.
        if (this.State.Status != ServiceStatus.Running)
        {
            this.Consumer.Stop();
            this.State.Stop();
        }

        var content = await _api.FindContentByIdAsync(result.Message.Value.ContentId);
        if (content != null)
        {
            await UpdateContentAsync(content);
            await SendIndexingRequest(content);
        }
        else
        {
            // Identify requests for transcription for content that does not exist.
            this.Logger.LogWarning("Content does not exist for this message. Key: {Key}, Content ID: {ContentId}", result.Message.Key, result.Message.Value.ContentId);
        }

        // Successful run clears any errors.
        this.State.ResetFailures();
    }

    /// <summary>
    /// Make a request to generate a transcription for the specified 'content'.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private async Task UpdateContentAsync(ContentModel content)
    {
        this.Logger.LogInformation("Transcription requested.  Content ID: {Id}", content.Id);

        var labels = await RequestNlpAsync(content); // TODO: Extract language from data source.

        // Fetch content again because it may have been updated by an external source.
        // This can introduce issues if the transcript has been edited as now it will overwrite what was changed.
        var result = await _api.FindContentByIdAsync(content.Id);
        if (result != null && labels.Any())
        {
            // Only add new labels.
            var originalLabels = new List<ContentLabelModel>(result.Labels);
            foreach (var label in labels)
            {
                var original = result.Labels.FirstOrDefault(l => l.Key == label.Key && l.Value == label.Value);
                if (original == null) originalLabels.Add(label);
            }
            result.Labels = originalLabels.ToArray();

            await _api.UpdateContentAsync(result); // TODO: This can result in an editor getting a optimistic concurrency error.
            this.Logger.LogInformation("Labels updated.  Content ID: {Id}", content.Id);
        }
        else if (!labels.Any())
        {
            this.Logger.LogWarning("Content did not generate a labels. Content ID: {Id}", content.Id);
        }
        else
        {
            // The content is no longer available for some reason.
            this.Logger.LogError("Content no longer exists. Content ID: {Id}", content.Id);
        }
    }

    /// <summary>
    /// Make a request to OpenNLP models to extract information from content.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    private Task<ContentLabelModel[]> RequestNlpAsync(ContentModel content)
    {
        var labels = new List<ContentLabelModel>();

        labels.AddRange(ExtractEntity(content, EntityType.Person));
        labels.AddRange(ExtractEntity(content, EntityType.Organization));
        labels.AddRange(ExtractEntity(content, EntityType.Location));
        labels.AddRange(ExtractEntity(content, EntityType.Date));
        labels.AddRange(ExtractEntity(content, EntityType.Time));
        labels.AddRange(ExtractEntity(content, EntityType.Money));

        return Task.FromResult(labels.ToArray());
    }

    /// <summary>
    /// Process the content and extract the specified entity type.
    /// </summary>
    /// <param name="content"></param>
    /// <param name="entityType"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private IEnumerable<ContentLabelModel> ExtractEntity(ContentModel content, EntityType entityType)
    {
        var text = new StringBuilder();
        text.AppendLine(content.Summary);
        if (!String.IsNullOrWhiteSpace(content.Transcription)) text.AppendLine(content.Transcription);
        return _extractor.ExtractEntities(text.ToString(), entityType).Distinct().Select(l => new ContentLabelModel()
        {
            ContentId = content.Id,
            Key = Enum.GetName(entityType) ?? throw new InvalidOperationException("Entity type does not exist"),
            Value = l
        });
    }

    /// <summary>
    /// Send a request to Kafka to update the indexes in Elasticsearch.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task SendIndexingRequest(ContentModel content)
    {
        if (!String.IsNullOrWhiteSpace(_options.IndexingTopic))
        {
            var result = await this.Producer.SendMessageAsync(_options.IndexingTopic, new IndexRequest(content, IndexAction.Index));
            if (result == null) throw new InvalidOperationException($"Failed to receive result from Kafka when submitting an indexing request.  ContentId: {content.Id}");

            if (content.Status == ContentStatus.Published)
            {
                result = await this.Producer.SendMessageAsync(_options.IndexingTopic, new IndexRequest(content, IndexAction.Publish));
                if (result == null) throw new InvalidOperationException($"Failed to receive result from Kafka when submitting an indexing request.  ContentId: {content.Id}");
            }
        }
    }
    #endregion
}
