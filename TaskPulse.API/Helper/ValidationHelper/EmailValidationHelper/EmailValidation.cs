namespace TaskPulse.API.Helper.ValidationHelper.EmailValidationHelper;

    // Example usage:
    //
    // Just need a yes/no:
    // bool ok = EmailValidator.IsValidEmail(dto.Email);
    // if (!ok) return BadRequest("Invalid email address.");
    //
    // Want the reason for user-facing feedback later:
    // if (!EmailValidator.CheckEmail(dto.Email, out var issue))
    //     return BadRequest(issue);

using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

public static class EmailValidator
{
    // Reasonable, widely-used email pattern. Not fully RFC 5322 compliant
    // (nothing simple is), but catches the vast majority of real mistakes.
    private static readonly Regex EmailPattern = new Regex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled);

    /// <summary>
    /// Simple pass/fail check for whether a string is a validly formatted email address.
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        return CheckEmail(email, out _);
    }

    /// <summary>
    /// Validates format. Returns false and sets <paramref name="issue"/> to the reason
    /// when invalid; returns true and sets <paramref name="issue"/> to null when valid.
    /// </summary>
    public static bool CheckEmail(string? email, out string? issue)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            issue = "Email is required.";
            return false;
        }

        email = email.Trim();

        if (email.Length > 254) // RFC 5321 max length
        {
            issue = "Email is too long.";
            return false;
        }

        if (!EmailPattern.IsMatch(email))
        {
            issue = "Email format is invalid.";
            return false;
        }

        // Cross-check with .NET's own MailAddress parser as a second layer —
        // catches edge cases the regex alone might miss or wrongly allow.
        try
        {
            var parsed = new MailAddress(email);
            if (parsed.Address != email)
            {
                issue = "Email format is invalid.";
                return false;
            }
        }
        catch (FormatException)
        {
            issue = "Email format is invalid.";
            return false;
        }

        issue = null;
        return true;
    }
}