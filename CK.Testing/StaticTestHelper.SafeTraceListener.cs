using System;

namespace CK.Testing;


static partial class StaticTestHelper
{
    /// <summary>
    /// This listener is removed by CK.Testing.MonitorTestHelper because the MonitorTraceListener
    /// that throws MonitoringFailFastException is injected and does the job.
    /// The key used to remove this listener is its name: "CK.Testing.SafeTraceListener" that
    /// MUST NOT be changed since this magic string is used by the MonitorTestHelper.
    /// </summary>
    sealed class SafeTraceListener : System.Diagnostics.DefaultTraceListener
    {
        const string _messagePrefix = "Assertion Failed: ";

        public SafeTraceListener()
        {
            Name = "CK.Testing.SafeTraceListener";
        }

        public override void Fail( string? message, string? detailMessage ) => throw new Exception( _messagePrefix + message + " - Detail: " + detailMessage );
        public override void Fail( string? message ) => throw new Exception( _messagePrefix + message );
    }
}
