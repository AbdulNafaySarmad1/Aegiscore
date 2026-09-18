namespace AegisCore.Infrastructure.Deployment;

/// <summary>
/// Something this installation has not been set up for yet, as distinct from something that went
/// wrong. These were plain <see cref="InvalidOperationException"/>s and reached the caller as
/// "Something went wrong on the server" with a trace id — which sent an administrator to the
/// application log to discover that the answer was a setting they had not filled in.
///
/// Answered as 409 with <paramref name="message"/> shown to the reader, so the sentence names what
/// is missing and where to set it.
/// </summary>
public sealed class ConfigurationRequiredException(string message) : InvalidOperationException(message);
