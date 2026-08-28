namespace TaskPulse.API.Helper.ValidationHelper.PasswordValidationHelper;
// Example usage:
//
// Just need a yes/no answer right now:
// bool ok = PasswordStrengthValidator.IsStrong(request.Password);
// if (!ok) return BadRequest("Password is too weak.");
//
// Want a stricter bar than "Medium"?
// bool ok = PasswordStrengthValidator.IsStrong(request.Password, PasswordStrength.Strong);
//
// Single combined reason:
// bool ok = PasswordStrengthValidator.CheckStrength(request.Password, out var issue);
// if (!ok) return BadRequest(issue);
//
// Full detail (strength level + each individual reason):
// bool ok = PasswordStrengthValidator.CheckStrength(request.Password, out var strength, out var issues);
// if (!ok) return BadRequest(new { strength, issues });

using System;
using System.Linq;
using System.Text.RegularExpressions;

public enum PasswordStrength
{
    VeryWeak = 0,
    Weak = 1,
    Medium = 2,
    Strong = 3,
    VeryStrong = 4
}

public static class PasswordStrengthValidator
{
    // A tiny sample of extremely common passwords / sequences worth flagging.
    // Swap in a real breached-password list (e.g. Have I Been Pwned's list) for production use.
    private static readonly string[] CommonPasswords =
    {
        "password", "123456", "12345678", "qwerty", "abc123",
        "letmein", "iloveyou", "admin", "welcome", "monkey", "football"
    };

    /// <summary>
    /// Simple pass/fail check. Returns true if the password meets at least
    /// the "Medium" strength bar (or your own custom minimum).
    /// </summary>
    public static bool IsStrong(string password, PasswordStrength minimum = PasswordStrength.Medium)
    {
        CheckStrength(password, out var strength, out _);
        return strength >= minimum;
    }

    /// <summary>
    /// Returns false and sets <paramref name="issue"/> to a single combined reason
    /// when below "Medium"; returns true (and null issue) when it meets the "Medium" bar.
    /// </summary>
    public static bool CheckStrength(string password, out string? issue)
    {
        bool ok = CheckStrength(password, out _, out var issues);
        issue = issues.Length > 0 ? string.Join(" ", issues) : null;
        return ok;
    }

    /// <summary>
    /// Returns false and sets <paramref name="strength"/>/<paramref name="issues"/> to the
    /// reasons when below "Medium"; returns true when the password meets the "Medium" bar.
    /// </summary>
    public static bool CheckStrength(string password, out PasswordStrength strength, out string[] issues)
    {
        if (string.IsNullOrEmpty(password))
        {
            strength = PasswordStrength.VeryWeak;
            issues = new[] { "Password is empty." };
            return false;
        }

        var issueList = new System.Collections.Generic.List<string>();
        int score = 0;

        // --- Length ---
        if (password.Length >= 8) score++;
        else issueList.Add("Use at least 8 characters.");

        if (password.Length >= 12) score++; // bonus for longer passwords

        // --- Character variety ---
        bool hasLower = Regex.IsMatch(password, @"[a-z]");
        bool hasUpper = Regex.IsMatch(password, @"[A-Z]");
        bool hasDigit = Regex.IsMatch(password, @"\d");
        bool hasSymbol = Regex.IsMatch(password, @"[^a-zA-Z0-9]");

        if (hasLower && hasUpper) score++;
        else issueList.Add("Mix uppercase and lowercase letters.");

        if (hasDigit) score++;
        else issueList.Add("Include at least one number.");

        if (hasSymbol) score++;
        else issueList.Add("Include at least one special character (e.g. !@#$%).");

        // --- Penalize repeated characters (e.g. "aaaa", "1111") ---
        if (Regex.IsMatch(password, @"(.)\1{2,}"))
        {
            score--;
            issueList.Add("Avoid repeating the same character 3+ times in a row.");
        }

        // --- Penalize sequential characters (e.g. "1234", "abcd") ---
        if (HasSequentialChars(password, 4))
        {
            score--;
            issueList.Add("Avoid simple sequences like \"1234\" or \"abcd\".");
        }

        // --- Penalize common/breached passwords ---
        if (CommonPasswords.Any(p => string.Equals(p, password, StringComparison.OrdinalIgnoreCase)))
        {
            score = 0;
            issueList.Add("This password is far too common — choose something less predictable.");
        }

        score = Math.Clamp(score, 0, 6);

        strength = score switch
        {
            <= 1 => PasswordStrength.VeryWeak,
            2 => PasswordStrength.Weak,
            3 => PasswordStrength.Medium,
            4 => PasswordStrength.Strong,
            _ => PasswordStrength.VeryStrong
        };

        issues = issueList.ToArray();
        return strength >= PasswordStrength.Medium;
    }

    private static bool HasSequentialChars(string password, int runLength)
    {
        string lower = password.ToLowerInvariant();
        for (int i = 0; i <= lower.Length - runLength; i++)
        {
            bool ascending = true;
            bool descending = true;

            for (int j = 0; j < runLength - 1; j++)
            {
                int diff = lower[i + j + 1] - lower[i + j];
                if (diff != 1) ascending = false;
                if (diff != -1) descending = false;
            }

            if (ascending || descending) return true;
        }
        return false;
    }
}