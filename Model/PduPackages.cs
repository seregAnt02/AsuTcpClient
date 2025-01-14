using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.Model {
    class PduPackages {
        internal byte slave_adress { get; set; }
        internal int function_code { get; set; }
        internal int start_adress_high { get; set; }
        internal int start_adress_low { get; set; }
        internal int high_count { get; set; }
        internal int low_count { get; set; }
        internal string hing_volume { get; set; }
        internal string low_volume { get; set; }
    }
}
