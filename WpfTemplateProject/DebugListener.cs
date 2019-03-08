using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DupFileCleaner
{
    class DebugListener : TraceListener
    {
        private readonly Action<string> _appender;

        public DebugListener(Action<string> appender)
        {
            _appender = appender;
        }

        public override void Write(string message)
        {
            _appender(DateTime.Now.ToString("g") + ": " + message);
        }

        public override void WriteLine(string message)
        {
            _appender(DateTime.Now.ToString("g") + ": " + message + Environment.NewLine);
        }
    }
}
