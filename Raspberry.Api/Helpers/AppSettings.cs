using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Raspberry.Api.Helpers
{
    public class AppSettings
    {
        public int AudioLectureSampleMS { get; set; }
        public int AdcAddress { get; set; }
        public int LowVolumeLevelConsideredMute { get; set; }
    }
}
