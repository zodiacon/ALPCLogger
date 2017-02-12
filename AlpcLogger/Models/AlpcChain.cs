using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpcLogger.Models {
    class AlpcChainItem {
        public string ProcessName { get; internal set; }
        public int Process { get; set; }
        public int Thread { get; set; }
    }

}
