using MediatR;
using Microsoft.Extensions.Logging;

namespace AdminService.Application.UseCases.Content;

public class GetContentOverviewQueryHandler : IRequestHandler<GetContentOverviewQuery, ContentOverviewResponse>
{
    private readonly ILogger<GetContentOverviewQueryHandler> _logger;

    public GetContentOverviewQueryHandler(ILogger<GetContentOverviewQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<ContentOverviewResponse> Handle(GetContentOverviewQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting content overview");

        var banners = GetSampleBanners();
        var pages = GetSamplePages();
        var posts = GetSampleBlogPosts();

        var response = new ContentOverviewResponse(banners, pages, posts, "24,580");
        return Task.FromResult(response);
    }

    private static List<Banner> GetSampleBanners() => new()
    {
        new("b1", "Banner Principal - Compra tu Carro", "/images/banner1.jpg", "/vehiculos", "homepage-hero", "active",
            DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"), DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"), 12500, 340),
        new("b2", "Promoción Dealers - Plan Premium", "/images/banner2.jpg", "/dealers/planes", "homepage-secondary", "active",
            DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd"), DateTime.UtcNow.AddDays(50).ToString("yyyy-MM-dd"), 8200, 195),
        new("b3", "Vende tu Carro Rápido", "/images/banner3.jpg", "/vender", "listing-page", "scheduled",
            DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd"), DateTime.UtcNow.AddDays(60).ToString("yyyy-MM-dd"), 0, 0),
    };

    private static List<StaticPage> GetSamplePages() => new()
    {
        new("p1", "Términos y Condiciones", "terminos-condiciones", "published",
            DateTime.UtcNow.AddDays(-60).ToString("yyyy-MM-dd"), "Admin", 3200),
        new("p2", "Política de Privacidad", "politica-privacidad", "published",
            DateTime.UtcNow.AddDays(-60).ToString("yyyy-MM-dd"), "Admin", 1800),
        new("p3", "Guía para Vendedores", "guia-vendedores", "published",
            DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"), "Admin", 5600),
        new("p4", "Preguntas Frecuentes", "faq", "published",
            DateTime.UtcNow.AddDays(-15).ToString("yyyy-MM-dd"), "Admin", 9800),
    };

    private static List<BlogPost> GetSampleBlogPosts() => new()
    {
        new("bp1", "5 consejos para vender tu carro más rápido", "5-consejos-vender-carro",
            "published", "OKLA Team", DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd"), 4200, "Consejos"),
        new("bp2", "Los carros más buscados en República Dominicana 2025", "carros-mas-buscados-2025",
            "published", "OKLA Team", DateTime.UtcNow.AddDays(-12).ToString("yyyy-MM-dd"), 8900, "Tendencias"),
        new("bp3", "Cómo evitar estafas al comprar un vehículo", "evitar-estafas-vehiculo",
            "draft", "OKLA Team", null, 0, "Seguridad"),
    };
}

public class GetBannersQueryHandler : IRequestHandler<GetBannersQuery, List<Banner>>
{
    public Task<List<Banner>> Handle(GetBannersQuery request, CancellationToken cancellationToken)
    {
        return new GetContentOverviewQueryHandler(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GetContentOverviewQueryHandler>.Instance
        ).Handle(new GetContentOverviewQuery(), cancellationToken)
        .ContinueWith(t => t.Result.Banners, cancellationToken);
    }
}

public class GetStaticPagesQueryHandler : IRequestHandler<GetStaticPagesQuery, List<StaticPage>>
{
    public Task<List<StaticPage>> Handle(GetStaticPagesQuery request, CancellationToken cancellationToken)
    {
        return new GetContentOverviewQueryHandler(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GetContentOverviewQueryHandler>.Instance
        ).Handle(new GetContentOverviewQuery(), cancellationToken)
        .ContinueWith(t => t.Result.Pages, cancellationToken);
    }
}

public class GetBlogPostsQueryHandler : IRequestHandler<GetBlogPostsQuery, List<BlogPost>>
{
    public Task<List<BlogPost>> Handle(GetBlogPostsQuery request, CancellationToken cancellationToken)
    {
        return new GetContentOverviewQueryHandler(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GetContentOverviewQueryHandler>.Instance
        ).Handle(new GetContentOverviewQuery(), cancellationToken)
        .ContinueWith(t => t.Result.BlogPosts, cancellationToken);
    }
}

public class DeleteBannerCommandHandler : IRequestHandler<DeleteBannerCommand>
{
    private readonly ILogger<DeleteBannerCommandHandler> _logger;

    public DeleteBannerCommandHandler(ILogger<DeleteBannerCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DeleteBannerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting banner {BannerId}", request.BannerId);
        // No-op for now: would persist to DB in future
        return Task.CompletedTask;
    }
}
