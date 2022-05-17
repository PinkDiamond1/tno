using TNO.API.Areas.Editor.Models.Lookup;
using TNO.API.Areas.Services.Models.Content;
using TNO.API.Areas.Services.Models.ContentReference;
using TNO.API.Areas.Services.Models.DataSource;

namespace TNO.Services;

/// <summary>
/// IApiService interface, provides a way to interace with the API.
/// </summary>
public interface IApiService
{
    /// <summary>
    /// Make an AJAX request to the api to fetch data sources for the specified media type.
    /// </summary>
    /// <param name="mediaType"></param>
    /// <returns></returns>
    public Task<IEnumerable<DataSourceModel>> GetDataSourcesAsync(string mediaType);

    /// <summary>
    /// Make an AJAX request to the api to fetch the data source for the specified 'code'.
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Task<DataSourceModel?> GetDataSourceAsync(string code);

    /// <summary>
    /// Make an AJAX request to the api to update the data source.
    /// </summary>
    /// <param name="dataSource"></param>
    /// <returns></returns>
    public Task<DataSourceModel?> UpdateDataSourceAsync(DataSourceModel dataSource);

    /// <summary>
    /// Make an AJAX request to the api to find the content reference for the specified key.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="uid"></param>
    /// <returns></returns>
    public Task<ContentReferenceModel?> FindContentReferenceAsync(string source, string uid);

    /// <summary>
    /// Make an AJAX request to the api to add the specified content reference.
    /// </summary>
    /// <param name="contentReference"></param>
    /// <returns></returns>
    public Task<ContentReferenceModel?> AddContentReferenceAsync(ContentReferenceModel contentReference);

    /// <summary>
    /// Make an AJAX request to the api to update the specified content reference.
    /// </summary>
    /// <param name="contentReference"></param>
    /// <returns></returns>
    public Task<ContentReferenceModel?> UpdateContentReferenceAsync(ContentReferenceModel contentReference);

    /// <summary>
    /// Make an AJAX request to the api to add the specified content.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public Task<ContentModel?> AddContentAsync(ContentModel content);

    /// <summary>
    /// Make an AJAX request to the api to get the lookups.
    /// </summary>
    /// <returns></returns>
    public Task<LookupModel?> GetLookupsAsync();
}
