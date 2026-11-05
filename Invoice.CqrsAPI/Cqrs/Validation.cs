namespace Invoice.CqrsAPI.Cqrs;

public sealed record ValidationFailure(string Property, string Message);

/// <summary>Thrown by the ValidationBehavior. The middleware turns it into HTTP 400.</summary>
public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Failures = failures.ToList();
    }

    public IReadOnlyList<ValidationFailure> Failures { get; }
}

public interface IValidator<in T>
{
    IReadOnlyList<ValidationFailure> Validate(T instance);
}

/// <summary>
/// Tiny rule-based validator (a light version of FluentValidation).
///
///   Rule(x => x.Age > 0, "Age", "Age must be positive.");              // one yes/no rule
///   Custom(x => x.Lines.SelectMany(CheckLine));                         // a rule that can report many failures
///
/// Validators check the SHAPE of the request only (required, length, range, format).
/// Rules that need the database ("does this vendor exist?") belong in the handler / BAL.
/// </summary>
public abstract class RuleValidator<T> : IValidator<T>
{
    private readonly List<Func<T, IEnumerable<ValidationFailure>>> _checks = new();

    protected void Rule(Func<T, bool> isValid, string property, string message)
    {
        _checks.Add(instance => isValid(instance)
            ? Enumerable.Empty<ValidationFailure>()
            : new[] { new ValidationFailure(property, message) });
    }

    protected void Custom(Func<T, IEnumerable<ValidationFailure>> check)
    {
        _checks.Add(check);
    }

    public IReadOnlyList<ValidationFailure> Validate(T instance)
    {
        return _checks.SelectMany(check => check(instance)).ToList();
    }
}
