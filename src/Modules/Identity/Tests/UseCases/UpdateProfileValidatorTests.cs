using BUnited.Modules.Identity.Application.UseCases.Profile;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class UpdateProfileValidatorTests
{
    private static readonly UpdateProfileValidator Validator = new();

    [Fact]
    public async Task Valid_request_passes()
    {
        var result = await Validator.ValidateAsync(new UpdateProfileRequest("Europe/Bucharest", "ro", true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rejects_an_unknown_timezone()
    {
        var result = await Validator.ValidateAsync(new UpdateProfileRequest("Not/A_Real_Zone", "ro", true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileRequest.Timezone)
            && e.ErrorCode == "errors.timezone.invalid");
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("")]
    public async Task Rejects_an_unsupported_language(string language)
    {
        var result = await Validator.ValidateAsync(new UpdateProfileRequest("Europe/Bucharest", language, true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileRequest.PreferredLanguage));
    }
}
