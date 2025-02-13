public readonly struct Result<T>
{
    public ResultState State { get; }
    public T Value { get; }
    public string ErrorMessage { get; }

    private Result(ResultState state, T value = default, string errorMessage = null)
    {
        State = state;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Ok(T value) => new Result<T>(ResultState.Ok, value);
    public static Result<T> Error(string errorMessage) => new Result<T>(ResultState.Error, default, errorMessage);
    public static Result<T> NotFound() => new Result<T>(ResultState.NotFound);
    public static Result<T> AlreadyExists() => new Result<T>(ResultState.AlreadyExists);

    public override readonly string ToString() =>
        State switch
        {
            ResultState.Ok => $"Ok({Value})",
            ResultState.Error => $"Error({ErrorMessage})",
            ResultState.NotFound => "NotFound",
            ResultState.AlreadyExists => "AlreadyExists",
            _ => "Unknown"
        };
}

// Enum for result states
public enum ResultState
{
    Ok,
    Error,
    NotFound,
    AlreadyExists
}
