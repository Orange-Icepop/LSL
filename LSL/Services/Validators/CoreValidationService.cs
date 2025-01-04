using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.Services.Validators
{
    public class CoreValidationService
    {
        public enum CoreType
        {
            illegal,
            unknown,
            client,
            installer,
            forge,
            server
        }
        public static CoreType Validate(string filePath)
        {
            var file = File.ReadAllText(filePath);
            return CoreType.unknown;
        }
    }
}
