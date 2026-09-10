using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CoreLibs.CQRS.Tests;

public record TestQuery(string Name) : IQuery<string>;

public class TestQueryValidator : AbstractValidator<TestQuery>
{
    public TestQueryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
    }
}

public class ValidationBehaviorTests
{
    private static IOptionsMonitor<CoreCqrsOptions> BuildOptions(bool enabled = true)
    {
        var monitor = Substitute.For<IOptionsMonitor<CoreCqrsOptions>>();
        monitor.CurrentValue.Returns(new CoreCqrsOptions { EnableValidationBehavior = enabled });
        return monitor;
    }

    private static RequestHandlerDelegate<ErrorOr<string>> Next(string value = "ok") =>
        () => Task.FromResult(ErrorOrFactory.From(value));

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var sut = new ValidationBehavior<TestQuery, string>([], BuildOptions());
        var called = false;
        var result = await sut.Handle(new TestQuery("test"), () =>
        {
            called = true;
            return Task.FromResult(ErrorOrFactory.From("ok"));
        }, CancellationToken.None);

        Assert.True(called);
        Assert.False(result.IsError);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var sut = new ValidationBehavior<TestQuery, string>(
            [new TestQueryValidator()],
            BuildOptions());

        var result = await sut.Handle(new TestQuery("Alice"), Next(), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ReturnsValidationErrors_WithoutCallingNext()
    {
        var sut = new ValidationBehavior<TestQuery, string>(
            [new TestQueryValidator()],
            BuildOptions());

        var nextCalled = false;
        var result = await sut.Handle(new TestQuery(""), () =>
        {
            nextCalled = true;
            return Task.FromResult(ErrorOrFactory.From("ok"));
        }, CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Type == ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenDisabled_CallsNext_EvenIfInvalid()
    {
        var sut = new ValidationBehavior<TestQuery, string>(
            [new TestQueryValidator()],
            BuildOptions(enabled: false));

        var called = false;
        var result = await sut.Handle(new TestQuery(""), () =>
        {
            called = true;
            return Task.FromResult(ErrorOrFactory.From("ok"));
        }, CancellationToken.None);

        Assert.True(called);
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task Handle_DuplicateErrors_AreDeduplicatedByCodeAndDescription()
    {
        // Two validators that produce the same error
        var validator1 = new TestQueryValidator();
        var validator2 = new TestQueryValidator();

        var sut = new ValidationBehavior<TestQuery, string>(
            [validator1, validator2],
            BuildOptions());

        var result = await sut.Handle(new TestQuery(""), () =>
            Task.FromResult(ErrorOrFactory.From("ok")), CancellationToken.None);

        Assert.True(result.IsError);
        // Deduplicated: only one error despite two identical validators
        Assert.Single(result.Errors);
    }
}
