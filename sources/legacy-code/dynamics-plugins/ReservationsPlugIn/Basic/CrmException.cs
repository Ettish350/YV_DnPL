using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BasicPlugIn
{
    public class CrmException
    {
       // [Serializable()]
        public class CrmErrorException : System.Exception
        {
            public CrmErrorException()
            {
            }


            public CrmErrorException(string message)
                : base(message)
            {
            }
        }
    }
}
