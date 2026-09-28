namespace ImageSearch.Core
{
    public abstract record Result<TData, TError>
    {
        private protected Result() { }

        public static Result<TData, TError> FromSuccess(TData data) => new Success(data);
        public static Result<TData, TError> FromError(TError error) => new Error(error);

        public sealed record Success : Result<TData, TError>
        {
            public TData Data { get; }

            internal Success(TData data) => Data = data;
        }

        public sealed record Error : Result<TData, TError>
        {
            public TError Value { get; }

            internal Error(TError error) => Value = error;
        }
    }
}
