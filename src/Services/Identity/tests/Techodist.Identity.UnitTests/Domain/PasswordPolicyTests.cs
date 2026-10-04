using Techodist.Identity.Domain.ValueObjects;
using Xunit;

namespace Techodist.Identity.UnitTests.Domain;

public sealed class PasswordPolicyTests
{
    [Fact]
    public void Validate_StrongPassword_ReturnsNoViolations()
    {
        Assert.Empty(PasswordPolicy.Default.Validate("Techodist!Dev2026", "admin@techodist.local"));
        Assert.True(PasswordPolicy.Default.IsSatisfied("Techodist!Dev2026"));
    }

    [Fact]
    public void Validate_EmptyPassword_ReportsSingleRequiredViolation()
        => Assert.Equal(new[] { "Пароль обязателен." }, PasswordPolicy.Default.Validate("   "));

    [Fact]
    public void Validate_WeakPassword_ReportsEveryBrokenRule()
    {
        var violations = PasswordPolicy.Default.Validate("admin", "admin@techodist.local");

        Assert.Equal(4, violations.Count);
        Assert.Contains(violations, v => v.Contains($"{PasswordPolicy.MinimumLength} символов"));
        Assert.Contains("Пароль должен содержать заглавную букву.", violations);
        Assert.Contains("Пароль должен содержать цифру.", violations);
        Assert.Contains("Пароль должен содержать специальный символ.", violations);
    }

    [Fact]
    public void Validate_MissingSpecialCharacter_ReportsSpecialViolation()
        => Assert.Equal(
            new[] { "Пароль должен содержать специальный символ." },
            PasswordPolicy.Default.Validate("Techodist12345", "admin@techodist.local"));

    [Fact]
    public void Validate_PasswordEqualToEmail_ReportsViolation()
    {
        var violations = PasswordPolicy.Default.Validate("admin@techodist.local", "admiN@techodist.local");

        Assert.Contains("Пароль не должен совпадать с e-mail.", violations);
    }

    [Fact]
    public void Describe_MentionsMinimumLength()
        => Assert.Contains(PasswordPolicy.MinimumLength.ToString(), PasswordPolicy.Default.Describe());
}
