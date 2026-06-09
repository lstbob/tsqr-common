namespace TSQR.Common.Errors;

public sealed record NotFoundError(string Code, string Message) : Error(Code, Message);
