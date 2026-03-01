using MediatR;

namespace AdminService.Application.UseCases.Content;

public record GetContentOverviewQuery : IRequest<ContentOverviewResponse>;
public record GetBannersQuery : IRequest<List<Banner>>;
public record GetStaticPagesQuery : IRequest<List<StaticPage>>;
public record GetBlogPostsQuery : IRequest<List<BlogPost>>;
public record DeleteBannerCommand(string BannerId) : IRequest;
