
namespace MuhasibPro.Domain.Common
{
    public class Result
    {
        public bool IsOk { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
        public Exception Exception { get; set; }

        static public Result Ok()
        {
            return Ok(null, null);
        }
        static public Result Ok(string message = null, string description = null)
        {
            return new Result { IsOk = true, Message = message, Description = description };
        }

        static public Result Error(string message, string description = null)
        {
            return new Result { IsOk = false, Message = message, Description = description };
        }
        static public Result Error(Exception ex)
        {
            return new Result { IsOk = false, Message = ex.Message, Description = ex.ToString(), Exception = ex };
        }

        public override string ToString()
        {
            return $"{Message}\r\n{Description}";
        }
    }
}
