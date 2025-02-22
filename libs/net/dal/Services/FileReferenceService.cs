using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TNO.DAL.Config;
using TNO.DAL.Extensions;
using TNO.DAL.Models;
using TNO.Entities;

namespace TNO.DAL.Services;

public class FileReferenceService : BaseService<FileReference, long>, IFileReferenceService
{
    #region Properties
    private readonly StorageOptions _options;
    #endregion

    #region Constructors
    public FileReferenceService(
        TNOContext dbContext,
        ClaimsPrincipal principal,
        IServiceProvider serviceProvider,
        StorageOptions options,
        ILogger<FileReferenceService> logger) : base(dbContext, principal, serviceProvider, logger)
    {
        _options = options;
    }
    #endregion

    #region Methods
    /// <summary>
    /// Find all file references for the specified 'contentId'.
    /// </summary>
    /// <param name="contentId"></param>
    /// <returns></returns>
    public IEnumerable<FileReference> FindByContentId(long contentId)
    {
        return this.Context.FileReferences
            .Include(fr => fr.Content)
            .Where(fr => fr.ContentId == contentId).ToArray();
    }

    /// <summary>
    /// Open the file for reading and return the stream.
    /// Note - this does not close the stream.  You need to do this.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public FileStream Download(FileReference entity)
    {
        // TODO: Handle different data locations.
        var path = entity.GetFilePath(this.Context, _options);
        return File.OpenRead(path);
    }

    /// <summary>
    /// Upload the file to the configured data location and add or update the specified file reference.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<FileReference> Upload(ContentFileReference model)
    {
        // TODO: Handle different data locations.
        var path = model.GetFilePath(this.Context, _options);
        var directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory) && !String.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        if (model.File?.Length > 0)
        {
            using var stream = File.Open(path, FileMode.Create);
            await model.File.CopyToAsync(stream);
            model.IsUploaded = true;
        }

        var entity = (FileReference)model;
        if (model.Id == 0)
            this.Context.Add(entity);
        else
            this.Context.Update(entity);

        this.Context.CommitTransaction();
        return entity;
    }

    /// <summary>
    /// Copy the file to the configured data location and add or update the specified file reference.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public FileReference Attach(ContentFileReference model)
    {
        // TODO: Handle different data locations.
        var path = model.GetFilePath(this.Context, _options);
        var directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory) && !String.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var file = new FileInfo(model.SourceFile);
        file.CopyTo(path, true);

        var entity = (FileReference)model;
        if (model.Id == 0)
            this.Context.Add(entity);
        else
            this.Context.Update(entity);

        this.Context.CommitTransaction();
        return entity;
    }

    /// <summary>
    /// Delete the specified file reference and the file from the configured data location.
    /// </summary>
    /// <param name="entity"></param>
    public override void Delete(FileReference entity)
    {
        // TODO: Handle different data locations.
        var path = entity.GetFilePath(this.Context, _options);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        base.Delete(entity);
    }
    #endregion
}
