namespace MuhasibPro.Domain.Utilities.Responses
{
    public static class ApiDataExtensions
    {
        public static ApiDataResponse<T> SuccessResponse<T>(T data, string message, int resultCount=0)
        => new SuccessApiDataResponse<T>(data, message, success:true,resultCodes:Enum.ResultCodes.BASARILI_Tamamlandi,resultCount:resultCount);
        public static ApiDataResponse<T> ErrorResponse<T>(T data, string message,int resultCount=0)
            => new ErrorApiDataResponse<T>(data, message,success:false,resultCodes:Enum.ResultCodes.HATA_BilinmeyenHata,resultCount: resultCount);
        
    }
}
