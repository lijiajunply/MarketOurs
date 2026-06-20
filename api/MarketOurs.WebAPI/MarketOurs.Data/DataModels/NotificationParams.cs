using System.Text.Json.Serialization;

namespace MarketOurs.Data.DataModels;

[JsonDerivedType(typeof(CommentReplyParams), typeDiscriminator: "commentReply")]
[JsonDerivedType(typeof(PostReplyParams), typeDiscriminator: "postReply")]
[JsonDerivedType(typeof(HotListParams), typeDiscriminator: "hotList")]
[JsonDerivedType(typeof(ReviewParams), typeDiscriminator: "review")]
[JsonDerivedType(typeof(SystemParams), typeDiscriminator: "system")]
public abstract record NotificationParams;

public record CommentReplyParams(string CommenterName, string BodySnippet) : NotificationParams;

public record PostReplyParams(string CommenterName, string BodySnippet) : NotificationParams;

public record HotListPost(string Id, string Title);

public record HotListParams(string Header, List<HotListPost> Posts) : NotificationParams;

public record ReviewParams(string EntityType, string Name, bool Approved, string? Reason) : NotificationParams;

public record SystemParams(string Message) : NotificationParams;
