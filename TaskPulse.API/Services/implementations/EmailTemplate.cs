using System.Net;

namespace TaskPulse.API.Services.implementations
{
    // A single, plain HTML shell shared by every transactional email
    // (confirm email, reset password). Deliberately simple — no images, no
    // tracking pixels, no link shorteners, a real clickable button plus the
    // raw URL as a fallback — the kind of mail spam filters are least
    // suspicious of, as opposed to a bare paragraph with just a token in it.
    public static class EmailTemplate
    {
        public static string Render(string heading, string intro, string buttonLabel, string actionUrl, string footnote)
        {
            // The URL is also the button's href, so it has to be
            // HTML-attribute-safe; everything else is plain display text.
            var safeUrl = WebUtility.HtmlEncode(actionUrl);

            return $$"""
                <div style="font-family: Arial, Helvetica, sans-serif; max-width: 480px; margin: 0 auto; padding: 24px; color: #1f2937;">
                  <p style="font-size: 13px; font-weight: 600; color: #3563E9; letter-spacing: 0.04em; text-transform: uppercase; margin: 0 0 16px;">TaskPulse</p>
                  <h2 style="font-size: 20px; margin: 0 0 12px;">{{WebUtility.HtmlEncode(heading)}}</h2>
                  <p style="font-size: 14px; line-height: 1.6; color: #4b5563; margin: 0 0 24px;">{{WebUtility.HtmlEncode(intro)}}</p>
                  <p style="margin: 0 0 24px;">
                    <a href="{{safeUrl}}" style="background: #3563E9; color: #ffffff; padding: 10px 22px; border-radius: 6px; text-decoration: none; font-weight: 600; font-size: 14px; display: inline-block;">{{WebUtility.HtmlEncode(buttonLabel)}}</a>
                  </p>
                  <p style="font-size: 12px; color: #9ca3af; line-height: 1.6; margin: 0 0 8px;">If the button doesn't work, copy and paste this link into your browser:</p>
                  <p style="font-size: 12px; word-break: break-all; margin: 0 0 24px;"><a href="{{safeUrl}}" style="color: #3563E9;">{{safeUrl}}</a></p>
                  <p style="font-size: 12px; color: #9ca3af; line-height: 1.6; margin: 0;">{{WebUtility.HtmlEncode(footnote)}}</p>
                </div>
                """;
        }
    }
}
