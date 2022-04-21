using TNO.Entities;

namespace TNO.API.Areas.Editor.Models.Content;

/// <summary>
/// DataSourceModel class, provides a model that represents an category.
/// </summary>
public class DataSourceModel
{
    #region Properties
    /// <summary>
    /// get/set -
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// get/set -
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// get/set -
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// get/set -
    /// </summary>
    public string ShortName { get; set; } = "";

    /// <summary>
    /// get/set -
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// get/set -
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// get/set -
    /// </summary>
    public int DataLocationId { get; set; }

    /// <summary>
    /// get/set -
    /// </summary>
    public int MediaTypeId { get; set; }

    /// <summary>
    /// get/set -
    /// </summary>
    public int LicenseId { get; set; }

    /// <summary>
    /// get/set -
    /// </summary>
    public DataSourceScheduleType ScheduleType { get; set; }

    /// <summary>
    /// get/set -
    /// </summary>
    public string Topic { get; set; } = "";

    /// <summary>
    /// get/set -
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// get/set -
    /// </summary>
    public string Connection { get; set; } = "";

    /// <summary>
    /// get/set -
    /// </summary>
    public DateTime? LastRanOn { get; set; }

    /// <summary>
    /// get/set -
    /// </summary>
    public int RetryLimit { get; set; }

    /// <summary>
    /// get/set -
    /// </summary>
    public int FailedAttempts { get; set; }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new instance of an DataSourceModel.
    /// </summary>
    public DataSourceModel() { }

    /// <summary>
    /// Creates a new instance of an DataSourceModel, initializes with specified parameter.
    /// </summary>
    /// <param name="entity"></param>
    public DataSourceModel(Entities.DataSource entity)
    {
        this.Id = entity.Id;
        this.Code = entity.Code;
        this.Name = entity.Name;
        this.ShortName = entity.ShortName;
        this.Description = entity.Description;
        this.IsEnabled = entity.IsEnabled;
        this.DataLocationId = entity.DataLocationId;
        this.MediaTypeId = entity.MediaTypeId;
        this.LicenseId = entity.LicenseId;
        this.ScheduleType = entity.ScheduleType;
        this.Topic = entity.Topic;
        this.ParentId = entity.ParentId;
        this.Connection = entity.Connection;
        this.LastRanOn = entity.LastRanOn;
        this.RetryLimit = entity.RetryLimit;
        this.FailedAttempts = entity.FailedAttempts;
    }
    #endregion

    #region Methods
    /// <summary>
    /// Explicit cast to entity.
    /// </summary>
    /// <param name="model"></param>
    public static explicit operator Entities.DataSource(DataSourceModel model)
    {
        return new Entities.DataSource(model.Name, model.Code, model.DataLocationId, model.MediaTypeId, model.LicenseId, model.ScheduleType, model.Topic)
        {
            Id = model.Id,
            ShortName = model.ShortName,
            Description = model.Description,
            IsEnabled = model.IsEnabled,
            ParentId = model.ParentId,
            Connection = model.Connection,
            LastRanOn = model.LastRanOn,
            RetryLimit = model.RetryLimit,
            FailedAttempts = model.FailedAttempts,
        };
    }
    #endregion
}
