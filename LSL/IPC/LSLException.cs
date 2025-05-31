using System;

namespace LSL.IPC
{
    public class LSLException : Exception// LSL异常，Exception的包装类
    {
        public LSLException(string message, Exception innerException) : base(message, innerException) { }
        public LSLException(string message) : base(message) { }
        public LSLException() { }
    }

    public class FatalException : LSLException
    { 
        public FatalException(string message, Exception innerException) : base(message, innerException) { }
        public FatalException(string message) : base(message) { }
        public FatalException() { }

    }// 致命错误

    public class NonfatalException : LSLException 
    {
        public NonfatalException(string message, Exception innerException) : base(message, innerException) { }
        public NonfatalException(string message) : base(message) { }
        public NonfatalException() { }

    }// 非致命错误
    /*
     * ServiceError使用说明
     * ErrorCode为0时表示没有错误。
     * 1:执行完成，但是包含警告。
     * 2:出现错误。只代表最基础的错误返回。
     * -1:在没有执行完成的情况下中断了。
     */
    public class ServiceError
    {
        public int ErrorCode { get; }
        public string? Message { get; }
        public Exception? Error { get; }
        private ServiceError()
        {
            ErrorCode = 0;
            Message = null;
            Error = null;
        }
        public ServiceError(int errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
            Error = null;
        }
        public ServiceError(int errorCode, Exception error)
        {
            ErrorCode = errorCode;
            Error = error;
        }
        public static ServiceError Success => new ServiceError();
    }
}
