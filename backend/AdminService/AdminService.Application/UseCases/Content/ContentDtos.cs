namespace AdminService.Application.UseCases.Content;

public record Banner(
    string Id,
    string Title,
    string Image,
    string Link,
    string Placement,
    string Status,
    string StartDate,
    string EndDate,
    int Views,
    int Clicks
);

public record StaticPage(
    string Id,
    string Title,
    string Slug,
    string Status,
    string LastModified,
    string Author,
    int Views
);

public record BlogPost(
    string Id,
    string Title,
    string Slug,
    string Status,
    string Author,
    string? PublishedAt,
    int Views,
    string Category
);

public record ContentOverviewResponse(
    List<Banner> Banners,
    List<StaticPage> Pages,
    List<BlogPost> BlogPosts,
    string TotalViews
);
