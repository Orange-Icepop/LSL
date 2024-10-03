using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.Services
{
    public static class AlgoServices
    {
        public static string GetSubstringBeforeNextSpace(string input, int startPosition)// 获取从起始位置到下一个空格之前的子字符串
        {
            if (startPosition < 0 || startPosition >= input.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startPosition), "起始位置应在字符串的有效范围内。");
            }

            int endIndex = input.IndexOf(' ', startPosition);

            if (endIndex == -1)
            {
                // 如果没有找到空格，则返回从起始位置到字符串末尾的子字符串  
                return input.Substring(startPosition);
            }
            else
            {
                // 返回从起始位置到下一个空格之前的子字符串  
                return input.Substring(startPosition, endIndex - startPosition);
            }
        }
    }
}
