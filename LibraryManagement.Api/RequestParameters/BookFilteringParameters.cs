using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.RequestParameters;

public record BookFilteringParameters(
     [property: FromQuery(Name = "title")] string? Title,
     [property: FromQuery(Name = "categoryId")] int? CategoryId,
     [property: FromQuery(Name = "authorId")] int? AuthorId);
