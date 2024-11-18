using System;

namespace LSL.Components
{
    public class LSLException : Exception// LSL异常，Exception的包装类
    {
        public LSLException(string message, Exception innerException) : base(message, innerException) { }
        public LSLException(string message) : base(message) { }
        public LSLException() { }
    }

    public class FatalException : LSLException { }// 致命错误

    public class NonfatalException : LSLException { }// 非致命错误
}
