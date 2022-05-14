using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TNO.API.Areas.Services.Models.DataSource;
using TNO.Services.Config;

namespace TNO.Services;

/// <summary>
/// ServiceManager class, provides a way to manage several data source schedules.
/// It will fetch all data sources for the configured media types.
/// It will ensure all data sources are being run based on their schedules.
/// </summary>
public abstract class ServiceManager<TDataSourceManager, TOption> : IServiceManager
    where TDataSourceManager : IDataSourceManager
    where TOption : IngestServiceOptions
{
    #region Variables
    /// <summary>
    /// Api service controller.
    /// </summary>
    protected readonly IApiService _api;

    /// <summary>
    /// Configuration options for this service.
    /// </summary>
    protected readonly TOption _options;

    /// <summary>
    /// Logger for this service.
    /// </summary>
    protected readonly ILogger _logger;
    private readonly DataSourceManagerFactory<TDataSourceManager, TOption> _factory;
    private readonly Dictionary<int, TDataSourceManager> _dataSources = new();
    #endregion

    #region Properties
    /// <summary>
    /// get - The state of the service.
    /// </summary>
    public ServiceState State { get; private set; }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new instance of a ServiceManager object, initializes with specified parameters.
    /// </summary>
    /// <param name="api">Service to communicate with the api.</param>
    /// <param name="factory">Data source manager factory.</param>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Logging client.</param>
    public ServiceManager(
        IApiService api,
        DataSourceManagerFactory<TDataSourceManager, TOption> factory,
        IOptions<TOption> options,
        ILogger<ServiceManager<TDataSourceManager, TOption>> logger)
    {
        _api = api;
        _factory = factory;
        _options = options.Value;
        _logger = logger;
        this.State = new ServiceState(_options);
    }
    #endregion

    #region Methods
    /// <summary>
    /// Run the service manager.
    /// </summary>
    /// <returns></returns>
    public async Task RunAsync()
    {
        var dataSources = await GetDataSourcesAsync();

        // Run at the shortest interval of all schedules.
        var delay = dataSources.Min(ds => ds.DataSourceSchedules.Where(s => s.Schedule?.DelayMS != 0).Min(s => s.Schedule?.DelayMS)) ?? _options.DefaultDelayMS;

        while (this.State.Status != ServiceStatus.Stopped)
        {
            if (this.State.Status != ServiceStatus.Running)
            {
                _logger.LogDebug("The service is not running '{Status}'", this.State.Status);
            }
            else if (!dataSources.Any(ds => ds.IsEnabled))
            {
                // If there are no data sources, then we need to keep the service alive.
                _logger.LogWarning("There are no configured data sources");
            }
            else
            {
                foreach (var dataSource in dataSources)
                {
                    // Update the delay if a schedule has changed and is less than the original value.
                    var delayMS = dataSource.DataSourceSchedules.Where(s => s.Schedule?.DelayMS > 0).Min(s => s.Schedule?.DelayMS) ?? delay;
                    delay = delayMS < delay ? delayMS : delay;

                    // If the service isn't running, don't make additional requests.
                    if (this.State.Status != ServiceStatus.Running) continue;

                    // Maintain a dictionary of managers for each data source.
                    // Fire event for the data source scheduler.
                    var hasKey = _dataSources.ContainsKey(dataSource.Id);
                    if (!hasKey) _dataSources.Add(dataSource.Id, _factory.Create(dataSource));
                    var manager = _dataSources[dataSource.Id];

                    try
                    {
                        if (dataSource.FailedAttempts >= dataSource.RetryLimit)
                        {
                            _logger.LogWarning("Data source '{Code}' has reached maximum failure limit", dataSource.Code);
                            continue;
                        }

                        // TODO: This needs to run asynchronously so that many data sources are being acted upon at one time.
                        // TODO: Need to propogate service status change to running threads.
                        await manager.RunAsync();

                        // Successful run clears any errors.
                        this.State.ResetFailures();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process data source '{Code}'", dataSource.Code);

                        // Update data source with failure.
                        await manager.RecordFailureAsync();
                        this.State.RecordFailure();
                    }
                }
            }

            // The delay ensures we don't have a run away thread.
            // With a minimum delay for all data source schedules, it could mean some data sources are pinged more often then required.
            _logger.LogDebug("Service sleeping for {delay} ms", delay);
            // await Thread.Sleep(new TimeSpan(0, 0, 0, delay));
            await Task.Delay(delay);

            // Fetch all data sources again to determine if there are any changes to the list.
            dataSources = await GetDataSourcesAsync();
        }

        _logger.LogInformation("Service stopping");
    }

    /// <summary>
    /// Make an AJAX request to the api to fetch data sources for the configured media types.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<DataSourceModel>> GetDataSourcesAsync()
    {
        var dataSources = new List<DataSourceModel>();
        foreach (var mediaType in _options.MediaTypes)
        {
            try
            {
                // If the service isn't running, don't make additional requests.
                if (this.State.Status != ServiceStatus.Running) continue;

                dataSources.AddRange(await _api.GetDataSourcesAsync(mediaType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch data sources for media type", mediaType);
                this.State.RecordFailure();
            }
        }

        return dataSources;
    }
    #endregion
}
