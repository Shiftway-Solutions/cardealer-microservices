using FluentAssertions;
using FluentValidation.TestHelper;
using SupportAgent.Application.Features.Chat.Commands;
using SupportAgent.Application.Features.Chat.Validators;
using Xunit;

namespace SupportAgent.Tests;

public class SendMessageCommandValidatorTests
{
    private readonly SendMessageCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyMessage_ShouldFail()
    {
        var command = new SendMessageCommand("", null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void Validate_ValidMessage_ShouldPass()
    {
        var command = new SendMessageCommand("¿Cómo me registro?", null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TooLongMessage_ShouldFail()
    {
        var longMessage = new string('a', 2001);
        var command = new SendMessageCommand(longMessage, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void Validate_TooLongSessionId_ShouldFail()
    {
        var longSessionId = new string('x', 65);
        var command = new SendMessageCommand("Hola", longSessionId, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
    }
}
