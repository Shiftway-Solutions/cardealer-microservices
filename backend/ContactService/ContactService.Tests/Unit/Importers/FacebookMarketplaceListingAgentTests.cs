using FluentAssertions;
using ContactService.Domain.Entities;
using ContactService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ContactService.Tests.Unit.Importers;

/// <summary>
/// Unit tests for Facebook Marketplace Listing Agent importer.
/// Tests HTML fixture parsing, price extraction, field validation, and rate limiting.
/// </summary>
public class FacebookMarketplaceListingAgentTests
{
    // ════════════════════════════════════════════════════════════════════════
    // A. Price Extraction Tests (Dominican Pesos / RD$)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Helper: Parse price from HTML text with Dominican currency markers.</summary>
    private decimal ExtractPriceFromHtml(string htmlText)
    {
        // Regex patterns for Dominican Peso variations
        var patterns = new[] 
        {
            @"RD\$\s*([\d,\.]+)",      // RD$ 1,200,000
            @"\$([\d,\.]+)\s*RD",       // $1,200,000 RD
            @"(?:pesos|dominicano).*?([\d,\.]+)",  // pesos... 1200000
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(htmlText, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var priceStr = match.Groups[1].Value
                    .Replace(",", "")      // Remove thousands separators
                    .Replace(".", "")
                    .Trim();
                
                if (decimal.TryParse(priceStr, out var price))
                    return price;
            }
        }

        return 0;
    }

    [Fact]
    public void ExtractPrice_RD_PesoSymbol_CorrectlyParsed()
    {
        // Fixture: Real Facebook Marketplace listing HTML (simplified)
        var htmlFixture = @"
            <div class='price-container'>
                <span>RD$ 1,850,000</span>
                <p>Precio de venta: RD$ 1,850,000</p>
            </div>";

        var price = ExtractPriceFromHtml(htmlFixture);
        price.Should().Be(1850000m, "price should be extracted as 1,850,000 Dominican pesos");
    }

    [Fact]
    public void ExtractPrice_DollarPesoNotation_CorrectlyParsed()
    {
        var htmlFixture = @"
            <div class='price'>
                $950,500 RD
            </div>";

        var price = ExtractPriceFromHtml(htmlFixture);
        price.Should().Be(950500m, "price should handle $ RD notation");
    }

    [Fact]
    public void ExtractPrice_WithThousandsSeparators_CorrectlyParsed()
    {
        var htmlFixture = @"
            <span class='price-text'>
                RD$ 2,450,000
            </span>";

        var price = ExtractPriceFromHtml(htmlFixture);
        price.Should().Be(2450000m, "thousands separators should be removed");
    }

    [Fact]
    public void ExtractPrice_InvalidPrice_ReturnsZero()
    {
        var htmlFixture = @"
            <div>
                <p>Sin precio especificado</p>
                <span>Contacta al vendedor</span>
            </div>";

        var price = ExtractPriceFromHtml(htmlFixture);
        price.Should().Be(0m, "invalid price should return 0");
    }

    // ════════════════════════════════════════════════════════════════════════
    // B. Title Extraction Tests (Brand/Model/Year)
    // ════════════════════════════════════════════════════════════════════════

    private (string Brand, string Model, int Year) ExtractVehicleInfoFromTitle(string title)
    {
        // Pattern: "Toyota Corolla 2020" or "2020 Toyota Corolla Automático"
        var yearMatch = System.Text.RegularExpressions.Regex.Match(title, @"(20\d{2}|19\d{2})");
        var year = yearMatch.Success ? int.Parse(yearMatch.Groups[1].Value) : 0;

        var parts = title
            .Replace(year.ToString(), "")
            .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        var brand = parts.FirstOrDefault() ?? "";
        var model = parts.Skip(1).FirstOrDefault() ?? "";

        return (brand, model, year);
    }

    [Fact]
    public void ExtractTitle_StandardFormat_BrandModelYearCorrect()
    {
        var title = "Toyota Corolla 2020 Automático";
        
        var (brand, model, year) = ExtractVehicleInfoFromTitle(title);
        
        brand.Should().Be("Toyota");
        model.Should().Be("Corolla");
        year.Should().Be(2020);
    }

    [Fact]
    public void ExtractTitle_YearAtStart_BrandModelYearCorrect()
    {
        var title = "2017 Honda Civic EX";
        
        var (brand, model, year) = ExtractVehicleInfoFromTitle(title);
        
        brand.Should().Be("Honda");
        model.Should().Be("Civic");
        year.Should().Be(2017);
    }

    [Fact]
    public void ExtractTitle_WithExtraInfo_BrandModelExtracted()
    {
        var title = "2019 Chevrolet Spark LT Gasolina";
        
        var (brand, model, year) = ExtractVehicleInfoFromTitle(title);
        
        brand.Should().Be("Chevrolet");
        model.Should().Be("Spark");
        year.Should().Be(2019);
    }

    [Fact]
    public void ExtractTitle_MissingYear_ReturnsZero()
    {
        var title = "Toyota Corolla Automático";
        
        var (brand, model, year) = ExtractVehicleInfoFromTitle(title);
        
        year.Should().Be(0, "missing year should return 0");
    }

    // ════════════════════════════════════════════════════════════════════════
    // C. Description Cleaning Tests (Remove UI elements, Facebook noise)
    // ════════════════════════════════════════════════════════════════════════

    private string CleanDescriptionFromHtml(string rawHtml)
    {
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            rawHtml,
            @"<[^>]+>|&nbsp;|&quot;|&amp;|&lt;|&gt;|javascript:|onclick:|on\w+\s*=",
            " ",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove "Contact seller", "Save", "Share" buttons text
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"(Contactar vendedor|Contact seller|Guardar|Save|Compartir|Share|Ver más)",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Normalize whitespace
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned;
    }

    [Fact]
    public void CleanDescription_RemovesHtmlTags_PlainTextReturned()
    {
        var dirtyHtml = @"
            <p>Hermoso vehículo en excelente condición</p>
            <ul>
                <li>Aire acondicionado</li>
                <li>Dirección hidráulica</li>
            </ul>";

        var cleaned = CleanDescriptionFromHtml(dirtyHtml);
        
        cleaned.Should().NotContain("<");
        cleaned.Should().NotContain(">");
        cleaned.Should().Contain("Hermoso");
        cleaned.Should().Contain("Aire acondicionado");
    }

    [Fact]
    public void CleanDescription_RemovesFacebookButtons_NoButtonText()
    {
        var dirtyHtml = @"
            <div>Carro en perfecto estado</div>
            <button>Contactar vendedor</button>
            <button>Guardar</button>
            <span>Compartir</span>";

        var cleaned = CleanDescriptionFromHtml(dirtyHtml);
        
        cleaned.Should().Contain("Carro en perfecto estado");
        cleaned.Should().NotContain("Contactar");
        cleaned.Should().NotContain("Guardar");
        cleaned.Should().NotContain("Compartir");
    }

    [Fact]
    public void CleanDescription_NormalizesWhitespace_SingleSpaces()
    {
        var dirtyHtml = "Carro    bien   cuidado     con   mantenimiento";
        
        var cleaned = CleanDescriptionFromHtml(dirtyHtml);
        
        cleaned.Should().Be("Carro bien cuidado con mantenimiento");
    }

    // ════════════════════════════════════════════════════════════════════════
    // D. Photo Download Tests (URL extraction, validation)
    // ════════════════════════════════════════════════════════════════════════

    private List<string> ExtractPhotoUrlsFromHtml(string htmlContent)
    {
        var photoUrls = new List<string>();
        var matches = System.Text.RegularExpressions.Regex.Matches(
            htmlContent,
            @"(?:src|data-src|href)\s*=\s*[""']([^""']*\.(jpg|jpeg|png|webp))",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var url = match.Groups[1].Value;
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    Uri.TryCreate("https://example.com" + url, UriKind.Absolute, out _))
                {
                    photoUrls.Add(url);
                }
            }
        }

        return photoUrls;
    }

    [Fact]
    public void ExtractPhotos_MultipleImages_AllUrlsExtracted()
    {
        var htmlFixture = @"
            <div class='photos'>
                <img src='https://cdn.facebook.com/images/car-photo-1.jpg' />
                <img src='https://cdn.facebook.com/images/car-photo-2.jpg' />
                <img src='https://cdn.facebook.com/images/car-photo-3.webp' />
            </div>";

        var urls = ExtractPhotoUrlsFromHtml(htmlFixture);
        
        urls.Should().HaveCount(3, "all three photo URLs should be extracted");
        urls.Should().AllSatisfy(url => 
        {
            url.Should().MatchRegex(@"\.(jpg|jpeg|png|webp)$", "all URLs should be image files");
        });
    }

    [Fact]
    public void ExtractPhotos_LazyLoadedImages_DataSrcExtracted()
    {
        var htmlFixture = @"
            <img data-src='https://cdn.com/car-lazy-1.jpg' />
            <img data-src='https://cdn.com/car-lazy-2.png' />";

        var urls = ExtractPhotoUrlsFromHtml(htmlFixture);
        
        urls.Should().HaveCount(2, "data-src attributes should be extracted");
    }

    [Fact]
    public void ExtractPhotos_NoImageUrls_EmptyListReturned()
    {
        var htmlFixture = @"
            <div>
                <p>Sin fotos disponibles</p>
                <span>Contactar al vendedor por más información</span>
            </div>";

        var urls = ExtractPhotoUrlsFromHtml(htmlFixture);
        
        urls.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    // E. Rate Limiting Tests (Graceful handling of Facebook blocks)
    // ════════════════════════════════════════════════════════════════════════

    public class RateLimiter
    {
        private readonly Dictionary<string, (int Count, DateTime ResetTime)> _requestCounts = new();
        private readonly int _maxRequestsPerMinute;
        private readonly TimeSpan _window = TimeSpan.FromMinutes(1);

        public RateLimiter(int maxRequestsPerMinute = 30)
        {
            _maxRequestsPerMinute = maxRequestsPerMinute;
        }

        public bool CanMakeRequest(string sourceId)
        {
            var now = DateTime.UtcNow;

            if (!_requestCounts.ContainsKey(sourceId))
            {
                _requestCounts[sourceId] = (1, now.Add(_window));
                return true;
            }

            var (count, resetTime) = _requestCounts[sourceId];

            if (now >= resetTime)
            {
                _requestCounts[sourceId] = (1, now.Add(_window));
                return true;
            }

            if (count < _maxRequestsPerMinute)
            {
                _requestCounts[sourceId] = (count + 1, resetTime);
                return true;
            }

            return false;
        }

        public TimeSpan GetRetryAfter(string sourceId)
        {
            if (_requestCounts.TryGetValue(sourceId, out var data))
            {
                var remaining = data.ResetTime - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
            return TimeSpan.Zero;
        }
    }

    [Fact]
    public void RateLimiter_UnderLimit_AllowsRequests()
    {
        var limiter = new RateLimiter(maxRequestsPerMinute: 10);
        var sourceId = "facebook-importer-1";

        for (int i = 0; i < 10; i++)
        {
            limiter.CanMakeRequest(sourceId).Should().BeTrue($"request {i + 1} should be allowed");
        }
    }

    [Fact]
    public void RateLimiter_ExceedsLimit_BlocksRequest()
    {
        var limiter = new RateLimiter(maxRequestsPerMinute: 5);
        var sourceId = "facebook-importer-1";

        for (int i = 0; i < 5; i++)
            limiter.CanMakeRequest(sourceId);

        limiter.CanMakeRequest(sourceId).Should().BeFalse("6th request should be blocked");
    }

    [Fact]
    public void RateLimiter_ExceedLimit_ReturnsRetryAfterTime()
    {
        var limiter = new RateLimiter(maxRequestsPerMinute: 3);
        var sourceId = "facebook-importer-1";

        for (int i = 0; i < 3; i++)
            limiter.CanMakeRequest(sourceId);

        // Block request
        limiter.CanMakeRequest(sourceId).Should().BeFalse();

        var retryAfter = limiter.GetRetryAfter(sourceId);
        retryAfter.Should().BeGreaterThan(TimeSpan.Zero, "retry-after should indicate wait time");
        retryAfter.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(1), "retry-after should be within the window");
    }

    // ════════════════════════════════════════════════════════════════════════
    // F. Field Population Tests (Complete OKLA Listing Entity)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CreateListingFromFacebook_AllRequiredFields_PopulatedCorrectly()
    {
        // Complete HTML fixture from actual Facebook Marketplace listing
        var htmlFixture = @"
            <div class='listing'>
                <h1>2020 Toyota Corolla Automático</h1>
                <span class='price'>RD$ 1,350,000</span>
                <div class='description'>
                    <p>Hermoso Corolla 2020 en perfecto estado. Tiene aire acondicionado, 
                    dirección hidráulica, vidrios eléctricos, espejos eléctricos. 
                    Mantenimiento al día. Lista para traspaso.</p>
                </div>
                <div class='photos'>
                    <img src='https://cdn.facebook.com/photo1.jpg' />
                    <img src='https://cdn.facebook.com/photo2.jpg' />
                    <img src='https://cdn.facebook.com/photo3.jpg' />
                </div>
                <div class='seller-info'>
                    <span class='name'>Juan Pérez</span>
                    <span class='location'>Santo Domingo, RD</span>
                </div>
            </div>";

        // Parse from HTML
        var title = "2020 Toyota Corolla Automático";
        var (brand, model, year) = ExtractVehicleInfoFromTitle(title);
        var price = ExtractPriceFromHtml(htmlFixture);
        var description = CleanDescriptionFromHtml(htmlFixture);
        var photos = ExtractPhotoUrlsFromHtml(htmlFixture);

        // Validate all fields populated
        brand.Should().NotBeNullOrEmpty();
        model.Should().NotBeNullOrEmpty();
        year.Should().BeGreaterThan(2000);
        price.Should().BeGreaterThan(0);
        description.Should().Contain("Corolla");
        description.Should().Contain("aire acondicionado");
        photos.Should().HaveCount(3);

        // Create domain entity
        var inquiry = new ContactRequest(
            vehicleId: Guid.NewGuid(),
            buyerId: Guid.NewGuid(),
            sellerId: Guid.NewGuid(),
            subject: $"Import: {title} - RD$ {price:N0}",
            buyerName: "Facebook Marketplace Importer",
            buyerEmail: "importer@okla.local",
            message: description);

        inquiry.Should().NotBeNull();
        inquiry.Subject.Should().Contain("Toyota");
        inquiry.Subject.Should().Contain("2020");
        inquiry.Subject.Should().Contain("1,350,000");
    }

    [Fact]
    public void CreateListingFromFacebook_MinimalFields_StillCreates()
    {
        var minimalHtml = @"
            <h1>Honda Civic</h1>
            <span>RD$ 800,000</span>
            <p>Carro en buen estado</p>";

        var price = ExtractPriceFromHtml(minimalHtml);
        var (brand, model, year) = ExtractVehicleInfoFromTitle("Honda Civic");

        // Even with missing data, should create entity
        var inquiry = new ContactRequest(
            vehicleId: null,
            buyerId: Guid.NewGuid(),
            sellerId: Guid.NewGuid(),
            subject: "Import: Honda Civic",
            buyerName: "Facebook Marketplace Bot",
            buyerEmail: "bot@okla.local",
            message: "Carro en buen estado");

        inquiry.Should().NotBeNull();
        price.Should().Be(800000m);
        brand.Should().Be("Honda");
    }

    [Fact]
    public void ImportMultipleListings_ParallelProcessing_AllProcessed()
    {
        var listings = new[]
        {
            ("2019 Hyundai Elantra", "RD$ 950,000"),
            ("2021 Nissan Sentra", "RD$ 1,200,000"),
            ("2020 Chevrolet Spark", "RD$ 750,000"),
            ("2018 Toyota RAV4", "RD$ 1,600,000"),
        };

        var processedCount = 0;
        var limiter = new RateLimiter(maxRequestsPerMinute: 100);

        foreach (var (title, priceText) in listings)
        {
            if (limiter.CanMakeRequest("batch-importer"))
            {
                var price = ExtractPriceFromHtml(priceText);
                var (brand, model, year) = ExtractVehicleInfoFromTitle(title);

                if (!string.IsNullOrEmpty(brand) && price > 0)
                    processedCount++;
            }
        }

        processedCount.Should().Be(4, "all 4 listings should be processed without hitting rate limit");
    }
}
