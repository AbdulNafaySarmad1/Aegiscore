namespace AegisCore.Infrastructure.Deployment;

/// <summary>
/// A rule of the institution's own, refusing what was asked: a payment larger than the demand it
/// pays, a voucher that is already posted, a correction after the window closed. The caller is not
/// at fault in the sense of having sent nonsense, and the server has not failed — the state of the
/// register simply does not allow it.
///
/// These were plain <see cref="InvalidOperationException"/>s, which the middleware could not tell
/// apart from a defect, so a clerk paying twice by mistake was told "Something went wrong on the
/// server" and given a trace id. Answered as 409 with the rule's own sentence.
/// </summary>
public sealed class BusinessRuleException(string message) : InvalidOperationException(message);
