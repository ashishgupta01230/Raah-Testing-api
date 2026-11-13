using System.Xml.Serialization;

namespace HautSphere.BuildingBlocks.Configuration.Models
{

    public class ResponseModel
    {

        public object Data { get; set; }

        public int ErrorCode { get; private set; }

        public string ErrorDescription { get; private set; }

        public FeedbackMessageModel Message { get; private set; }


        [XmlIgnore]
        public bool IsFeedbackSet
        {
            get
            {
                return Message != null;
            }
        }

        public void SetErrorMessage(string message, ResponseErrorCodeType errorCode = ResponseErrorCodeType.RandomException)
        {
            Message = FeedbackMessageModel.CreateErrorMessage(message);
            ErrorCode = (int)errorCode;
        }

        public void SetErrorMessage(string message, string errorDescription, ResponseErrorCodeType errorCode = ResponseErrorCodeType.RandomException)
        {
            Message = FeedbackMessageModel.CreateErrorMessage(message);
            ErrorDescription = errorDescription;
            ErrorCode = (int)errorCode;
        }

        public void SetSuccessMessage(string message)
        {
            Message = FeedbackMessageModel.CreateSuccessMessage(message);
        }

        public ResponseModel()
        {
            ErrorCode = 0;
        }

    }

    public enum ResponseErrorCodeType
    {
        RandomException = 1
    }
}
