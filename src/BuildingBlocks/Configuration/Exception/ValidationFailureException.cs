namespace HautSphere.BuildingBlocks.Configuration.Exceptions
{
    public class ValidationFailureException : BaseException
    {
        public ValidationFailureException(string message)
            : base(message)
        {

        }

        public ValidationFailureException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
    public class BaseException : Exception
    {
        public BaseException(string message) : base(message)
        {
        }
        public BaseException()
        {
        }
        public BaseException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}
