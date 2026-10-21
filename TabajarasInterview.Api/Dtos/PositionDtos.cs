using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Dtos;

/// <summary>JSON uses snake_case ("on_hold"); the database stores the PascalCase value ("OnHold").</summary>
public enum PositionStatus
{
    Draft,
    Open,
    OnHold,
    Closed,
    Cancelled,
}

/// <summary>JSON and database both use lowercase ("remote", "hybrid", "onsite").</summary>
public enum WorkModel
{
    Remote,
    Hybrid,
    Onsite,
}

public static class PositionEnumExtensions
{
    public static string ToDbValue(this PositionStatus status) => status switch
    {
        PositionStatus.Draft => "Draft",
        PositionStatus.Open => "Open",
        PositionStatus.OnHold => "OnHold",
        PositionStatus.Closed => "Closed",
        PositionStatus.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static string ToDbValue(this WorkModel workModel) => workModel switch
    {
        WorkModel.Remote => "remote",
        WorkModel.Hybrid => "hybrid",
        WorkModel.Onsite => "onsite",
        _ => throw new ArgumentOutOfRangeException(nameof(workModel)),
    };
}

/// <summary>Rust <c>PositionResponse</c>.</summary>
public sealed record PositionResponse(
    int Id,
    string Title,
    string? Description,
    PositionStatus Status,
    WorkModel WorkModel,
    int CreatedBy,
    DateTime? UpdatedAt,
    DateTime CreatedAt,
    List<StackResponse> Stacks,
    List<LocationResponse> Locations)
{
    /// <summary>Rust <c>to_response</c>: stacks and locations start empty. Unknown stored values give 500.</summary>
    public static PositionResponse From(Position position)
    {
        var status = position.Status switch
        {
            "Draft" => PositionStatus.Draft,
            "Open" => PositionStatus.Open,
            "OnHold" => PositionStatus.OnHold,
            "Closed" => PositionStatus.Closed,
            "Cancelled" => PositionStatus.Cancelled,
            _ => throw new ApiException(StatusCodes.Status500InternalServerError, "Unknown position status"),
        };

        var workModel = position.WorkModel switch
        {
            "remote" => WorkModel.Remote,
            "hybrid" => WorkModel.Hybrid,
            "onsite" => WorkModel.Onsite,
            _ => throw new ApiException(StatusCodes.Status500InternalServerError, "Unknown work model"),
        };

        return new PositionResponse(
            position.Id,
            position.Title,
            position.Description,
            status,
            workModel,
            position.CreatedBy,
            position.UpdatedAt,
            position.CreatedAt,
            [],
            []);
    }
}

public sealed record CreatePositionRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required PositionStatus Status { get; init; }
    public required WorkModel WorkModel { get; init; }
}

public sealed record UpdatePositionRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public PositionStatus? Status { get; init; }
    public WorkModel? WorkModel { get; init; }
}
